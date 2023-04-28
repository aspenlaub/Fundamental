using System;
using System.ComponentModel.DataAnnotations;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class DateSummary : IGuid {
    [Key]
    public string Guid { get; set; }

    public DateTime Date { get; set; }

    public double CostValueInEuro { get; set; }
    public double QuoteValueInEuro { get; set; }
    public double RealizedLossInEuro { get; set; }
    public double RealizedProfitInEuro { get; set; }
    public double UnrealizedLossInEuro { get; set; }
    public double UnrealizedProfitInEuro { get; set; }

    public DateSummary() {
        Guid = System.Guid.NewGuid().ToString();
        CostValueInEuro = 0;
        QuoteValueInEuro = 0;
        RealizedLossInEuro = 0;
        RealizedProfitInEuro = 0;
        UnrealizedLossInEuro = 0;
        UnrealizedProfitInEuro = 0;
    }
}