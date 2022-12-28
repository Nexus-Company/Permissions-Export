using Microsoft.Graph;

namespace Nexus.Permissions.Export.Models;

public class Library
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Url { get; set; }

    public Library(string id, string displayName, string url)
    {
        Id = id;
        DisplayName = displayName;
        Url = url;
    }

    public static Library ToLibrary(Drive libary)
        => new(libary.Id, libary.Name, libary.WebUrl);
}