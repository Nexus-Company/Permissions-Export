using System.ComponentModel.DataAnnotations;

namespace Nexus.Permissions.Export.Models.Enums;
public enum Roles
{
    [Display(Name = "Proprietário")]
    Owner,
    [Display(Name = "Leitura")]
    Read,
    [Display(Name = "Colaboração")]
    Write
}
