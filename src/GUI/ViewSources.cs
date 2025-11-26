using System.Windows;
using System.Windows.Data;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.GUI;

public class ViewSources(FrameworkElement window) {
    public CollectionViewSource TransactionViewSource = window.FindResource("TransactionViewSource") as CollectionViewSource;
    public CollectionViewSource SecurityViewSource = window.FindResource("SecurityViewSource") as CollectionViewSource;
    public CollectionViewSource OtherSecurityViewSource = window.FindResource("OtherSecurityViewSource") as CollectionViewSource;
    public CollectionViewSource HoldingPerSecurityViewSource = window.FindResource("HoldingPerSecurityViewSource") as CollectionViewSource;
    public CollectionViewSource DateSummaryViewSource = window.FindResource("DateSummaryViewSource") as CollectionViewSource;
    public CollectionViewSource HoldingPerDateViewSource = window.FindResource("HoldingPerDateViewSource") as CollectionViewSource;
    public CollectionViewSource LogViewSource = window.FindResource("LogViewSource") as CollectionViewSource;
}