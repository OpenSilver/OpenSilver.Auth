# OpenSilver Google OAuth Authentication Demo

A complete example implementing Google OAuth authentication between an OpenSilver client and ASP.NET Core backend.

<img width="49%" height="804" alt="a22d4ebf347d2ce81b797498bc331e94" src="https://github.com/user-attachments/assets/8900f555-e51b-451a-bd7a-b83009a52e59" />
<img width="49%" height="848" alt="16d140f85c1d7fdfae6c7616f8eeee26" src="https://github.com/user-attachments/assets/106b2605-3a56-42b8-a205-7822acecc26a" />


## Key Features
- Google account login (Authorization Code Flow)
- JWT token-based API security
- Configuration file-based setup

## Project Structure

```
OpenSilverAuthClient/
├── OpenSilverAuthClient.Browser/       # Client (OpenSilver)
│   ├── wwwroot/
│   │   ├── index.html                  # Google OAuth config and callback handling
│   │   └── libs/auth.js                # Authentication logic
│   └── AuthBar.xaml                    # Login UI
├── OpenSilverAuthClient.Server/        # Server (ASP.NET Core)
│   ├── Program.cs                      # Backend authentication logic
│   └── appsettings.json                # JWT and Google settings
```

## Authentication Flow

1. User clicks "Login with Google" button
2. Redirects to Google login page
3. After login completion, returns to site with authorization code
4. Client sends code to backend API
5. Backend exchanges code for access token and generates JWT token

## Client Implementation

### `wwwroot/index.html`
```javascript
// Google OAuth configuration (change to actual values)
window.AUTH_CFG = {
    apiBase: "https://localhost:7224",  // Change to actual backend API server address
    googleClientId: "YOUR_GOOGLE_CLIENT_ID",  // Change to client ID generated from Google Cloud Console
    storageKey: "accessToken"
};

// Authorization code callback handling
const code = new URLSearchParams(window.location.search).get('code');
if (code) {
    fetch(window.AUTH_CFG.apiBase + "/auth/google", {
        method: "POST",
        body: JSON.stringify({ code: code })
    })
    .then(response => response.json())
    .then(data => localStorage.setItem(window.AUTH_CFG.storageKey, data.accessToken));
}
```

### `wwwroot/libs/auth.js`
```javascript
function login() {
    const params = new URLSearchParams({
        client_id: CFG.googleClientId,
        redirect_uri: window.location.origin,  // Automatically uses current page origin
        response_type: 'code',
        scope: 'email profile openid',
        prompt: 'select_account'
    });
    window.location.href = `https://accounts.google.com/o/oauth2/auth?${params}`;
}
```

## Server Implementation

### `Program.cs`
Core logic for exchanging authorization code to JWT token:

```csharp
// Load configuration values
var googleCid = cfg["Google:ClientId"]!;
var googleSecret = cfg["Google:ClientSecret"]!;
var clientOrigin = cfg["Client:Origin"]!;

// Google OAuth → JWT token exchange
app.MapPost("/auth/google", async (GoogleExchange body, IHttpClientFactory httpClientFactory) => {
    // 1. Exchange authorization code for access token
    var tokenRequest = new FormUrlEncodedContent(new[] {
        new KeyValuePair<string, string>("client_id", googleCid),
        new KeyValuePair<string, string>("client_secret", googleSecret),
        new KeyValuePair<string, string>("code", body.code),
        new KeyValuePair<string, string>("redirect_uri", clientOrigin)
    });
    
    // 2. Get user info and generate JWT token
    return Results.Ok(new { accessToken = jwtToken });
});

record GoogleExchange(string code);
```

### `appsettings.json`
```json
{
  "Jwt": {
    "Key": "minimum-32-character-secret-key-required",
    "Issuer": "OpenSilver.AuthService",
    "Audience": "OpenSilver.Clients",
    "Hours": 2
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "Client": {
    "Origin": "http://localhost:55592"
  }
}
```

## Pre-Execution Setup

### Google Cloud Console Configuration
1. Create OAuth Client ID in [Google Cloud Console](https://console.cloud.google.com)
2. Select Application type: **"Web application"**
3. Add client address to **Authorized redirect URIs** (e.g., `http://localhost:55592`)
4. Apply generated **Client ID** and **Client Secret** to configuration files

### Required Configuration Checklist
- [ ] Google Client ID and Secret applied to configuration files
- [ ] JWT key is at least 32 characters long
- [ ] Client address matches server configuration

### Common Issues
- **"redirect_uri_mismatch"**: Check redirect URI in Google Console matches actual client address
- **CORS error**: Verify `Client:Origin` setting in server configuration
- **JWT key length error**: Ensure JWT key is at least 32 characters long
