using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

public interface IDateSummaryCalculator {
    IDateSummaryCalculator WithHoldings(IList<Holding> holdings);
    IList<DateSummary> CalculateDateSummaries();
}