using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class UndoCommand(IUndo contextOwner) : IApplicationCommand {
    public bool MakeLogEntries => true;
    public string Name => Properties.Resources.UndoCommandName;
    public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

    public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        await contextOwner.UndoAsync();
    }
}