using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.DbOperations;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Microsoft.EntityFrameworkCore;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class FundamentalApplication : IDisposable, IRefreshContext, IRefreshChart, ITimeSeriesChartApplication, ISave, IUndo, IImport, IAddTransaction {
    protected EnvironmentType EnvironmentType;
    protected IApplicationCommandController Controller;
    protected Context Context;
    public Security SecurityInFocus { get; private set; }
    protected DateTime LatestHoldingDate;
    protected DateTime DateInFocus;
    protected TimeSeriesChartSource HoldingsPerSecurityChartSource, SummaryChartSource, RelativeSummaryChartSource;
    protected bool Disposed;
    protected HashSet<string> SecurityIdsWithDataToSave;
    protected IImportFileScanner ImportFileScanner;
    protected IImporter Importer;
    protected SynchronizationContext UiSynchronizationContext;
    protected IFolderResolver FolderResolver;
    protected IContextFactory ContextFactory;
    protected IApplicationCommandExecutionContext ApplicationCommandExecutionContext;
    protected ISecretRepository SecretRepository;
    private readonly ObservableCollection<LogEntry> _LogEntries = new();

    public FundamentalApplication(EnvironmentType environmentType,
            IApplicationCommandController controller, IApplicationCommandExecutionContext context,
            SynchronizationContext uiSynchronizationContext, IFolderResolver folderResolver,
            IContextFactory contextFactory, ISecretRepository secretRepository) {
        Disposed = false;
        UiSynchronizationContext = uiSynchronizationContext;
        ContextFactory = contextFactory;
        SecurityIdsWithDataToSave = new HashSet<string>();
        HoldingsPerSecurityChartSource = new TimeSeriesChartSource();
        SummaryChartSource = new TimeSeriesChartSource();
        RelativeSummaryChartSource = new TimeSeriesChartSource();
        Importer = new Importer(environmentType, context, folderResolver, contextFactory);
        EnvironmentType = environmentType;
        ApplicationCommandExecutionContext = context;
        SecretRepository = secretRepository;
        Controller = controller;
        Controller.AddCommand(new RefreshContextCommand(this), true);
        Controller.AddCommand(new RefreshHoldingsPerSecurityChartCommand(this), true);
        Controller.AddCommand(new RefreshSummaryChartCommand(this), true);
        Controller.AddCommand(new RefreshRelativeSummaryChartCommand(this), true);
        Controller.AddCommand(new SaveCommand(this), false);
        Controller.AddCommand(new UndoCommand(this), false);
        Controller.AddCommand(new ImportCommand(this), true);
        Controller.AddCommand(new AddTransactionCommand(this), true);
        FolderResolver = folderResolver;
    }

    public void Dispose() {
        Disposed = true;
        Context.Dispose();
    }

    public async Task OnLoadedAsync() {
        ImportFileScanner = new ImportFileScanner(EnvironmentType, ApplicationCommandExecutionContext, FolderResolver);
        await ImportFileScanner.CopyInputFilesToEnvironmentsAsync();
    }

    public async Task RefreshContextAsync() {
        var securityInFocusId = SecurityInFocus == null ? "" : SecurityInFocus.SecurityId;
        DetachCollectionChangedHandler();
        Context = await ContextFactory.CreateAsync(EnvironmentType, UiSynchronizationContext);
        await DataWasSavedAsync();
        SecurityInFocus = null;
        DateInFocus = new DateTime(0);
        LatestHoldingDate = Context.Holdings.Any() ? Context.Holdings.Max(x => x.Date) : DateTime.Today;
        await Context.Securities.LoadAsync();
        await Context.Transactions.LoadAsync();
        await Context.Holdings.LoadAsync();
        await Context.DateSummaries.LoadAsync();
        AttachCollectionChangedHandler();
        RefreshSummaryChartData();
        await Controller.ExecuteAsync(typeof(RefreshSummaryChartCommand));
        await Controller.ExecuteAsync(typeof(RefreshRelativeSummaryChartCommand));
        var security = Context.Securities.FirstOrDefault(s => s.SecurityId == securityInFocusId);
        FocusOnSecurity(security);
    }

    protected void RefreshSummaryChartData() {
        foreach (var summary in Context.DateSummaries.Local) {
            SummaryChartSource.AddPoint(summary.Date, summary.QuoteValueInEuro);
            if (Math.Abs(summary.CostValueInEuro) < 0.001) { continue; }
            RelativeSummaryChartSource.AddPoint(summary.Date, Math.Round(100 * summary.QuoteValueInEuro / summary.CostValueInEuro, 1));
        }
    }

    public Security FocusOnSecurity(Security security) {
        var changed = SecurityInFocus != security;
        SecurityInFocus = security ?? SecurityInFocus;
        if (changed) {
            Controller.ExecuteAsync(typeof(RefreshHoldingsPerSecurityChartCommand));
        }
        return SecurityInFocus;
    }

    public bool ShowSecurity(Security s, bool showCurrentSecurities) {
        if (s == null) { return false; }
        var securityId = s.SecurityId;
        var holding = Context.Holdings.Where(x => x.Security.SecurityId == securityId).OrderByDescending(x => x.Date).FirstOrDefault();
        var showAsCurrent = holding != null && holding.Date.Year >= DateTime.Now.Year - 1;
        if (showCurrentSecurities) { return showAsCurrent; }
        return !showAsCurrent;
    }

    public bool ShowTransaction(Transaction t) {
        return t != null && SecurityInFocus != null && t.Security == SecurityInFocus;
    }

    public bool ShowHoldingWithSecurityInFocus(Holding h) {
        return h?.Security != null && SecurityInFocus != null && h.Security.SecurityId == SecurityInFocus.SecurityId;
    }

    public DateTime FocusOnDate(DateTime date) {
        if (date.Year > 1980) {
            DateInFocus = date;
        }
        return DateInFocus;
    }

    public bool ShowHoldingWithDateInFocus(Holding h) {
        return h != null && DateInFocus.Year > 1980 && h.Date == DateInFocus;
    }

    public ObservableCollection<Security> Securities() {
        return Context.Securities.Local.ToObservableCollection();
    }

    public ObservableCollection<Transaction> Transactions() {
        return Context.Transactions.Local.ToObservableCollection();
    }

    public ObservableCollection<Holding> Holdings() {
        return Context.Holdings.Local.ToObservableCollection();
    }

    public ObservableCollection<DateSummary> DateSummaries() {
        return Context.DateSummaries.Local.ToObservableCollection();
    }

    public IDictionary<DateTime, double> VisibleChartPoints(uint chartId) {
        var chartSource = ChartSource(chartId);
        return chartSource.VisiblePoints();
    }

    public async Task ZoomInAsync(uint chartId) {
        var chartSource = ChartSource(chartId);
        chartSource.ZoomIn();
        await ExecuteRefreshChartCommandAsync(chartId);
    }

    public async Task ZoomOutAsync(uint chartId) {
        var chartSource = ChartSource(chartId);
        chartSource.ZoomOut();
        await ExecuteRefreshChartCommandAsync(chartId);
    }

    public async Task ScrollLeftAsync(uint chartId) {
        var chartSource = ChartSource(chartId);
        chartSource.ScrollLeft();
        await ExecuteRefreshChartCommandAsync(chartId);
    }

    public async Task ScrollRightAsync(uint chartId) {
        var chartSource = ChartSource(chartId);
        chartSource.ScrollRight();
        await ExecuteRefreshChartCommandAsync(chartId);
    }

    protected TimeSeriesChartSource ChartSource(uint chartId) {
        switch (chartId) {
            case (uint)Charts.HoldingsPerSecurity: {
                return HoldingsPerSecurityChartSource;
            }
            case (uint)Charts.Summary: {
                return SummaryChartSource;
            }
            case (uint)Charts.RelativeSummary: {
                return RelativeSummaryChartSource;
            }
            default: {
                throw new NotImplementedException();
            }
        }
    }

    public void RefreshChart(uint chartId) {
        if (Disposed) { return; }
        switch (chartId) {
            case (uint)Charts.HoldingsPerSecurity: {
                if (SecurityInFocus == null) { return; }
                HoldingsPerSecurityChartSource.Clear();
                foreach (var holding in Context.Holdings.Local.Where(x => x.Security != null && x.Security.SecurityId == SecurityInFocus.SecurityId && Math.Abs(x.NominalBalance) > 0.001)) {
                    HoldingsPerSecurityChartSource.AddPoint(holding.Date, holding.QuoteValueInEuro);
                }
            }
                break;
            case (uint)Charts.Summary:
            case (uint)Charts.RelativeSummary: {
            }
                break;
            default: {
                throw new NotImplementedException();
            }
        }
    }

    public async Task ExecuteRefreshChartCommandAsync(uint chartId) {
        if (Disposed) { return; }
        switch (chartId) {
            case (uint)Charts.HoldingsPerSecurity: {
                if (SecurityInFocus != null && await Controller.EnabledAsync(typeof(RefreshHoldingsPerSecurityChartCommand))) {
                    await Controller.ExecuteAsync(typeof(RefreshHoldingsPerSecurityChartCommand));
                }
            }
                break;
            case (uint)Charts.Summary: {
                await Controller.ExecuteAsync(typeof(RefreshSummaryChartCommand));
            }
                break;
            case (uint)Charts.RelativeSummary: {
                await Controller.ExecuteAsync(typeof(RefreshRelativeSummaryChartCommand));
            }
                break;
            default: {
                throw new NotImplementedException();
            }
        }
    }

    protected void DetachCollectionChangedHandler() {
        if (Context == null) { return; }
        Context.Transactions.Local.CollectionChanged -= TransactionCollectionChangedAsync;
        DetachTransactionChangedHandlers();
    }

    protected void DetachTransactionChangedHandlers() {
        if (Context == null) { return; }
        foreach (var transaction in Context.Transactions.Local) {
            transaction.PropertyChanged -= TransactionPropertyChangedAsync;
        }
    }

    protected void AttachCollectionChangedHandler() {
        if (Context == null) { return; }
        Context.Transactions.Local.CollectionChanged += TransactionCollectionChangedAsync;
        AttachTransactionChangedHandlers();
    }

    protected void AttachTransactionChangedHandlers() {
        if (Context == null) { return; }
        foreach (var transaction in Context.Transactions.Local) {
            transaction.PropertyChanged += TransactionPropertyChangedAsync;
        }
    }

    protected async void TransactionPropertyChangedAsync(object sender, PropertyChangedEventArgs e) {
        await DataWasEnteredAsync();
    }

    protected async void TransactionCollectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e) {
        if (e.Action != NotifyCollectionChangedAction.Add) { return; }
        await DataWasEnteredAsync();
        if (e.NewItems == null) { return; }

        foreach (Transaction t in e.NewItems) {
            t.Security = SecurityInFocus;
        }
    }

    public async Task SaveAsync() {
        Context.SaveChanges();
        await DeleteInertTransactionsAsync();
        await new Rebooker(EnvironmentType, ContextFactory).RebookAsync(SecurityIdsWithDataToSave);
        await DataWasSavedAsync();
    }

    protected async Task DeleteInertTransactionsAsync() {
        await using var context = await ContextFactory.CreateAsync(EnvironmentType, UiSynchronizationContext);
        var inertTransactions = context.Transactions.Include(t => t.Security).Where(Inert).ToList();
        if (!inertTransactions.Any()) { return; }
        foreach (var t in inertTransactions) {
            context.Remove(t);
        }
        context.SaveChanges();
    }

    private static bool Inert(Transaction t) {
        if (Math.Abs(t.Nominal) < 0.001 && Math.Abs(t.IncomeInEuro) < 0.001 && Math.Abs(t.ExpensesInEuro) < 0.001) {
            return true;
        }
        return t.Security == null;
    }

    public async Task UndoAsync() {
        await RefreshContextAsync();
        await DataWasSavedAsync();
    }

    public bool WasDataEntered() {
        return SecurityIdsWithDataToSave.Any();
    }

    public async Task<bool> IsThereAnythingToImportAsync() {
        return await ImportFileScanner.AnythingToImportAsync();
    }

    protected async Task DataWasEnteredAsync() {
        var anyChanged = SecurityIdsWithDataToSave.Any();
        SecurityIdsWithDataToSave.Add(SecurityInFocus.SecurityId);
        if (anyChanged) { return; }
        await Controller.EnableCommandAsync(typeof(SaveCommand));
        await Controller.EnableCommandAsync(typeof(UndoCommand));
    }

    public async Task DataWasSavedAsync() {
        if (!SecurityIdsWithDataToSave.Any()) { return; }
        await Controller.DisableCommandAsync(typeof(SaveCommand));
        await Controller.DisableCommandAsync(typeof(UndoCommand));
        SecurityIdsWithDataToSave.Clear();
        await RefreshContextAsync();
    }

    public async Task ImportAsync() {
        var fundamentalSettingsSecret = new SecretFundamentalSettings();
        var errorsAndInfos = new ErrorsAndInfos();
        var fundamentalSettings = await SecretRepository.GetAsync(fundamentalSettingsSecret, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(nameof(ImportAsync));
        }
        await Importer.ImportAFileAsync(fundamentalSettings.BankStatementInfix);
        await RefreshContextAsync();
        await DataWasSavedAsync();
    }

    public async Task DumpAsync() {
        var dumper = new Dumper();
        var errorsAndInfos = new ErrorsAndInfos();
        var dumpFolder = (await FolderResolver.ResolveAsync("$(MainUserFolder)", errorsAndInfos))
                         .SubFolder("Fundamental").SubFolder(Enum.GetName(typeof(EnvironmentType), EnvironmentType)).SubFolder("Dump");
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }
        dumpFolder.CreateIfNecessary();
        dumper.DumpSecurities(dumpFolder, Context.Securities.ToList());
        dumper.DumpTransactions(dumpFolder, Context.Transactions.Where(t => t.Security != null).ToList());
        dumper.DumpQuotes(dumpFolder, Context.Quotes.ToList());
    }

    public bool IsSecurityInFocus() {
        return SecurityInFocus != null;
    }

    public bool IsAnInertTransactionPresent() {
        return Context.Transactions.Any(Inert);
    }

    public void AddTransaction() {
        if (SecurityInFocus == null) { return; }

        var transaction = new Transaction {
            Security = SecurityInFocus
        };
        Context.Add(transaction);
    }

    public bool ShowLogEntry(LogEntry _) {
        return true;
    }

    public ObservableCollection<LogEntry> LogEntries() {
        return _LogEntries;
    }

    public void AddLogEntry(string logType, string logMessage) {
        _LogEntries.Add(new LogEntry { LogTime = DateTime.Now, LogType = logType, LogMessage = logMessage });
    }
}