(function () {
    const CFG = window.AUTH_CFG;
    const KEY = CFG.storageKey || "accessToken";

    function login() {
        console.log("Redirecting to Google login page...");
        const params = new URLSearchParams({
            client_id: CFG.googleClientId,
            redirect_uri: window.location.origin,
            response_type: 'code',
            scope: 'email profile openid',
            access_type: 'offline',
            prompt: 'select_account'
        });
        window.location.href = `https://accounts.google.com/o/oauth2/auth?${params}`;
    }

    function logout() {
        localStorage.removeItem(KEY);
        window.dispatchEvent(new CustomEvent("auth:changed"));
        console.log("Logged out");
    }

    function getToken() {
        return localStorage.getItem(KEY) || "";
    }

    function init() {
        console.log("Auth module initializing...");
    }

    window.Auth = {
        login,
        logout,
        getToken,
        init
    };

    console.log("Auth module loaded");
})();