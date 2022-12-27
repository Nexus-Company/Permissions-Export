namespace Nexus.Permissions.Export.Models;

public class Member
{
    public string DisplayName { get; set; }
    public string Id { get; set; }
    public string Email { get; set; }

    public Member(string displayName, string id, string email)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Email = email ?? throw new ArgumentNullException(nameof(email));
    }
}