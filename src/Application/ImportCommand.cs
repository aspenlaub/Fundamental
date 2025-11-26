using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class ImportCommand(IImport contextOwner) : IApplicationCommand {
    public bool MakeLogEntries => true;
    public string Name => Properties.Resources.ImportCommandName;

    public async Task<bool> CanExecuteAsync() {
        return !contextOwner.WasDataEntered() && await contextOwner.IsThereAnythingToImportAsync();
    }

    public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        await contextOwner.ImportAsync();
    }
}