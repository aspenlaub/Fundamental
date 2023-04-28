using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class ImportCommand : IApplicationCommand {
    private readonly IImport _ContextOwner;

    public bool MakeLogEntries => true;
    public string Name => Properties.Resources.ImportCommandName;

    public ImportCommand(IImport contextOwner) {
        _ContextOwner = contextOwner;
    }

    public async Task<bool> CanExecuteAsync() {
        return !_ContextOwner.WasDataEntered() && await _ContextOwner.IsThereAnythingToImportAsync();
    }

    public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        await _ContextOwner.ImportAsync();
    }
}