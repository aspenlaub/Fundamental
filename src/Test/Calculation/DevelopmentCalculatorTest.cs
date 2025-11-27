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
            1.1, 1.2, 1.3, 0.7, 1.2, 1.4, 0.24, 1.24, 1.24, 1.7, 0.7, 1,
            1.2, 1.3, 0.7, 1.2, 1.4, 0.24, 1.24, 1.24, 1.7, 0.7, 1, 1.1
        ];

        for (int i = 0; i < 20; i++) {
            DateTime oldDate = i == 0 ? _Date2025 : _OtherDates[i - 1];
            DateTime newDate = _OtherDates[i];
            IList<Quote> quotes = _Quotes.Where(q => q.Date == oldDate).ToList();
            double changeFactor = changeFactors[i];
            foreach (Quote quote in quotes) {
                double priceInEuro = Math.Round(quote.PriceInEuro / changeFactor, 2);
                Quote newQuote = CreateQuote(quote.Security.SecurityId, priceInEuro, newDate);
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
        IList<DateTime> dates = _Quotes.Select(q => q.Date).Distinct().ToList();
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

        Assert.IsGreaterThan(14, pickedDates.Count);
    }
    /*
    (OK) Nimm zufällig einen Tag aus den letzten 10 Jahren, ausgenommen die letzten x Jahre
    (REQ) Angenommen an dem Tag hätte das Depot die VMPH Komposition gehabt
    (REQ) Was wäre der Kurswert x Jahre später gewesen
    (REQ) Errechne durchschnittliches Wachstum oder Verlustrate pro Jahr
    (REQ) Ergebnis: Minimum, Median, Maximum     
    */

    private Security CreateSecurity(string id) {
        return new Security { Guid = Guid.NewGuid().ToString(), QuotedPer = 1, SecurityId = id, SecurityName = id };
    }

    private Holding CreateHolding(string id, int nominal, DateTime date) {
        return new Holding { Security = _Securities[id], Date = date, NominalBalance = nominal };
    }

    private Quote CreateQuote(string id, double priceInEuro, DateTime date) {
        return new Quote { Security = _Securities[id], Date = date, PriceInEuro = priceInEuro };
    }
}
