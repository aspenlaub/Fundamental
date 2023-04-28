using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class RefreshChartCommandBase : IApplicationCommand {
    protected Charts Chart;
    protected IRefreshChart Owner;

    public bool MakeLogEntries => false;
    public string Name => Properties.Resources.RefreshChartCommandName;
    public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

    public RefreshChartCommandBase(Charts chart, IRefreshChart owner) {
        Chart = chart;
        Owner = owner;
    }

    public Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        return Task.Run(() => {
            Owner.RefreshChart((uint)Chart);
        });
    }
}

public class RefreshHoldingsPerSecurityChartCommand : RefreshChartCommandBase {
    public RefreshHoldingsPerSecurityChartCommand(IRefreshChart owner) : base(Charts.HoldingsPerSecurity, owner) { }
}

public class RefreshSummaryChartCommand : RefreshChartCommandBase {
    public RefreshSummaryChartCommand(IRefreshChart owner) : base(Charts.Summary, owner) { }
}

public class RefreshRelativeSummaryChartCommand : RefreshChartCommandBase {
    public RefreshRelativeSummaryChartCommand(IRefreshChart owner) : base(Charts.RelativeSummary, owner) { }
}