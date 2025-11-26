using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class RefreshChartCommandBase(Charts chart, IRefreshChart owner) : IApplicationCommand {
    protected Charts Chart = chart;
    protected IRefreshChart Owner = owner;

    public bool MakeLogEntries => false;
    public string Name => Properties.Resources.RefreshChartCommandName;
    public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

    public Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        return Task.Run(() => {
            Owner.RefreshChart((uint)Chart);
        });
    }
}

public class RefreshHoldingsPerSecurityChartCommand(IRefreshChart owner) : RefreshChartCommandBase(Charts.HoldingsPerSecurity, owner);

public class RefreshSummaryChartCommand(IRefreshChart owner) : RefreshChartCommandBase(Charts.Summary, owner);

public class RefreshRelativeSummaryChartCommand(IRefreshChart owner) : RefreshChartCommandBase(Charts.RelativeSummary, owner);