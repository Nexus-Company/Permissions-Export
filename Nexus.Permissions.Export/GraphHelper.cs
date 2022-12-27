using Microsoft.Graph;
using Nexus.Permissions.Export.Models;
using Nexus.Permissions.Export.Models.Enums;
using PermissionType = Nexus.Permissions.Export.Models.Enums.PermissionType;

namespace Nexus.Permissions.Export;

internal class GraphHelper
{
    private const string sharepointHost = ".sharepoint.com";
    private readonly GraphServiceClient _userClient;
    public GraphHelper(GraphServiceClient client)
    {
        _userClient = client;
    }

    public async Task<Tuple<string, string>> GetSiteAsync()
    {
        bool validEntry = false;
        string? endpoint = null, site = null;

        while (!validEntry)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Digite o endpoint: ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            endpoint = Console.ReadLine();

            if (string.IsNullOrEmpty(endpoint))
                continue;

            UriBuilder builder = new(endpoint);

            endpoint = builder.Host.Trim();

            validEntry = true;
        }

        Console.ForegroundColor = ConsoleColor.White;
        endpoint ??= string.Empty;
        endpoint = endpoint.Contains(sharepointHost) ? endpoint : endpoint + sharepointHost;

        return await GetSiteAsync(endpoint);
    }

    public async Task<string?> GetListAsync(Tuple<string, string> site)
    {
        showSite(site.Item2);

        Console.WriteLine(" 0 - Listar de todas Bibliotecas \n 1 - Listar de uma Biblioteca\n");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Escolha uma opção: ");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        bool list = Program.GetInt(0, 1) == 1;
        Console.ForegroundColor = ConsoleColor.White;

        if (list)
            return await GetListAsync(site, null);

        return null;
    }

    public async Task<LibraryPermission[]> GetPermissionsAsync(Tuple<string, string> site, string? libary)
    {
        if (libary != null)
            return await GetPermissionsByLibaryAsync(site.Item1, libary);
        else
            return await GetPermissionsAsync(site.Item1);
    }

    private async Task<LibraryPermission[]> GetPermissionsByLibaryAsync(string site, string libary)
    {
        List<Permission> permissionsList = new();
        var permissions = await _userClient.Sites[site].Drive.Items[libary].Permissions.Request().GetAsync();

        permissionsList.AddRange(permissions.ToArray());

        while (permissions.NextPageRequest != null)
        {
            permissions = await permissions.NextPageRequest.GetAsync();
            permissionsList.AddRange(permissions.ToArray());
        }

        List<User> users = new();
        List<Group> groups = new();
        List<SharePointIdentity> spGroup = new();
        LibraryPermission[] libraryPermissions = new LibraryPermission[permissionsList.Count];

        for (int i = 0; i < permissionsList.Count; i++)
        {
            Permission permission = permissionsList[i];
            PermissionType type = 0;
            List<Member> members = new();
            string to = string.Empty;

            if (permission.GrantedToV2.SiteGroup != null)
            {
                type = PermissionType.Sharepoint;
                to = permission.GrantedToV2.SiteGroup.LoginName;
            }
            else if (permission.GrantedToV2.Group != null)
            {
                type = PermissionType.Domain;
                to = permission.GrantedToV2.Group.DisplayName;

                var group = await _userClient.Groups[permission.GrantedToV2.Group.Id].Request().GetAsync();

                if (groups.FirstOrDefault(gp => gp.Id == group.Id) == null)
                    groups.Add(group);

                var membersList = await _userClient.Groups[permission.GrantedToV2.Group.Id].Members.Request().GetAsync();

                while (membersList.NextPageRequest != null)
                {
                    membersList = await membersList.NextPageRequest.GetAsync();

                    foreach (var item in membersList)
                    {
                        User? user = users.FirstOrDefault(fs => fs.Id == item.Id);

                        if (user == null)
                        {
                            user = await _userClient.Users[item.Id].Request().GetAsync();
                            users.Add(user);
                        }

                        members.Add(new Member(user.DisplayName, user.Id, user.Mail));
                    }
                }
            }
            else if (permission.GrantedToV2.User != null)
            {
                type = PermissionType.User;
                Identity id = permission.GrantedToV2.User;
                to = permission.GrantedToV2.User.DisplayName;

                User? user = users.FirstOrDefault(fs => fs.Id == permission.GrantedToV2.User.Id);

                if (user == null)
                {
                    user = await _userClient.Users[id.Id].Request().GetAsync();
                    users.Add(user);
                }

                members.Add(new Member(user.DisplayName, user.Id, user.Mail));
            }

            libraryPermissions[i] = new LibraryPermission(libary, to, permission.Roles, type, members.ToArray());
        }

        return libraryPermissions;
    }
    private async Task<LibraryPermission[]> GetPermissionsAsync(string site)
    {
        List<LibraryPermission> permissionsList = new();
        var libaries = await _userClient.Sites[site].Drives.Request().GetAsync();

        foreach (var libary in libaries)
            permissionsList.AddRange(await GetPermissionsByLibaryAsync(site, libary.Id));

        while (libaries.NextPageRequest != null)
        {
            libaries = await libaries.NextPageRequest.GetAsync();

            foreach (var libary in libaries)
                permissionsList.AddRange(await GetPermissionsByLibaryAsync(site, libary.Id));
        }

        return permissionsList.ToArray();
    }

    private async Task<string> GetListAsync(Tuple<string, string> site, ISiteDrivesCollectionRequest? nextPage = null)
    {
        ISiteDrivesCollectionPage drives;

        if (nextPage == null)
            drives = await _userClient.Sites[site.Item1].Drives.Request().GetAsync();
        else
            drives = await nextPage.GetAsync();

        showSite(site.Item2);

        for (int i = 0; i < drives.Count; i++)
            Console.WriteLine($" {i} - {drives[i].Name}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"\nEscolha uma Biblioteca: ");
        Console.ForegroundColor = ConsoleColor.DarkGray;

        int item = Program.GetPageOrItem(out bool? next, drives.NextPageRequest != null, 0, drives.Count);
        Console.ForegroundColor = ConsoleColor.White;
        if (next ?? false)
            return await GetListAsync(site, nextPage);

        return drives[item].Id;
    }

    private async Task<Tuple<string, string>> GetSiteAsync(string endpoint, ISiteSitesCollectionRequest? nextPage = null)
    {
        ISiteSitesCollectionPage drives;
        Console.WriteLine();

        if (nextPage == null)
            drives = await _userClient.Sites[endpoint].Sites.Request(new List<QueryOption>()
            {
                new QueryOption("search", "*")
            }).GetAsync();
        else
            drives = await nextPage.GetAsync();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (int i = 0; i < drives.Count; i++)
            Console.WriteLine($" {i} - {drives[i].Name}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"\nEscolha uma Site: ");
        Console.ForegroundColor = ConsoleColor.DarkGray;

        int item = Program.GetPageOrItem(out bool? next, drives.NextPageRequest != null, 0, drives.Count);
        Console.ForegroundColor = ConsoleColor.White;
        if (next ?? false)
            return await GetSiteAsync(endpoint, nextPage);

        return (drives[item].Id, drives[item].WebUrl).ToTuple();
    }

    private static void showSite(string site)
    {
        Console.Clear();
        Console.Write($"Site Selecionado: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{site} \n");
        Console.ForegroundColor = ConsoleColor.DarkGray;
    }
}
