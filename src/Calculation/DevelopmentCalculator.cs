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
    private DateTime MaximumStartDate => HoldingDate.AddYears(- DevelopmentCalculatorConstants.ScenarioLengthInYears);

    private readonly Random _Random = new Random();

    public DevelopmentCalculator WithHoldings(IList<Holding> holdings) {
        HoldingDate = holdings.Max(h => h.Date);
        _Holdings.Clear();
        _Holdings.AddRange(holdings.Where(h => h.Date == HoldingDate && h.NominalBalance > 0));
        return this;
    }

    public DevelopmentCalculator WithQuotes(IList<Quote> quotes) {
        _Quotes.AddRange(quotes);
        return this;
    }

    public DateTime PickADate() {
        IList<DateTime> dates = [.. _Quotes.Select(q => q.Date).Where(d => d >= MinimumStartDate && d <= MaximumStartDate).Distinct()];
        return dates[_Random.Next(0, dates.Count)];
    }

    public ScenarioResult CalculateScenario(DateTime pickedDate) {
        if (_Holdings.Count == 0 || _Quotes.Count == 0) {
            return null;
        }

        DateTime scenarioEndDate = pickedDate.AddYears(DevelopmentCalculatorConstants.ScenarioLengthInYears);
        if (scenarioEndDate >= DateTime.Today) {
            return null;
        }

        IList<Holding> scenarioStartHoldings = [.. _Holdings.Select(h => UpdateHoldingWithQuoteOnOrBeforeDate(h, pickedDate))];

        IList<Holding> scenarioEndHoldings = [.. _Holdings.Select(h => UpdateHoldingWithQuoteOnOrBeforeDate(h, scenarioEndDate))];

        return new ScenarioResult(scenarioStartHoldings, scenarioEndHoldings);
    }

    private Holding UpdateHoldingWithQuoteOnOrBeforeDate(Holding holding, DateTime date) {
        var quotes = _Quotes.Where(q => q.SecurityGuid == holding.SecurityGuid && q.Date <= date).ToList();
        if (quotes.Count != 0) {
            DateTime maxDate = quotes.Max(q => q.Date);
            quotes = [.. quotes.Where(q => q.Date == maxDate)];
        }
        double priceInEuro = quotes.Count == 0 ? 0 : quotes[0].PriceInEuro;
        return new Holding {
            SecurityGuid = holding.SecurityGuid,
            Security = holding.Security,
            NominalBalance = holding.NominalBalance,
            Date = date,
            QuoteValueInEuro = Math.Round(holding.NominalBalance * priceInEuro / holding.Security.QuotedPer, 2)
        };
    }

    public ScenariosResult CalculateScenarios(IList<DateTime> pickedDates) {
        ScenariosResult scenariosResult = new();
        foreach (DateTime pickedDate in pickedDates) {
            ScenarioResult scenarioResult = CalculateScenario(pickedDate);
            scenariosResult.Add(scenarioResult.AverageYearlyChangeFactor());

        }

        return scenariosResult;
    }

    public ScenariosResult CalculateScenariosToFixedPoint() {
        ScenariosResult scenariosResult = new();
        int minimumNumberOfScenarios = 1000;
        do {
            DateTime pickedDate = PickADate();
            ScenarioResult scenarioResult = CalculateScenario(pickedDate);
            scenariosResult.Add(scenarioResult.AverageYearlyChangeFactor());
        } while (scenariosResult.ResultsChangedAfterLatestAddition || --minimumNumberOfScenarios >= 0);

        return scenariosResult;
    }
}
