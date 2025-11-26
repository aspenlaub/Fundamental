using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]
namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;

[TestClass]
public class DateSummaryCalculatorTest {
    protected IDateSummaryCalculator DateSummaryCalculator;

    [TestInitialize]
    public void Initialize() {
        DateSummaryCalculator = new DateSummaryCalculator();
    }

    [TestMethod]
    public void NewCalculatorDoesNotProduceAnyResults() {
        Assert.IsEmpty(DateSummaryCalculator.CalculateDateSummaries());
    }

    [TestMethod]
    public void SingleHoldingIsSummary() {
        Holding holding = CreateTestHolding(new DateTime(2016, 2, 24), 1);
        IList<DateSummary> summaries = DateSummaryCalculator.WithHoldings([holding]).CalculateDateSummaries();
        Assert.HasCount(1, summaries);
        DateSummary summary = summaries[0];
        Assert.AreEqual(holding.CostValueInEuro, summary.CostValueInEuro);
        Assert.AreEqual(holding.Date, summary.Date);
        Assert.AreEqual(holding.RealizedLossInEuro, summary.RealizedLossInEuro);
        Assert.AreEqual(holding.RealizedProfitInEuro, summary.RealizedProfitInEuro);
        Assert.AreEqual(holding.UnrealizedLossInEuro, summary.UnrealizedLossInEuro);
        Assert.AreEqual(holding.UnrealizedProfitInEuro, summary.UnrealizedProfitInEuro);
    }

    [TestMethod]
    public void TwoHoldingsOnSameDateProduceOneSummary() {
        Holding holding = CreateTestHolding(new DateTime(2016, 2, 24), 1);
        Holding holding2 = CreateTestHolding(new DateTime(2016, 2, 24), 2);
        IList<DateSummary> summaries = DateSummaryCalculator.WithHoldings([ holding, holding2 ]).CalculateDateSummaries();
        Assert.HasCount(1, summaries);
        DateSummary summary = summaries[0];
        Assert.AreEqual(holding.CostValueInEuro + holding2.CostValueInEuro, summary.CostValueInEuro);
        Assert.AreEqual(holding.Date, summary.Date);
        Assert.AreEqual(holding.RealizedLossInEuro + holding2.RealizedLossInEuro, summary.RealizedLossInEuro);
        Assert.AreEqual(holding.RealizedProfitInEuro + holding2.RealizedProfitInEuro, summary.RealizedProfitInEuro);
        Assert.AreEqual(holding.UnrealizedLossInEuro + holding2.UnrealizedLossInEuro, summary.UnrealizedLossInEuro);
        Assert.AreEqual(holding.UnrealizedProfitInEuro + holding2.UnrealizedProfitInEuro, summary.UnrealizedProfitInEuro);
    }

    [TestMethod]
    public void TwoHoldingsOnDifferentDatesProduceTwoSummaries() {
        Holding holding = CreateTestHolding(new DateTime(2016, 2, 20), 1);
        Holding holding2 = CreateTestHolding(new DateTime(2016, 2, 24), 2);
        IList<DateSummary> summaries = DateSummaryCalculator.WithHoldings([ holding, holding2 ]).CalculateDateSummaries();
        Assert.HasCount(2, summaries);
        Assert.AreEqual(holding.Date, summaries[0].Date);
        Assert.AreEqual(holding2.Date, summaries[1].Date);
    }

    [TestMethod]
    public void TwoHoldingsOnDifferentDatesInReverseOrderProduceTwoSummariesInCorrectOrder() {
        Holding holding = CreateTestHolding(new DateTime(2016, 2, 24), 2);
        Holding holding2 = CreateTestHolding(new DateTime(2016, 2, 20), 1);
        IList<DateSummary> summaries = DateSummaryCalculator.WithHoldings([ holding, holding2 ]).CalculateDateSummaries();
        Assert.HasCount(2, summaries);
        Assert.AreEqual(holding2.Date, summaries[0].Date);
        Assert.AreEqual(holding.Date, summaries[1].Date);
    }

    [TestMethod]
    public void ZeroHoldingsDoNotProduceDateSummaries() {
        Holding holding = CreateTestHolding(new DateTime(2016, 2, 24), 1);
        holding.NominalBalance = 0;
        IList<DateSummary> summaries = DateSummaryCalculator.WithHoldings([ holding ]).CalculateDateSummaries();
        Assert.IsEmpty(summaries);
    }

    private static Holding CreateTestHolding(DateTime date, uint number) {
        return new Holding {
            CostValueInEuro = number, NominalBalance = 2 * number, QuoteValueInEuro = 3 * number, Date = date,
            RealizedLossInEuro = 4 * number, RealizedProfitInEuro = 5 * number, UnrealizedLossInEuro = 6 * number, UnrealizedProfitInEuro = 7 * number
        };
    }
}