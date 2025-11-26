using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class RefreshContextCommand(IRefreshContext contextOwner) : IApplicationCommand {
    protected IRefreshContext ContextOwner = contextOwner;

    public bool MakeLogEntries => false;
    public string Name => Properties.Resources.RefreshContextCommandName;
    public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

    public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        await ContextOwner.RefreshContextAsync();
    }
}