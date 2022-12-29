using Microsoft.Graph;
using Nexus.Permissions.Export.Models;
using PermissionType = Nexus.Permissions.Export.Models.Enums.PermissionType;

namespace Nexus.Permissions.Export;

internal class GraphHelper
{
    private const string sharepointHost = ".sharepoint.com";
    private readonly GraphServiceClient _userClient;
    private readonly List<User> users = new();
    private readonly List<Group> groups = new();
    private readonly List<SharePointIdentity> spGroup = new();
    public GraphHelper(GraphServiceClient client)
    {
        _userClient = client;
    }

    #region Membros Publicos
    public async Task<Tuple<string, string>> GetSiteAsync()
    {
        bool validEntry = false;
        string? endpoint = null;

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

            if (string.IsNullOrEmpty(builder.Path))
                endpoint = builder.Host.Trim() + sharepointHost;

            validEntry = true;
        }

        Console.ForegroundColor = ConsoleColor.White;

        if (new UriBuilder(endpoint).Host != endpoint)
        {
            string id = await GetSiteIdByUrlAsync(new Uri(endpoint));

            if (!string.IsNullOrEmpty(id))
                return (id, endpoint).ToTuple();
        }

        endpoint ??= string.Empty;
        endpoint = endpoint.Contains(sharepointHost) ? endpoint : endpoint + sharepointHost;

        return await GetSiteAsync(endpoint);
    }

    public async Task<Library?> GetListAsync(Tuple<string, string> site)
    {
        ShowSite(site.Item2);

        Console.WriteLine(" 0 - Listar de todas Bibliotecas \n 1 - Listar de uma Biblioteca\n");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Escolha uma opção: ");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        bool list = GetInt(0, 1) == 1;
        Console.ForegroundColor = ConsoleColor.White;

        if (list)
            return await GetListAsync(site, null);

        return null;
    }

    public async Task<LibraryPermission[]> GetPermissionsAsync(Tuple<string, string> site, Library? libary)
    {
        if (libary != null)
            return await GetPermissionsByLibaryAsync(site, libary);
        else
            return await GetPermissionsAsync(site);
    }
    #endregion

    #region Auxiliares Graph
    private async Task<LibraryPermission[]> GetPermissionsAsync(Tuple<string, string> site)
    {
        List<LibraryPermission> permissionsList = new();
        List<Library> libariesList = new();
        var libaries = await _userClient.Sites[site.Item1].Drives.Request().GetAsync();

        libariesList.AddRange(libaries.Select(lib => Library.ToLibrary(lib)));

        while (libaries.NextPageRequest != null)
        {
            libaries = await libaries.NextPageRequest.GetAsync();

            libariesList.AddRange(libaries.Select(lib => Library.ToLibrary(lib)));
        }

        for (int i = 0; i < libariesList.Count; i++)
        {
            Library libary = libariesList[i];
            permissionsList.AddRange(await GetPermissionsByLibaryAsync(site, libary));

            ShowSite(site.Item2);
            ShowProgressBar("Obtendo Bibliotecas", 100 / libariesList.Count * (i + 1));
        }

        return permissionsList.ToArray();
    }
    private async Task<LibraryPermission[]> GetPermissionsByLibaryAsync(Tuple<string, string> site, Library libary)
    {
        List<Permission> permissionsList = new();
        var permissions = await _userClient.Sites[site.Item1].Drives[libary.Id].Root.Permissions.Request().GetAsync();
        permissionsList.AddRange(permissions.ToArray());

        while (permissions.NextPageRequest != null)
        {
            permissions = await permissions.NextPageRequest.GetAsync();
            permissionsList.AddRange(permissions.ToArray());
        }

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

                var membersRequest = await _userClient.Groups[permission.GrantedToV2.Group.Id].Members.Request().GetAsync();

                members.AddRange(await MembersRequestToMembers(membersRequest));

                while (membersRequest.NextPageRequest != null)
                {
                    membersRequest = await membersRequest.NextPageRequest.GetAsync();

                    members.AddRange(await MembersRequestToMembers(membersRequest));
                }
            }
            else if (permission.GrantedToV2.User != null)
            {
                type = PermissionType.User;
                Identity id = permission.GrantedToV2.User;
                string mail = id.AdditionalData["email"]?.ToString() ?? string.Empty;
                to = id.DisplayName;

                if (!string.IsNullOrEmpty(id.Id))
                {
                    User? user = users.FirstOrDefault(fs => fs.Id == permission.GrantedToV2.User.Id);

                    if (user == null)
                    {
                        user = await _userClient.Users[id.Id].Request().GetAsync();
                        users.Add(user);
                    }
                }

                members.Add(new Member(id.DisplayName, id.Id, mail));
            }
            else if (permission.GrantedToV2.SiteUser != null)
            {
                type = PermissionType.Sharepoint;
                to = permission.GrantedToV2.SiteUser.DisplayName;
            }

            libraryPermissions[i] = new LibraryPermission(libary, to, permission.Roles, type, members.ToArray());

            ShowSite(site.Item2);
            ShowProgressBar("Obtendo Membros", 100 / permissionsList.Count * (i + 1));
        }

        return libraryPermissions;
    }
    private async Task<Library> GetListAsync(Tuple<string, string> site, ISiteDrivesCollectionRequest? nextPage = null)
    {
        ISiteDrivesCollectionPage drives;

        if (nextPage == null)
            drives = await _userClient.Sites[site.Item1].Drives.Request().GetAsync();
        else
            drives = await nextPage.GetAsync();

        ShowSite(site.Item2);

        for (int i = 0; i < drives.Count; i++)
            Console.WriteLine($" {i} - {drives[i].Name}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"\nEscolha uma Biblioteca: ");
        Console.ForegroundColor = ConsoleColor.DarkGray;

        int item = GetPageOrItem(out bool? next, drives.NextPageRequest != null, 0, drives.Count);
        Console.ForegroundColor = ConsoleColor.White;
        if (next ?? false)
            return await GetListAsync(site, nextPage);

        return Library.ToLibrary(drives[item]);
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

        int item = GetPageOrItem(out bool? next, drives.NextPageRequest != null, 0, drives.Count);
        Console.ForegroundColor = ConsoleColor.White;
        if (next ?? false)
            return await GetSiteAsync(endpoint, nextPage);

        return (drives[item].Id, drives[item].WebUrl).ToTuple();
    }
    private async Task<string> GetSiteIdByUrlAsync(Uri url)
    {
        string id = string.Empty;

        try
        {
            id = (await _userClient.Sites
                .GetByPath(url.LocalPath, url.Host)
                .Request()
                .GetAsync()).Id;
        }
        catch (Exception)
        {
        }

        return id;
    }

    private async Task<Member[]> MembersRequestToMembers(IGroupMembersCollectionWithReferencesPage membersRequest)
    {
        Member[] members = new Member[membersRequest.Count];

        for (int i = 0; i < membersRequest.Count; i++)
        {
            var member = membersRequest[i];
            User? user = users.FirstOrDefault(fs => fs.Id == member.Id);

            if (user == null)
            {
                user = await _userClient.Users[member.Id].Request().GetAsync();
                users.Add(user);
            }

            members[i] = new Member(user.DisplayName, user.Id, user.Mail);
        }

        return members;
    }
    #endregion

    #region Auxiliares Console
    private static void ShowSite(string site)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"Site Selecionado: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{site} \n");
        Console.ForegroundColor = ConsoleColor.DarkGray;
    }
    private static int GetInt(int min = 0, int max = int.MaxValue)
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
    private static int GetPageOrItem(out bool? next, bool nextPage = false, int min = 0, int max = int.MaxValue)
    {
        next = null;

        while (true)
        {
            string entry = Console.ReadLine()?.ToLowerInvariant().Trim() ?? string.Empty;

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
    private static void ShowProgressBar(string text, int value)
    {
        Console.ForegroundColor = ConsoleColor.White;
        int progress = value * 50 / 100;
        Console.Write("\r{0}: ", text);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("[");
        for (int j = 0; j < progress; j++)
        {
            Console.Write("=");
        }
        for (int j = progress; j < 50; j++)
        {
            Console.Write(" ");
        }
        Console.WriteLine("] {0}%\n", value);
        Console.ForegroundColor = ConsoleColor.White;
    }
    #endregion
}
