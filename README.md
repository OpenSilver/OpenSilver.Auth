# OpenSilver Google OAuth Authentication Demo

A complete example implementing Google OAuth authentication between an OpenSilver client and ASP.NET Core Minimal API backend. Includes production-ready patterns and security best practices.

**Key Features:**
- User authentication via Google OAuth 2.0
- JWT token-based API security
- HTTPS enforcement and CORS configuration
- Secure token exchange between client and server

## Implemented Authentication Providers

| Provider | Status | Description |
|----------|--------|-------------|
| Google OAuth 2.0 | ✅ Complete | Google account login, ID token validation, JWT issuance |

## Exploring the Project

```bash
git clone https://github.com/opensilver/opensilver.auth
```

The project consists of two main parts:

```
OpenSilverAuthClient/
├── OpenSilverAuthClient.Browser/       # Client (OpenSilver)
│   ├── wwwroot/
│   │   ├── index.html                  # Google OAuth configuration
│   │   └── libs/auth.js                # Client authentication logic
│   └── Properties/launchSettings.json
├── OpenSilverAuthClient.Server/        # Server (Minimal API)
│   ├── Program.cs                      # Backend authentication logic
│   └── appsettings.json                # JWT and Google settings
```

## Client Implementation (OpenSilver)

### `wwwroot/index.html`
Google OAuth library loading and basic configuration:

```javascript
// Google OAuth configuration
window.AUTH_CFG = {
    apiBase: "https://localhost:7224",
    googleClientId: "YOUR_GOOGLE_CLIENT_ID",
    storageKey: "accessToken"
};

// Load Google GSI library
<script src="https://accounts.google.com/gsi/client" onload="onGoogleLibraryLoad()"></script>
```

### `wwwroot/libs/auth.js`
Core client authentication logic:

```javascript
async function login() {
    // Initialize Google OAuth
    google.accounts.id.initialize({
        client_id: CFG.googleClientId,
        callback: async (resp) => {
            // Send Google ID token to backend
            const r = await fetch(CFG.apiBase + "/auth/google", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ idToken: resp.credential })
            });
            // Store JWT token in localStorage
            const data = await r.json();
            localStorage.setItem(KEY, data.accessToken);
        }
    });
    google.accounts.id.prompt();
}
```

**Process Flow:**
1. User clicks "Login with Google" button
2. Google OAuth popup displays
3. After user login completion, ID token is received
4. ID token is sent to backend API
5. JWT token received from backend and stored in localStorage

### `Properties/launchSettings.json`
HTTPS development environment configuration:

```json
{
  "profiles": {
    "OpenSilverAuthClient.Browser": {
      "applicationUrl": "https://localhost:55922;http://localhost:55592"
    }
  }
}
```

## Server Implementation (ASP.NET Core Minimal API)

### `Program.cs`
Complete backend authentication pipeline:

```csharp
// 1. Load configuration
var googleCid = cfg["Google:ClientId"]!;
var jwtKey = cfg["Jwt:Key"]!;

// 2. CORS configuration (allow client origin)
builder.Services.AddCors(p => p.AddDefaultPolicy(b =>
    b.WithOrigins("https://localhost:55922")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// 3. JWT authentication setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* JWT token validation settings */);

// 4. Google ID token → JWT conversion endpoint
app.MapPost("/auth/google", async (GoogleExchange body) => {
    // Validate Google ID token
    var payload = await GoogleJsonWebSignature.ValidateAsync(
        body.idToken,
        new GoogleJsonWebSignature.ValidationSettings { 
            Audience = new[] { googleCid } 
        });
    
    // Generate JWT token
    var token = new JwtSecurityToken(/* Create JWT with user info */);
    return Results.Ok(new { accessToken = tokenHandler.WriteToken(token) });
});

// 5. Protected API endpoint
app.MapGet("/secure/ping", (ClaimsPrincipal user) => {
    // API requiring JWT authentication
}).RequireAuthorization();
```

### `appsettings.json`
JWT and Google OAuth configuration:

```json
{
  "Jwt": {
    "Key": "minimum-32-character-secret-key-required",
    "Issuer": "OpenSilver.AuthService",
    "Audience": "OpenSilver.Clients",
    "Hours": 2
  },
  "Google": {
    "ClientId": "Client ID generated from Google Cloud Console"
  }
}
```

## Pre-Execution Checklist

### Google Cloud Console Setup
1. Create OAuth Client ID in [Google Cloud Console](https://console.cloud.google.com)
2. Select Application type: "Web application"
3. Add `https://localhost:55922` to **Authorized JavaScript origins**
4. Apply the generated Client ID to `appsettings.json` and `index.html`

### Required Configuration Verification
- [ ] Google Client ID correctly applied to all configuration files
- [ ] JWT key is at least 32 characters long
- [ ] CORS configuration has correct client URL

### Execution and Debugging
1. Run backend and client simultaneously
2. Observe authentication process in browser developer tools:

```javascript
// Check status in console
console.log("Auth Config:", window.AUTH_CFG);
console.log("Google Object:", window.google);
console.log("Current Token:", window.Auth.getToken());
```

### Common Issues
- **"unregistered_origin"**: Check JavaScript origins in Google Cloud Console
- **CORS error**: Verify client URL in backend CORS configuration
- **JWT key length error**: Ensure JWT key is at least 32 bytes
- **Token time error**: Check system time synchronization status
