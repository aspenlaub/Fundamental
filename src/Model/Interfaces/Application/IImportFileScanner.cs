using System.Threading.Tasks;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;

public interface IImportFileScanner {
    Task<bool> AnythingToImportAsync();
    Task CopyInputFilesToEnvironmentsAsync();
}