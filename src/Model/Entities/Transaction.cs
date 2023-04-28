using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class Transaction : IGuid, INotifyPropertyChanged, ITransaction {
    [Key]
    public string Guid { get; set; }

    private DateTime _PrivateDate;
    public DateTime Date { get => _PrivateDate; set { _PrivateDate = value; OnPropertyChanged(nameof(Date)); } }

    public string SecurityGuid { get; set; }
    private Security _PrivateSecurity;
    [ForeignKey("SecurityGuid")]
    public Security Security { get => _PrivateSecurity; set { _PrivateSecurity = value; OnPropertyChanged(nameof(Security)); } }

    private TransactionType _PrivateTransactionType;
    public TransactionType TransactionType { get => _PrivateTransactionType; set { _PrivateTransactionType = value; OnPropertyChanged(nameof(TransactionType)); } }

    private double _PrivateNominal;
    public double Nominal { get => _PrivateNominal; set { _PrivateNominal = value; OnPropertyChanged(nameof(Nominal)); } }

    private double _PrivatePriceInEuro;
    public double PriceInEuro { get => _PrivatePriceInEuro; set { _PrivatePriceInEuro = value; OnPropertyChanged(nameof(PriceInEuro)); } }

    private double _PrivateExpensesInEuro;
    public double ExpensesInEuro { get => _PrivateExpensesInEuro; set { _PrivateExpensesInEuro = value; OnPropertyChanged(nameof(ExpensesInEuro)); } }

    private double _PrivateIncomeInEuro;
    public double IncomeInEuro { get => _PrivateIncomeInEuro; set { _PrivateIncomeInEuro = value; OnPropertyChanged(nameof(IncomeInEuro)); } }

    public Transaction() {
        Guid = System.Guid.NewGuid().ToString();
        _PrivateDate = DateTime.Today;
        _PrivateTransactionType = TransactionType.None;
        _PrivateNominal = 0;
        _PrivatePriceInEuro = 0;
        _PrivateExpensesInEuro = 0;
        _PrivateIncomeInEuro = 0;
    }

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}