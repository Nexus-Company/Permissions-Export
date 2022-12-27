using Microsoft.Graph;
using Nexus.Permissions.Export.Base;
using Nexus.Permissions.Export.Properties;
using System.Diagnostics;
using System.Net.Http.Headers;
using AccessToken = Nexus.Permissions.Export.Base.AccessToken;
using Process = System.Diagnostics.Process;

namespace Nexus.Permissions.Export;
public class Program
{
    static AccessToken accessToken;
    static async Task Main(string[] args)
    {
        var oauth = Settings.LoadSettings().OAuth;
        var authenticator = new Authenticator(oauth);
        authenticator.RequestLogin();
        accessToken = await authenticator.AwaitLoginAsync();

        var client = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(accessToken.Token_type, accessToken.Access_token);
            await Task.CompletedTask;
        }));

        while (true)
        {
            var helper = new GraphHelper(client);
            var site = await helper.GetSiteAsync();
            var list = await helper.GetListAsync(site);
            var obj = await helper.GetPermissionsAsync(site, list);
            string path = Path.Combine($@"Results\{DateTime.Now:dd-MM-yyyy HH-mm}.xlsx");

            obj.SaveInXlsx(path, true);

            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = path
            });

            path = Console.ReadLine() ?? string.Empty;

            if (path == "exit")
                break;
        }
    }
}