using System;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

public interface ITransaction {
    DateTime Date { get; set; }
    Security Security { get; set; }
    TransactionType TransactionType { get; set; }
    double Nominal { get; set; }
    double PriceInEuro { get; set; }
    double ExpensesInEuro { get; set; }
    double IncomeInEuro { get; set; }
}