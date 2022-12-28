using Newtonsoft.Json;
using Nexus.Permissions.Export.Properties;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using Process = System.Diagnostics.Process;

namespace Nexus.Permissions.Export.Base;
internal class Authenticator : IDisposable
{
    private OAuthSettings _settings;
    private HttpListener _server;
    private AccessToken? _access;
    private string baseOAuth
        => $"https://login.microsoftonline.com/organizations/oauth2/v2.0/";
    public Authenticator(OAuthSettings settings)
    {
        _settings = settings;
        _server = new HttpListener();
        _server.Prefixes.Add(settings.RedirectUrl);
        _server.Start();
    }
    public void RequestLogin()
    {
        string url = $"{baseOAuth}authorize?grant_type=authorization_code" +
            $"&client_id={HttpUtility.UrlEncode(_settings.ClientId)}" +
            $"&response_type=code" +
            $"&response_mode=query" +
            $"&scope={GetScopes(_settings.GraphUserScopes)}" +
            $"&state=12345";

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true,
            Verb = "open"
        });

        _access = null;
    }
    public async Task<AccessToken?> AwaitLoginAsync()
    {
        while (_access == null)
        {
            HttpListenerContext ctx = _server.GetContext();
            HttpListenerRequest resquet = ctx.Request;

            using HttpListenerResponse resp = ctx.Response;
            var query = HttpUtility.ParseQueryString(resquet.Url?.Query ?? "");
            string code = query["code"] ?? string.Empty;

            if (string.IsNullOrEmpty(code))
            {
                resp.StatusCode = (int)HttpStatusCode.BadRequest;
                resp.StatusDescription = "Request is bad";
                continue;
            }

            Send(resp);

            string content = "grant_type=authorization_code" +
                $"&client_id={HttpUtility.UrlEncode(_settings.ClientId)}" +
                $"&scope={GetScopes(_settings.GraphUserScopes)}" +
                $"&code={HttpUtility.UrlEncode(code)}";

            HttpClient httpClient = new();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseOAuth}token")
            {
                Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            _access = JsonConvert.DeserializeObject<AccessToken>(body);
            return _access;
        }

        return null;
    }
    public async Task<AccessToken> RefreshLoginAsync(string refreshToken)
    {
        string content = "grant_type=refresh_token" +
                        $"&client_id={HttpUtility.UrlEncode(_settings.ClientId)}" +
                        $"&scope={GetScopes(_settings.GraphUserScopes)}" +
                        $"&refresh_token={HttpUtility.UrlEncode(refreshToken)}";

        HttpClient httpClient = new();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseOAuth}token")
        {
            Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        var response = await httpClient.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();

        _access = JsonConvert.DeserializeObject<AccessToken>(body);
        return _access;
    }
    private static string GetScopes(string[] graphScopes)
    {
        string scopes = string.Empty;

        foreach (var item in graphScopes)
            scopes += item + "%20";

        scopes = scopes.Remove(scopes.Length - 3, 3);

        return scopes;
    }
    public void Dispose()
    {
        _server.Stop();
    }
    private void Send(HttpListenerResponse resp)
    {
        byte[] body = File.ReadAllBytes(Path.GetFullPath(@"Resources\AutoClose.html"));
        resp.StatusCode = (int)HttpStatusCode.OK;
        resp.ContentType = "text/html; charset=utf-8";
        resp.OutputStream.Write(body, 0, body.Length);
    }
}

internal class AccessToken
{
    public string Token_type { get; set; }
    public string Access_token { get; set; }
    public string Refresh_token { get; set; }
    public string email { get; set; }
    public int Expires_in { get; set; }
}