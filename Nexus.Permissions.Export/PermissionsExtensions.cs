using Nexus.Permissions.Export.Models;
using Nexus.Permissions.Export.Models.Enums;
using OfficeOpenXml;
using System.Drawing;
using Directory = System.IO.Directory;

namespace Nexus.Permissions.Export;

internal static class PermissionsExtensions
{
    public static void SaveInXlsx(this IEnumerable<LibraryPermission> permissions, string path, bool oneFile = false)
    {
        if (permissions == null)
            throw new ArgumentNullException(nameof(permissions));

        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (!Directory.Exists(Path.GetDirectoryName(path)))
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Path.GetFullPath("Results"));

        // Cria um novo arquivo XLSX usando o EPPlus
        using var package = new ExcelPackage();
        // Adiciona uma nova planilha ao arquivo
        var worksheet = package.Workbook.Worksheets.Add("Permissões");
        // Define os títulos das colunas na primeira linha da planilha
        worksheet.Cells[1, 1].Value = "Biblioteca";
        worksheet.Cells[1, 2].Value = "Permisão para";
        worksheet.Cells[1, 3].Value = "Tipo";
        worksheet.Cells[1, 4].Value = "Nome Membro";
        worksheet.Cells[1, 5].Value = "E-mail Membro";
        worksheet.Cells[1, 6].Value = "Acesso";
        var namedStyle = worksheet.Workbook.Styles.CreateNamedStyle("HyperLink");
        namedStyle.Style.Font.UnderLine = true;
        namedStyle.Style.Font.Color.SetColor(Color.Blue);

        permissions = permissions.OrderBy(perm => perm.Library.DisplayName);
        int aditional = 1;

        for (int i = 1; i <= permissions.Count(); i++)
        {
            var permission = permissions.ElementAt(i - 1);
            int rowIndex = i + aditional;
            // Adiciona as funções como uma lista separada por vírgulas
            var roles = string.Join(", ", permission.Roles.Select(r => r.ToString()));

            if (permission.Type!= PermissionType.User)
            {
                worksheet.Cells[rowIndex, 1].DefineByLibary(permission.Library);
                worksheet.Cells[rowIndex, 2].Value = permission.To;
                worksheet.Cells[rowIndex, 3].Value = permission.Type.ToString();
                worksheet.Cells[rowIndex, 6].Value = roles;
            }
            else
            {
                aditional--;
            }

            for (int x = 0; x < permission.Members.Length; x++)
            {
                aditional++;
                rowIndex = i + aditional;
                var member = permission.Members[x];

                worksheet.Cells[rowIndex, 1].DefineByLibary(permission.Library);
                worksheet.Cells[rowIndex, 2].Value = permission.To;
                worksheet.Cells[rowIndex, 3].Value = permission.Type.ToString();
                worksheet.Cells[rowIndex, 4].Value = member.DisplayName;
                worksheet.Cells[rowIndex, 5].Value = member.Email;
                worksheet.Cells[rowIndex, 6].Value = roles;
            }
        }

        // Adiciona a tabela
        worksheet.Tables.Add(new ExcelAddressBase(1, 1, permissions.Count() + aditional, 6), "Permissões");

        // Salva o arquivo XLSX no caminho especificado
        try
        {
            package.SaveAs(new FileInfo(path));
        }
        catch (IOException ex)
        {
            // O arquivo já está sendo usado por outro processo
            Console.WriteLine("Ocorreu um erro ao salvar o arquivo: " + ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            // O usuário não tem permissão para escrever no caminho especificado
            Console.WriteLine("Ocorreu um erro ao salvar o arquivo: " + ex.Message);
        }
    }

    private static void DefineByLibary(this ExcelRange libaryCell, Library library)
    {
        libaryCell.Value = library.DisplayName;
        libaryCell.Hyperlink = new Uri(library.Url);
        libaryCell.StyleName = "HyperLink";
    }
}