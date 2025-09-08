using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var jwtKey = cfg["Jwt:Key"]!;
var jwtIssuer = cfg["Jwt:Issuer"]!;
var jwtAudience = cfg["Jwt:Audience"]!;
var jwtHours = cfg.GetValue("Jwt:Hours", 2);
var googleCid = cfg["Google:ClientId"]!;
var googleSecret = cfg["Google:ClientSecret"]!;
var clientOrigin = cfg["Client:Origin"]!;

builder.Services.AddCors(p => p.AddDefaultPolicy(b =>
    b.WithOrigins(clientOrigin)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

builder.Services.AddHttpClient();

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/public/ping", () => Results.Ok(new { ok = true, msg = "public pong" })).AllowAnonymous();

app.MapPost("/auth/google", async (GoogleExchange body, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(body.code))
        return Results.BadRequest(new { error = "code is required" });

    var httpClient = httpClientFactory.CreateClient();

    try
    {
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", googleCid),
            new KeyValuePair<string, string>("client_secret", googleSecret),
            new KeyValuePair<string, string>("code", body.code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", clientOrigin)
        });

        var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            return Results.Problem($"Token exchange failed: {errorContent}", statusCode: 400);
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenJson);

        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.access_token);
        var userResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        if (!userResponse.IsSuccessStatusCode)
        {
            return Results.Problem("Failed to get user info", statusCode: 400);
        }

        var userJson = await userResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<GoogleUserInfo>(userJson);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userData.id ?? ""),
            new Claim(ClaimTypes.Name, userData.name ?? userData.email ?? ""),
            new Claim(ClaimTypes.Email, userData.email ?? ""),
            new Claim("picture", userData.picture ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(jwtHours),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token) });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
}).AllowAnonymous();

app.MapGet("/secure/ping", (ClaimsPrincipal user) =>
{
    var name = user.Identity?.Name ?? "";
    var email = user.FindFirst(ClaimTypes.Email)?.Value ?? "";
    return Results.Ok(new { ok = true, msg = "secure pong", name, email });
}).RequireAuthorization();

app.Run();

record GoogleExchange(string code);
record GoogleTokenResponse(string access_token, string token_type, int expires_in);
record GoogleUserInfo(string id, string email, string name, string picture);