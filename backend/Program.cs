using System.Security.Claims;
using System.Text.Json;
using ImageVault.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
// Read Google OAuth config (env or appsettings)
var cfg = builder.Configuration;
var googleClientId = cfg["Google:ClientId"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? string.Empty;
var googleClientSecret = cfg["Google:ClientSecret"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? string.Empty;
var googleConfigured = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = googleConfigured
        ? GoogleDefaults.AuthenticationScheme
        : CookieAuthenticationDefaults.AuthenticationScheme;
});

authBuilder.AddCookie(options =>
{
    options.Cookie.Name = "imagevault.auth";
    options.LoginPath = "/api/auth/login";
    options.LogoutPath = "/api/auth/logout";
    options.SlidingExpiration = true;
});

if (googleConfigured)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/auth/callback/google";
        options.SaveTokens = true;
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Listen on 5080 on all interfaces by default (LAN access)
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(urls))
{
    app.Urls.Add("http://0.0.0.0:5080");
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

var appDataDir = Path.Combine(app.Environment.ContentRootPath, "app_data");
var configPath = Path.Combine(appDataDir, "config.json");
var imagesDir = Path.Combine(appDataDir, "images");

Directory.CreateDirectory(appDataDir);
Directory.CreateDirectory(imagesDir);

static bool IsConfigComplete(AppConfig? c)
{
    return c != null
           && !string.IsNullOrWhiteSpace(c.DbHost)
           && !string.IsNullOrWhiteSpace(c.DbName)
           && !string.IsNullOrWhiteSpace(c.DbUser);
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/config/status", () =>
{
    if (!File.Exists(configPath))
        return Results.Ok(new { present = false, complete = false });

    try
    {
        var json = File.ReadAllText(configPath);
        var cfg = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return Results.Ok(new { present = true, complete = IsConfigComplete(cfg) });
    }
    catch (JsonException je)
    {
        app.Logger.LogWarning(je, "Invalid JSON in config file at {ConfigPath}", configPath);
        return Results.Ok(new { present = true, complete = false, error = "Invalid JSON in config.json" });
    }
});

app.MapPost("/api/config", async (HttpContext http) =>
{
    try
    {
        var cfg = await JsonSerializer.DeserializeAsync<AppConfig>(http.Request.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (cfg is null)
            return Results.BadRequest(new { error = "Invalid JSON body" });

        await using var fs = File.Create(configPath);
        await JsonSerializer.SerializeAsync(fs, cfg, new JsonSerializerOptions { WriteIndented = true });
        return Results.Ok(new { saved = true });
    }
    catch (JsonException je)
    {
        app.Logger.LogWarning(je, "Failed to parse config JSON from request body");
        return Results.BadRequest(new { error = "Invalid JSON", detail = je.Message });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unexpected error saving config");
        return Results.Problem("Internal server error");
    }
});

app.MapGet("/api/auth/login", (HttpContext ctx) =>
{
    // Block login until setup is complete
    try
    {
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!IsConfigComplete(cfg))
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
        else
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    if (!googleConfigured)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    var props = new AuthenticationProperties { RedirectUri = "/" };
    return Results.Challenge(props, new[] { GoogleDefaults.AuthenticationScheme });
});

app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { loggedOut = true });
});

app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
{
    if (user?.Identity?.IsAuthenticated != true) return Results.Unauthorized();

    var name = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity?.Name ?? "";
    var email = user.FindFirst(ClaimTypes.Email)?.Value ?? "";
    var picture = user.FindFirst("picture")?.Value ?? "";
    return Results.Ok(new { name, email, picture });
}).RequireAuthorization();

// Development helper: fake login without Google
// Enable in Development OR when Google OAuth is not configured
if (builder.Environment.IsDevelopment() || !googleConfigured)
{
    var doDevLogin = async (HttpContext ctx) =>
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Dev User"),
            new(ClaimTypes.Email, "dev@example.com")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.Ok(new { loggedIn = true, dev = true });
    };

    app.MapPost("/api/auth/dev-login", doDevLogin);
    app.MapGet("/api/auth/dev-login", doDevLogin);
    app.MapPost("/api/auth/dev-logout", async (HttpContext ctx) =>
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { loggedOut = true, dev = true });
    });
}

// Upload image (auth required)
app.MapPost("/api/images", async (HttpContext http) =>
{
    if (http.User?.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var form = await http.Request.ReadFormAsync();
    var file = form.Files["file"];
    var description = form["description"].ToString() ?? string.Empty;
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "file missing" });

    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    var ext = Path.GetExtension(file.FileName);
    if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext))
        return Results.BadRequest(new { error = "unsupported file type" });

    var id = Guid.NewGuid().ToString("N");
    var idDir = Path.Combine(imagesDir, id);
    Directory.CreateDirectory(idDir);

    var originalPath = Path.Combine(idDir, "original" + ext.ToLowerInvariant());
    await using (var fs = File.Create(originalPath))
    {
        await file.CopyToAsync(fs);
    }

    var thumbPath = Path.Combine(idDir, "thumb.jpg");
    using (var image = await Image.LoadAsync(originalPath))
    {
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(128, 128)
        }));
        await image.SaveAsJpegAsync(thumbPath, new JpegEncoder { Quality = 85 });
    }

    var item = new ImageItem
    {
        Id = id,
        Description = description ?? string.Empty,
        OriginalPath = originalPath,
        ThumbPath = thumbPath,
        OriginalContentType = file.ContentType ?? "application/octet-stream",
        UploadedAt = DateTime.UtcNow
    };
    ImageIndex.Add(imagesDir, item);

    return Results.Ok(new { id, thumbUrl = $"/api/images/{id}/thumb" });
});

// Search images (public)
app.MapGet("/api/images/search", (HttpRequest req) =>
{
    var q = req.Query["q"].ToString() ?? string.Empty;
    var items = ImageIndex.Load(imagesDir);
    IEnumerable<ImageItem> query = items;
    if (!string.IsNullOrWhiteSpace(q))
    {
        query = items.Where(i => (i.Description ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase));
    }
    var results = query
        .OrderByDescending(i => i.UploadedAt)
        .Take(10)
        .Select(i => new { id = i.Id, description = i.Description, thumbUrl = $"/api/images/{i.Id}/thumb" });
    return Results.Ok(results);
});

// Serve thumbnail (public)
app.MapGet("/api/images/{id}/thumb", (string id) =>
{
    var item = ImageIndex.Get(imagesDir, id);
    if (item is null || !File.Exists(item.ThumbPath)) return Results.NotFound();
    return Results.File(item.ThumbPath, "image/jpeg");
});

// Serve original (public)
app.MapGet("/api/images/{id}/original", (string id) =>
{
    var item = ImageIndex.Get(imagesDir, id);
    if (item is null || !File.Exists(item.OriginalPath)) return Results.NotFound();
    var contentType = string.IsNullOrWhiteSpace(item.OriginalContentType) ? "application/octet-stream" : item.OriginalContentType;
    return Results.File(item.OriginalPath, contentType);
});

app.Run();

// Image models and helpers
namespace ImageVault.Api.Models
{
    public class ImageItem
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public string ThumbPath { get; set; } = string.Empty;
        public string OriginalContentType { get; set; } = "";
        public DateTime UploadedAt { get; set; }
    }
}

static class ImageIndex
{
    private const string IndexFileName = "index.json";
    private static readonly object Gate = new();

    public static List<ImageVault.Api.Models.ImageItem> Load(string imagesDir)
    {
        lock (Gate)
        {
            var path = Path.Combine(imagesDir, IndexFileName);
            if (!File.Exists(path)) return new();
            try
            {
                var json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<ImageVault.Api.Models.ImageItem>>(json) ?? new();
                return list;
            }
            catch
            {
                return new();
            }
        }
    }

    public static void Save(string imagesDir, List<ImageVault.Api.Models.ImageItem> items)
    {
        lock (Gate)
        {
            var path = Path.Combine(imagesDir, IndexFileName);
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public static void Add(string imagesDir, ImageVault.Api.Models.ImageItem item)
    {
        var list = Load(imagesDir);
        list.RemoveAll(x => x.Id == item.Id);
        list.Add(item);
        Save(imagesDir, list);
    }

    public static ImageVault.Api.Models.ImageItem? Get(string imagesDir, string id)
    {
        var list = Load(imagesDir);
        return list.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
