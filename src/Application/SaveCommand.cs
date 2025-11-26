using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class SaveCommand(ISave contextOwner) : IApplicationCommand {
    protected ISave ContextOwner = contextOwner;

    public bool MakeLogEntries => true;
    public string Name => Properties.Resources.SaveCommandName;
    public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

    public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        await ContextOwner.SaveAsync();
    }
}