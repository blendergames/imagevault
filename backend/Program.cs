using System.Security.Claims;
using System.Text.Json;
using ImageVault.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "imagevault.auth";
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        var cfg = builder.Configuration;
        options.ClientId = cfg["Google:ClientId"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
        options.ClientSecret = cfg["Google:ClientSecret"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";
        options.CallbackPath = "/api/auth/callback/google";
        options.SaveTokens = true;
    });

var app = builder.Build();

// Listen on 5080 by default for local dev
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(urls))
{
    app.Urls.Add("http://localhost:5080");
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

    var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
    return Results.Ok(new { present = true, complete = IsConfigComplete(cfg) });
});

app.MapPost("/api/config", async (HttpContext http) =>
{
    var cfg = await JsonSerializer.DeserializeAsync<AppConfig>(http.Request.Body);
    if (cfg is null) return Results.BadRequest(new { error = "Invalid body" });

    await using var fs = File.Create(configPath);
    await JsonSerializer.SerializeAsync(fs, cfg, new JsonSerializerOptions { WriteIndented = true });
    return Results.Ok(new { saved = true });
});

app.MapGet("/api/auth/login", (HttpContext ctx) =>
{
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

app.Run();

