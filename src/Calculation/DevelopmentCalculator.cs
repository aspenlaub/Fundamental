using System;
using System.Collections.Generic;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;

public class DevelopmentCalculator {
    private readonly List<Holding> _Holdings = [];
    private readonly List<Quote> _Quotes = [];

    public DateTime HoldingDate { get; private set; } = DateTime.Today;
    private DateTime MinimumStartDate => HoldingDate.AddYears(-10);

    private readonly Random _Random = new Random();

    public DevelopmentCalculator WithHoldings(IList<Holding> holdings) {
        HoldingDate = holdings.Max(h => h.Date);
        _Holdings.Clear();
        _Holdings.AddRange(holdings.Where(h => h.Date == HoldingDate));
        return this;
    }

    public DevelopmentCalculator WithQuotes(IList<Quote> quotes) {
        _Quotes.AddRange(quotes);
        return this;
    }

    public void CalculateScenario() {
        if (_Holdings.Count == 0 || _Quotes.Count == 0) {
            return;
        }
    }

    public DateTime PickADate() {
        IList<DateTime> dates = _Quotes.Select(q => q.Date).Where(d => d >= MinimumStartDate).Distinct().ToList();
        return dates[_Random.Next(0, dates.Count)];
    }
}
