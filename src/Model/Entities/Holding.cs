using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class Holding : IGuid {
    [Key]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    public DateTime Date { get; set; }

    public string SecurityGuid { get; set; }
    [ForeignKey("SecurityGuid")]
    public Security Security { get; set; }

    public double NominalBalance { get; set; }
    public double CostValueInEuro { get; set; }
    public double QuoteValueInEuro { get; set; }
    public double RealizedLossInEuro { get; set; }
    public double RealizedProfitInEuro { get; set; }
    public double UnrealizedLossInEuro { get; set; }
    public double UnrealizedProfitInEuro { get; set; }
}