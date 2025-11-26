using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class AddTransactionCommand(IAddTransaction contextOwner) : IApplicationCommand {
    protected IAddTransaction ContextOwner = contextOwner;

    public bool MakeLogEntries => true;
    public string Name => Properties.Resources.SaveCommandName;

    public async Task<bool> CanExecuteAsync() {
        if (!ContextOwner.IsSecurityInFocus()) { return false; }

        return await Task.FromResult(!ContextOwner.IsAnInertTransactionPresent());
    }

    public Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        return Task.Run(() => {
            ContextOwner.AddTransaction();
        });
    }
}