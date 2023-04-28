using System.Windows;
using System.Windows.Data;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.GUI;

public class ViewSources {
    public CollectionViewSource TransactionViewSource;
    public CollectionViewSource SecurityViewSource;
    public CollectionViewSource OtherSecurityViewSource;
    public CollectionViewSource HoldingPerSecurityViewSource;
    public CollectionViewSource DateSummaryViewSource;
    public CollectionViewSource HoldingPerDateViewSource;
    public CollectionViewSource LogViewSource;

    public ViewSources(FrameworkElement window) {
        TransactionViewSource = window.FindResource("TransactionViewSource") as CollectionViewSource;
        SecurityViewSource = window.FindResource("SecurityViewSource") as CollectionViewSource;
        OtherSecurityViewSource = window.FindResource("OtherSecurityViewSource") as CollectionViewSource;
        HoldingPerSecurityViewSource = window.FindResource("HoldingPerSecurityViewSource") as CollectionViewSource;
        DateSummaryViewSource = window.FindResource("DateSummaryViewSource") as CollectionViewSource;
        HoldingPerDateViewSource = window.FindResource("HoldingPerDateViewSource") as CollectionViewSource;
        LogViewSource = window.FindResource("LogViewSource") as CollectionViewSource;
    }
}