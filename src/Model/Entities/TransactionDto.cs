using System;
using System.Text.Json.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class TransactionDto : ITransaction {
    public TransactionDto() {
    }

    public TransactionDto(ITransaction transaction) : this() {
        Date = transaction.Date;
        Security = transaction.Security;
        SecurityDto = new SecurityDto(transaction.Security);
        TransactionType = transaction.TransactionType;
        Nominal = transaction.Nominal;
        PriceInEuro = transaction.PriceInEuro;
        ExpensesInEuro = transaction.ExpensesInEuro;
        IncomeInEuro = transaction.IncomeInEuro;
    }

    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonIgnore]
    public Security Security { get; set; }

    [JsonPropertyName("Security")]
    public SecurityDto SecurityDto { get; set; }

    [JsonPropertyName("TransactionType")]
    public TransactionType TransactionType { get; set; }

    [JsonPropertyName("Nominal")]
    public double Nominal { get; set; }

    [JsonPropertyName("PriceInEuro")]
    public double PriceInEuro { get; set; }

    [JsonPropertyName("ExpensesInEuro")]
    public double ExpensesInEuro { get; set; }

    [JsonPropertyName("IncomeInEuro")]
    public double IncomeInEuro { get; set; }
}