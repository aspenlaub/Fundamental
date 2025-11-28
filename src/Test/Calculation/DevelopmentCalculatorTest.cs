using System;
using System.Collections.Generic;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Calculation;

[TestClass]
public class DevelopmentCalculatorTest {
    private Dictionary<string, Security> _Securities;
    private List<Holding> _Holdings;
    private List<Quote> _Quotes;

    private readonly DateTime _Date2024 = new(2024, 7, 24);
    private readonly DateTime _Date2025 = new(2025, 10, 15);
    private List<DateTime> _OtherDates;

    private DevelopmentCalculator _Sut;

    [TestInitialize]
    public void Setup() {
        _Securities = new Dictionary<string, Security> {
            ["A"] = CreateSecurity("A"),
            ["B"] = CreateSecurity("B"),
            ["C"] = CreateSecurity("C")
        };

        _OtherDates = [];
        for (int i = 0; i < 20; i++) {
            _OtherDates.Add(_Date2024.AddDays(-217 * (i + 1)));
        }

        _Holdings = [
            CreateHolding("A", 20000, _Date2025), CreateHolding("B", 40000, _Date2025), CreateHolding("C", 24000, _Date2025),
            CreateHolding("A", 10000, _Date2024), CreateHolding("B", 10000, _Date2024), CreateHolding("C", 10000, _Date2024)
        ];

        _Quotes = [
            CreateQuote("A", 24, _Date2025), CreateQuote("B", 7, _Date2025), CreateQuote("C", 70, _Date2025),
        ];

        List<double> changeFactors = [
            1.1, 1.2, 1.3, 0.7, 1.2, 1.4, 0.97, 1.24, 1.24, 1.7, 0.7, 1,
            1.2, 1.3, 0.7, 1.2, 1.4, 0.97, 1.24, 1.24, 1.7, 0.7, 1, 1.1
        ];

        for (int i = 0; i < 20; i++) {
            DateTime oldDate = i == 0 ? _Date2025 : _OtherDates[i - 1];
            DateTime newDate = _OtherDates[i];
            IList<Quote> quotes = [.. _Quotes.Where(q => q.Date == oldDate)];
            double changeFactor = changeFactors[i];
            foreach (Quote newQuote in
                        from quote in quotes
                        let priceInEuro = Math.Round(quote.PriceInEuro / changeFactor, 2)
                        select CreateQuote(quote.Security.SecurityId, priceInEuro, newDate)) {
                _Quotes.Add(newQuote);
            }
        }

        _Sut = new DevelopmentCalculator().WithHoldings(_Holdings).WithQuotes(_Quotes);
    }

    [TestMethod]
    public void CanFindLatestHoldingDate() {
        Assert.AreEqual(_Date2025, _Sut.HoldingDate);
    }

    [TestMethod]
    public void HaveQuotesWithDistinctDates() {
        IList<DateTime> dates = [.. _Quotes.Select(q => q.Date).Distinct()];
        Assert.HasCount(21, dates);
    }

    [TestMethod]
    public void CanPickADate() {
        DateTime minDate = _Date2025.AddYears(-10);
        HashSet<DateTime> pickedDates = [];
        for (int i = 0; i < 1970; i++) {
            DateTime date = _Sut.PickADate();
            Assert.IsLessThanOrEqualTo(_Date2025, date);
            Assert.IsGreaterThanOrEqualTo(minDate, date);
            pickedDates.Add(date);
        }

        Assert.HasCount(8, pickedDates);
    }

    [TestMethod]
    public void CanRunScenarioWithPickedDate() {
        var pickedDate = new DateTime(2019, 10, 23);
        Assert.Contains(pickedDate, _OtherDates);
        ScenarioResult scenarioResult = _Sut.CalculateScenario(pickedDate);
        Assert.IsNotNull(scenarioResult);
        IList<Holding> scenarioStartHoldings = scenarioResult.ScenarioStartHoldings;
        Assert.IsNotNull(scenarioStartHoldings);
        Assert.HasCount(3, scenarioStartHoldings);
        Assert.IsTrue(scenarioStartHoldings.All(h => h.NominalBalance > 0));
        Assert.IsTrue(scenarioStartHoldings.All(h => h.QuoteValueInEuro > 0));
        double sum = scenarioResult.SumStart();
        Assert.AreEqual(830760, sum);
        IList<Holding> scenarioEndHoldings = scenarioResult.ScenarioEndHoldings;
        Assert.IsNotNull(scenarioEndHoldings);
        Assert.HasCount(3, scenarioEndHoldings);
        Assert.IsTrue(scenarioEndHoldings.All(h => h.NominalBalance > 0));
        Assert.IsTrue(scenarioEndHoldings.All(h => h.QuoteValueInEuro > 0));
        sum = scenarioResult.SumEnd();
        Assert.AreEqual(1832880, sum);
        double averageYearlyChangeFactor = scenarioResult.AverageYearlyChangeFactor();
        Assert.AreEqual(1.1714714016774219, averageYearlyChangeFactor);
    }

    [TestMethod]
    public void CanMultipleScenariosAndGetMinimumMedianAndMaximum() {
        IList<DateTime> pickedDates = [
            new(2019, 10, 23), new(2018, 8, 15), new(2017, 6, 7)
        ];
        Assert.IsTrue(pickedDates.All(_OtherDates.Contains));

        ScenariosResult scenariosResult = _Sut.CalculateScenarios(pickedDates);
        Assert.AreEqual(1.1714714016774219, scenariosResult.MinimumAverageYearlyChangeFactor);
        Assert.AreEqual(1.2441153613307703, scenariosResult.MedianAverageYearlyChangeFactor);
        Assert.AreEqual(1.3112460931755978, scenariosResult.MaximumAverageYearlyChangeFactor);
    }

    [TestMethod]
    public void CanMultipleScenariosToFixedPoint() {
        ScenariosResult scenariosResult = _Sut.CalculateScenariosToFixedPoint();
        int numberOfDistinctChangeFactors = scenariosResult.NumberOfDistinctChangeFactors();
        Assert.IsGreaterThanOrEqualTo(8, numberOfDistinctChangeFactors);
        Assert.AreEqual(1.1222066946663809, scenariosResult.MinimumAverageYearlyChangeFactor);
        Assert.AreEqual(1.3112460931755978, scenariosResult.MaximumAverageYearlyChangeFactor);
        Assert.IsGreaterThan(scenariosResult.MinimumAverageYearlyChangeFactor, scenariosResult.MedianAverageYearlyChangeFactor);
        Assert.IsGreaterThan(scenariosResult.MedianAverageYearlyChangeFactor, scenariosResult.MaximumAverageYearlyChangeFactor);
    }

    private static Security CreateSecurity(string id) {
        return new Security { Guid = Guid.NewGuid().ToString(), QuotedPer = 1, SecurityId = id, SecurityName = id };
    }

    private Holding CreateHolding(string id, int nominal, DateTime date) {
        return new Holding { Security = _Securities[id], Date = date, NominalBalance = nominal };
    }

    private Quote CreateQuote(string id, double priceInEuro, DateTime date) {
        return new Quote { Security = _Securities[id], Date = date, PriceInEuro = priceInEuro };
    }
}
