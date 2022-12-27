using Microsoft.Graph;
using Nexus.Permissions.Export.Base;
using Nexus.Permissions.Export.Properties;
using System.Net.Http.Headers;
using AccessToken = Nexus.Permissions.Export.Base.AccessToken;

namespace Nexus.Permissions.Export;
internal class Program
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

        var helper = new GraphHelper(client);
        var site = await helper.GetSiteAsync();
        var list = await helper.GetListAsync(site);
        var obj = await helper.GetPermissionsAsync(site, list);
    }

    public static int GetInt(int min = 0, int max = int.MaxValue)
    {
        while (true)
        {
            string entry = Console.ReadLine();

            if (string.IsNullOrEmpty(entry)) continue;
            if (int.TryParse(entry, out int value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine($"O valor de entrada deve estar entre '{min}' e '{max}'.");
        }
    }

    public static int GetPageOrItem(out bool? next, bool nextPage = false, int min = 0, int max = int.MaxValue)
    {
        next = null;

        while (true)
        {
            string entry = Console.ReadLine()?.ToLowerInvariant().Trim();

            if (string.IsNullOrEmpty(entry)) continue;
            if (entry == "n")
            {
                next = true;
                if (!nextPage)
                {
                    Console.WriteLine("Não existe próxima página.");
                    continue;
                }
                return -1;
            }

            if (int.TryParse(entry, out int value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine($"O valor de entrada deve estar entre '{min}' e '{max}'.");
        }
    }
}