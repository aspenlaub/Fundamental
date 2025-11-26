using System;
using System.Text.Json.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class TransactionDto(ITransaction transaction) : ITransaction {
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; } = transaction.Date;

    [JsonIgnore]
    public Security Security { get; set; } = transaction.Security;

    [JsonPropertyName("Security")]
    public SecurityDto SecurityDto { get; set; } = new(transaction.Security);

    [JsonPropertyName("TransactionType")]
    public TransactionType TransactionType { get; set; } = transaction.TransactionType;

    [JsonPropertyName("Nominal")]
    public double Nominal { get; set; } = transaction.Nominal;

    [JsonPropertyName("PriceInEuro")]
    public double PriceInEuro { get; set; } = transaction.PriceInEuro;

    [JsonPropertyName("ExpensesInEuro")]
    public double ExpensesInEuro { get; set; } = transaction.ExpensesInEuro;

    [JsonPropertyName("IncomeInEuro")]
    public double IncomeInEuro { get; set; } = transaction.IncomeInEuro;
}