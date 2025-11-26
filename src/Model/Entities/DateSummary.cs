using System;
using System.ComponentModel.DataAnnotations;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class DateSummary : IGuid {
    [Key]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    public DateTime Date { get; set; }

    public double CostValueInEuro { get; set; } = 0;
    public double QuoteValueInEuro { get; set; } = 0;
    public double RealizedLossInEuro { get; set; } = 0;
    public double RealizedProfitInEuro { get; set; } = 0;
    public double UnrealizedLossInEuro { get; set; } = 0;
    public double UnrealizedProfitInEuro { get; set; } = 0;
}