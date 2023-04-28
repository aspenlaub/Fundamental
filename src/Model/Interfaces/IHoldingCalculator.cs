using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

public interface IHoldingCalculator {
    IHoldingCalculator WithTransactions(IList<Transaction> transactions);
    IHoldingCalculator WithQuotes(IList<Quote> quotes);
    IList<Holding> CalculateHoldings();
}