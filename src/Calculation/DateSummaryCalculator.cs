using System;
using System.Collections.Generic;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;

public class DateSummaryCalculator : IDateSummaryCalculator {
    protected List<Holding> Holdings = [];
    protected IList<DateSummary> DateSummaries;

    public IDateSummaryCalculator WithHoldings(IList<Holding> holdings) {
        Holdings.AddRange(holdings);
        Holdings = Holdings.OrderBy(x => x.Date).ToList();
        return this;
    }

    public IList<DateSummary> CalculateDateSummaries() {
        DateSummaries = new List<DateSummary>();
        foreach(Holding holding in Holdings.Where(x => Math.Abs(x.NominalBalance) > 0.001)) {
            DateSummary summary = DateSummaries.FirstOrDefault(x => x.Date == holding.Date);
            if (summary == null) {
                summary = new DateSummary() { Date = holding.Date };
                DateSummaries.Add(summary);
            }
            summary.CostValueInEuro = Math.Round(summary.CostValueInEuro + holding.CostValueInEuro, 2);
            summary.QuoteValueInEuro = Math.Round(summary.QuoteValueInEuro + holding.QuoteValueInEuro, 2);
            summary.RealizedLossInEuro = Math.Round(summary.RealizedLossInEuro + holding.RealizedLossInEuro, 2);
            summary.RealizedProfitInEuro = Math.Round(summary.RealizedProfitInEuro + holding.RealizedProfitInEuro, 2);
            summary.UnrealizedLossInEuro = Math.Round(summary.UnrealizedLossInEuro + holding.UnrealizedLossInEuro, 2);
            summary.UnrealizedProfitInEuro = Math.Round(summary.UnrealizedProfitInEuro + holding.UnrealizedProfitInEuro, 2);
        }
        DateSummaries = DateSummaries.OrderBy(x => x.Date).ToList();
        return DateSummaries;
    }
}