using Microsoft.Graph;
using Nexus.Permissions.Export.Models;
using OfficeOpenXml;

namespace Nexus.Permissions.Export;

internal static class PermissionsExtensions
{
    public static void SaveInXlsx(this IEnumerable<LibraryPermission> permissions, string path, bool oneFile = false)
    {
        if (permissions == null)
            throw new ArgumentNullException(nameof(permissions));

        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        // Cria um novo arquivo XLSX usando o EPPlus
        using var package = new ExcelPackage();

        foreach (var permGroup in permissions.GroupBy(pem => pem.Library))
        {
            var array = permGroup.ToArray();
            // Adiciona uma nova planilha ao arquivo
            var worksheet = package.Workbook.Worksheets.Add(permGroup.Key);
            
            // Define os títulos das colunas na primeira linha da planilha
            worksheet.Cells[1, 1].Value = "Permisão para";
            worksheet.Cells[1, 2].Value = "Tipo";
            worksheet.Cells[1, 3].Value = "Membros";
            worksheet.Cells[1, 4].Value = "Roles";

            // Adiciona os dados das permissões à planilha, começando na linha 2
            for (int i = 0; i < array.Length; i++)
            {
                var permission = array[i];
                int rowIndex = i + 2;
                worksheet.Cells[rowIndex, 1].Value = permission.To;
                worksheet.Cells[rowIndex, 2].Value = permission.Type.ToString();

                // Adiciona os membros como uma lista separada por vírgulas
                var members = string.Join(", ", permission.Members.Select(m => m.DisplayName));
                worksheet.Cells[rowIndex, 3].Value = members;

                // Adiciona as funções como uma lista separada por vírgulas
                var roles = string.Join(", ", permission.Roles.Select(r => r.ToString()));
                worksheet.Cells[rowIndex, 4].Value = roles;
            }
        }

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
}