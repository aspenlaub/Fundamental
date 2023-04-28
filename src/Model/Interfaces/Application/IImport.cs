using System.Threading.Tasks;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;

public interface IImport {
    bool WasDataEntered();
    Task<bool> IsThereAnythingToImportAsync();
    Task ImportAsync();
}