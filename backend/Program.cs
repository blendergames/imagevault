using System.Security.Claims;
using System.Text.Json;
using ImageVault.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

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

Directory.CreateDirectory(appDataDir);

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

app.Run();
