using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.GUI;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;
using IContainer = Autofac.IContainer;

// ReSharper disable once UnusedMember.Global
// ReSharper disable AsyncVoidEventHandlerMethod

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental;

public partial class FundamentalWindow {
    private readonly ApplicationCommandController _Controller;
    private readonly FundamentalApplication _FundamentalApplication;
    private ViewSources _FundamentalViewSources;
    private DateTime _ResetStatusInformationAfter;
    public bool ReadyToUse { get; private set; }
    private readonly ISimpleLogger _SimpleLogger;
    private readonly IMethodNamesFromStackFramesExtractor _MethodNamesFromStackFramesExtractor;
    private readonly BackgroundWorker _BackgroundWorker = new BackgroundWorker();

    public FundamentalWindow() : this(Context.DefaultEnvironmentType) { }

    public FundamentalWindow(EnvironmentType environmentType) {
        SynchronizationContext uiSynchronizationContext = SynchronizationContext.Current;
        IContainer container = new ContainerBuilder().UsePegh("Fundamental", new DummyCsArgumentPrompter()).Build();
        ReadyToUse = false;
        _ResetStatusInformationAfter = DateTime.Now;
        InitializeComponent();
        _SimpleLogger = container.Resolve<ISimpleLogger>();
        _MethodNamesFromStackFramesExtractor = container.Resolve<IMethodNamesFromStackFramesExtractor>();
        _Controller = new ApplicationCommandController(_SimpleLogger, HandleFeedbackToApplicationAsync);
        _FundamentalApplication = new FundamentalApplication(environmentType, _Controller,
            _Controller, uiSynchronizationContext, container.Resolve<IFolderResolver>(),
            new ContextFactory(), container.Resolve<ISecretRepository>());
        HoldingsPerSecurityChart.Application = _FundamentalApplication;
        HoldingsPerSecurityChart.UniqueChartNumber = (uint)Charts.HoldingsPerSecurity;
        SummaryChart.Application = _FundamentalApplication;
        SummaryChart.UniqueChartNumber = (uint)Charts.Summary;
        SummaryChart.Title = Properties.Resources.SummaryChartTitle;
        RelativeSummaryChart.Application = _FundamentalApplication;
        RelativeSummaryChart.UniqueChartNumber = (uint)Charts.RelativeSummary;
        RelativeSummaryChart.Title = Properties.Resources.RelativeSummaryChartTitle;
        _BackgroundWorker.DoWork += DoBackgroundWork;
        _BackgroundWorker.RunWorkerCompleted += OnRunWorkerCompleted;
    }

    private async void Window_LoadedAsync(object sender, RoutedEventArgs e) {
        await _FundamentalApplication.OnLoadedAsync();
        _FundamentalViewSources = new ViewSources(this);
        TransactionDataGrid.IsReadOnly = true;
        try {
            await _Controller.ExecuteAsync(typeof(RefreshContextCommand));
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "Exception on startup", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
        ReadyToUse = true;
        await CommandsEnabledOrDisabledHandlerAsync();
    }

    private async void OnWindowClosingAsync(object sender, CancelEventArgs e) {
        await _FundamentalApplication.DumpAsync();
        _FundamentalApplication.Dispose();
    }

    private void SecurityDataGrid_CurrentCellChanged(object sender, EventArgs e) {
        FocusOnSecurity(SecurityDataGrid.CurrentCell.Item as Security);
    }

    private void OtherSecurityDataGrid_CurrentCellChanged(object sender, EventArgs e) {
        FocusOnSecurity(OtherSecurityDataGrid.CurrentCell.Item as Security);
    }

    private void SecurityViewSource_Filter(object sender, FilterEventArgs e) {
        e.Accepted = _FundamentalApplication.ShowSecurity(e.Item as Security, true);
    }

    private void OtherSecurityViewSource_Filter(object sender, FilterEventArgs e) {
        e.Accepted = _FundamentalApplication.ShowSecurity(e.Item as Security, false);
    }

    private void TransactionViewSource_Filter(object sender, FilterEventArgs e) {
        e.Accepted = _FundamentalApplication.ShowTransaction(e.Item as Transaction);
    }

    private void HoldingPerSecurityViewSource_Filter(object sender, FilterEventArgs e) {
        e.Accepted = _FundamentalApplication.ShowHoldingWithSecurityInFocus(e.Item as Holding);
    }

    private void FocusOnDate(DateTime date) {
        _FundamentalApplication.FocusOnDate(date);
        if (date.Year <= 1980) { return; }

        _FundamentalViewSources.HoldingPerDateViewSource.View.Refresh();
    }

    private void DateSummaryDataGrid_CurrentCellChanged(object sender, EventArgs e) {
        var dateSummary = DateSummaryDataGrid.CurrentCell.Item as DateSummary;
        if (dateSummary == null) { return; }

        FocusOnDate(dateSummary.Date);
        if (_BackgroundWorker.IsBusy) {
            return;
        }

        _BackgroundWorker.RunWorkerAsync();
    }

    private void HoldingPerDateViewSource_Filter(object sender, FilterEventArgs e) {
        e.Accepted = _FundamentalApplication.ShowHoldingWithDateInFocus(e.Item as Holding);
    }

    private async void OnTabControlSelectionChangedAsync(object sender, SelectionChangedEventArgs e) {
        var tabControl = sender as TabControl;
        // ReSharper disable once UseNullPropagation
        if (tabControl == null) { return; }

        var selectedItem = tabControl.SelectedItem as TabItem;
        if (selectedItem == null) { return; }

        string tabItemName = selectedItem.Name;
        await ExecuteRefreshChartCommandsForTabAsync(tabItemName);
    }

    private async void OnWindowSizeChangedAsync(object sender, SizeChangedEventArgs e) {
        var selectedItem = FundamentalTabControl.SelectedItem as TabItem;
        if (selectedItem == null) { return; }

        string tabItemName = selectedItem.Name;
        await ExecuteRefreshChartCommandsForTabAsync(tabItemName);
    }

    public async Task HandleFeedbackToApplicationAsync(IFeedbackToApplication feedback) {
        using (_SimpleLogger.BeginScope(SimpleLoggingScopeId.Create(nameof(HandleFeedbackToApplicationAsync)))) {
            IList<string> methodNamesFromStack = _MethodNamesFromStackFramesExtractor.ExtractMethodNamesFromStackFrames();
            switch (feedback.Type) {
                case FeedbackType.CommandExecutionCompleted: {
                    CommandExecutionCompletedHandler(feedback);
                }
                    break;
                case FeedbackType.CommandsEnabledOrDisabled: {
                    await CommandsEnabledOrDisabledHandlerAsync();
                }
                    break;
                case FeedbackType.LogInformation: {
                    _SimpleLogger.LogInformationWithCallStack(feedback.Message, methodNamesFromStack);
                    _FundamentalApplication.AddLogEntry("Information", feedback.Message);
                }
                    break;
                case FeedbackType.LogWarning: {
                    _SimpleLogger.LogWarningWithCallStack(feedback.Message, methodNamesFromStack);
                    _FundamentalApplication.AddLogEntry("Warning", feedback.Message);
                }
                    break;
                case FeedbackType.LogError: {
                    _SimpleLogger.LogErrorWithCallStack(feedback.Message, methodNamesFromStack);
                    _FundamentalApplication.AddLogEntry("Error", feedback.Message);
                }
                    break;
                case FeedbackType.CommandIsDisabled: {
                    _SimpleLogger.LogErrorWithCallStack("Attempt to run disabled command " + feedback.CommandType, methodNamesFromStack);
                }
                    break;
                case FeedbackType.ImportantMessage:
                case FeedbackType.MessageOfNoImportance:
                case FeedbackType.MessagesOfNoImportanceWereIgnored:
                case FeedbackType.EnableCommand:
                case FeedbackType.DisableCommand:
                case FeedbackType.UnknownCommand:
                case FeedbackType.CommandExecutionCompletedWithMessage:
                default: {
                    throw new NotImplementedException();
                }
            }
        }
    }

    private void CommandExecutionCompletedHandler(IFeedbackToApplication feedback) {
        if (!_Controller.IsMainThread()) { return; }

        if (AssumeContextRefresh(feedback)) {
            SetViewSource(_FundamentalViewSources.SecurityViewSource, _FundamentalApplication.Securities(), "SecurityId", ListSortDirection.Ascending);
            SetViewSource(_FundamentalViewSources.OtherSecurityViewSource, _FundamentalApplication.Securities(), "SecurityId", ListSortDirection.Ascending);
            SetViewSource(_FundamentalViewSources.TransactionViewSource, _FundamentalApplication.Transactions(), "Date", ListSortDirection.Ascending);
            SetViewSource(_FundamentalViewSources.HoldingPerSecurityViewSource, _FundamentalApplication.Holdings(), "Date", ListSortDirection.Descending);
            SetViewSource(_FundamentalViewSources.DateSummaryViewSource, _FundamentalApplication.DateSummaries(), "Date", ListSortDirection.Descending);
            SetViewSource(_FundamentalViewSources.HoldingPerDateViewSource, _FundamentalApplication.Holdings(), "Security.SecurityId", ListSortDirection.Ascending);
            SetViewSource(_FundamentalViewSources.LogViewSource, _FundamentalApplication.LogEntries(), "LogTime", ListSortDirection.Ascending);
            if (feedback.CommandType == typeof(SaveCommand)) {
                SetStatusInformation(Properties.Resources.DataSaved);
            } else if (feedback.CommandType == typeof(UndoCommand)) {
                SetStatusInformation(Properties.Resources.ChangesUndone);
            } else if (feedback.CommandType == typeof(ImportCommand)) {
                SetStatusInformation(Properties.Resources.FileImported);
            }
        } else if (feedback.CommandType == typeof(RefreshHoldingsPerSecurityChartCommand)) {
            Security security = _FundamentalApplication.SecurityInFocus;
            HoldingsPerSecurityChart.Title = security != null ? string.Format(Properties.Resources.HoldingsPerSecurityChartTitle, security.SecurityId, security.SecurityName) : "";
            try {
                HoldingsPerSecurityChart.Draw();
            } catch {
                // ignored
            }
        } else if (feedback.CommandType == typeof(RefreshSummaryChartCommand)) {
            SummaryChart.Draw();
        } else if (feedback.CommandType == typeof(RefreshRelativeSummaryChartCommand)) {
            RelativeSummaryChart.Draw();
        } else if (feedback.CommandType == typeof(AddTransactionCommand)) {
            if (TransactionDataGrid.Items.Count != 0) {
                object item = TransactionDataGrid.Items[^1];
                TransactionDataGrid.SelectedItem = item;
                TransactionDataGrid.ScrollIntoView(item);
            }
        } else {
            throw new NotImplementedException();
        }

        if (_ResetStatusInformationAfter <= DateTime.Now) {
            SetStatusInformation("");
        }
    }

    private static bool AssumeContextRefresh(IFeedbackToApplication feedback) {
        return feedback.CommandType == typeof(RefreshContextCommand) || feedback.CommandType == typeof(SaveCommand)
                                                                     || feedback.CommandType == typeof(UndoCommand) || feedback.CommandType == typeof(ImportCommand) ;
    }

    private void SetStatusInformation(string text) {
        StatusInformation.Text = text;
        StatusInformation.Padding = new Thickness(text.Length == 0 ? 0 : 10);
        _ResetStatusInformationAfter = text.Length == 0 ? DateTime.Now.AddHours(1) : DateTime.Now.AddSeconds(10);
    }

    private static void SetViewSource<T>(CollectionViewSource source, ObservableCollection<T> collection, string sortProperty, ListSortDirection sortDirection) {
        source.Source = collection;
        source.SortDescriptions.Clear();
        source.SortDescriptions.Add(new SortDescription(sortProperty, sortDirection));
    }

    public async Task CommandsEnabledOrDisabledHandlerAsync() {
        Save.IsEnabled = await _Controller.EnabledAsync(typeof(SaveCommand));
        Undo.IsEnabled = await _Controller.EnabledAsync(typeof(UndoCommand));
        Import.IsEnabled = await _Controller.EnabledAsync(typeof(ImportCommand));
        Add.IsEnabled = await _Controller.EnabledAsync(typeof(AddTransactionCommand));
    }

    private void FocusOnSecurity(Security security) {
        Security newSecurity = _FundamentalApplication.FocusOnSecurity(security);
        TransactionDataGrid.IsReadOnly = newSecurity == null;
        if (security == null) { return; }

        _FundamentalViewSources.TransactionViewSource.View.Refresh();
        _FundamentalViewSources.HoldingPerSecurityViewSource.View.Refresh();
    }

    private async Task ExecuteRefreshChartCommandsForTabAsync(string tabItemName) {
        if (tabItemName == LogTab.Name) { return; }

        if (tabItemName == DataTab.Name) {
            await _FundamentalApplication.ExecuteRefreshChartCommandAsync((uint)Charts.HoldingsPerSecurity);
        } else if (tabItemName == SummaryTab.Name) {
            await _FundamentalApplication.ExecuteRefreshChartCommandAsync((uint)Charts.Summary);
            await _FundamentalApplication.ExecuteRefreshChartCommandAsync((uint)Charts.RelativeSummary);
        } else {
            throw new NotImplementedException();
        }
    }

    private async void OnUndoClickAsync(object sender, RoutedEventArgs e) {
        await _Controller.ExecuteAsync(typeof(UndoCommand));
    }

    private async void SaveCommandExecutedAsync(object sender, ExecutedRoutedEventArgs e) {
        Save.Focus();
        await _Controller.ExecuteAsync(typeof(SaveCommand));
    }

    private async void CanExecuteSaveCommandAsync(object sender, CanExecuteRoutedEventArgs e) {
        // ReSharper disable once MergeSequentialChecksWhenPossible
        e.CanExecute = _Controller != null && await _Controller.EnabledAsync(typeof(SaveCommand));
    }

    private async void OnImportClickAsync(object sender, RoutedEventArgs e) {
        await _Controller.ExecuteAsync(typeof(ImportCommand));
    }

    private async void OnAddClickAsync(object sender, RoutedEventArgs e) {
        await _Controller.ExecuteAsync(typeof(AddTransactionCommand));
    }

    private void LogViewSource_Filter(object sender, FilterEventArgs e) {
        e.Accepted = true;
    }

    private void DoBackgroundWork(object sender, DoWorkEventArgs e) {
        e.Result = _FundamentalApplication.CalculateScenarios();
    }

    private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
        _FundamentalApplication.LogScenariosResult(e.Result as IList<string>);
    }
}