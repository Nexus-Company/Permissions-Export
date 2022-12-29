using Microsoft.Graph;
using Nexus.Permissions.Export.Base;
using Nexus.Permissions.Export.Properties;
using OfficeOpenXml;
using System.Diagnostics;
using System.Net.Http.Headers;
using AccessToken = Nexus.Permissions.Export.Base.AccessToken;
using Process = System.Diagnostics.Process;

namespace Nexus.Permissions.Export;
public class Program
{
    static AccessToken accessToken;
    public static async Task Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var oauth = Settings.LoadSettings().OAuth;
        var authenticator = new Authenticator(oauth);
        authenticator.RequestLogin();
        Console.WriteLine("Esperando retorno da autenticação...");
        accessToken = await authenticator.AwaitLoginAsync();

        var client = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(accessToken.Token_type, accessToken.Access_token);
            await Task.CompletedTask;
        }));

        while (true)
        {
            try
            {
                Console.Clear();
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

                Console.WriteLine("Aperte qualquer tecla para continuar...");

                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Modifiers == ConsoleModifiers.Control &&
                    (key.KeyChar == 'c' || key.KeyChar == 'C'))
                    break;
            }
            catch (Exception ex)
            {

            }
        }
    }
}