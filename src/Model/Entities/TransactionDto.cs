using System;
using System.Text.Json.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class TransactionDto : ITransaction {
    public DateTime Date { get; set; }
    [JsonIgnore]
    public Security Security { get; set; }
    [JsonPropertyName("Security")]
    public SecurityDto SecurityDto { get; set; }
    public TransactionType TransactionType { get; set; }
    public double Nominal { get; set; }
    public double PriceInEuro { get; set; }
    public double ExpensesInEuro { get; set; }
    public double IncomeInEuro { get; set; }

    public TransactionDto(ITransaction transaction) {
        Date = transaction.Date;
        Security = transaction.Security;
        SecurityDto = new SecurityDto(transaction.Security);
        TransactionType = transaction.TransactionType;
        Nominal = transaction.Nominal;
        PriceInEuro = transaction.PriceInEuro;
        ExpensesInEuro = transaction.ExpensesInEuro;
        IncomeInEuro = transaction.IncomeInEuro;
    }
}