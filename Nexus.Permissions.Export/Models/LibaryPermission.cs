using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Permissions.Export.Models.Enums;

namespace Nexus.Permissions.Export.Models;
public class LibraryPermission
{
    public string Library { get; set; }
    public string To { get; set; }
    public PermissionType Type { get; set; }
    public Member[] Members { get; set; }
    public Roles[] Roles { get; set; }

    public LibraryPermission(string library, string to, IEnumerable<string> roles, PermissionType type, Member[] members)
    {
        Library = library;
        To = to;
        Type = type;
        Members = members;
        Roles = roles.Select(r => Enum.Parse<Roles>(r, true)).ToArray();
    }

    public LibraryPermission(string library, string to, string[] roles, PermissionType type, Member member)
        : this(library, to, roles, type, new[] { member })
    {
    }

    public LibraryPermission(string library, string to, string[] roles, PermissionType type)
        : this(library, to, roles, type, Array.Empty<Member>())
    {
    }
}