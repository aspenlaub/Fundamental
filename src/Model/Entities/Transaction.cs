using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable UnusedMember.Global
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class Transaction : IGuid, INotifyPropertyChanged, ITransaction {
    [Key]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    public DateTime Date {
        get;
        set { field = value; OnPropertyChanged(nameof(Date)); }
    } = DateTime.Today;

    public string SecurityGuid { get; set; }

    [ForeignKey("SecurityGuid")]
    public Security Security {
        get;
        set { field = value; OnPropertyChanged(nameof(Security)); }
    }

    public TransactionType TransactionType {
        get;
        set { field = value; OnPropertyChanged(nameof(TransactionType)); }
    } = TransactionType.None;

    public double Nominal {
        get;
        set { field = value; OnPropertyChanged(nameof(Nominal)); }
    } = 0;

    public double PriceInEuro {
        get;
        set { field = value; OnPropertyChanged(nameof(PriceInEuro)); }
    } = 0;

    public double ExpensesInEuro {
        get;
        set { field = value; OnPropertyChanged(nameof(ExpensesInEuro)); }
    } = 0;

    public double IncomeInEuro {
        get;
        set { field = value; OnPropertyChanged(nameof(IncomeInEuro)); }
    } = 0;

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}