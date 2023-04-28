using System;
using System.Collections.Generic;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;

[TestClass]
public class HoldingCalculatorTest {
    public const string ShareId = TestDataRepository.ShareId, BondId = TestDataRepository.BondId;

    public static readonly DateTime LowDate = TestDataRepository.LowDate;
    public static readonly DateTime NoHoldingsDate = TestDataRepository.NoHoldingsDate, InitialBuysDate = TestDataRepository.InitialBuysDate;
    public static readonly DateTime QuoteAndPartialSellDate = TestDataRepository.QuoteAndPartialSellDate, QuoteOnlyDate = TestDataRepository.QuoteOnlyDate;
    public static readonly DateTime CouponAndDividendDate = TestDataRepository.CouponAndDividendDate, SellBondDate = TestDataRepository.SellBondDate;
    public static readonly DateTime StockSplitDate = TestDataRepository.StockSplitDate, LastQuoteDate = TestDataRepository.LastQuoteDate;
    public static readonly DateTime HighDate = TestDataRepository.HighDate;

    protected TestDataRepository Repository;
    protected IHoldingCalculator HoldingCalculator;
    protected TestHelper Helper;

    [TestInitialize]
    public void Initialize() {
        Repository = new TestDataRepository();
        HoldingCalculator = new HoldingCalculator();
        Helper = new TestHelper(new ContextFactory());
    }

    [TestMethod]
    public void CanCreateTestDataRepository() {
        Assert.AreEqual(2, Repository.Securities.Count);
        Assert.AreEqual(8, Repository.Transactions.Count);
        Assert.AreEqual(8, Repository.Quotes.Count);
    }

    [TestMethod]
    public void NoHoldingsOnDateWithoutHoldings() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, NoHoldingsDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, HighDate, new string[] { }))
                       .CalculateHoldings();
        Assert.AreEqual(0, holdings.Count);
    }

    [TestMethod]
    public void NoHoldingsWithoutTransactions() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, HighDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, LowDate, new string[] { }))
                       .CalculateHoldings();
        Assert.AreEqual(0, holdings.Count);
    }

    [TestMethod]
    public void NoHoldingsOnInitialBuysDate() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, InitialBuysDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, InitialBuysDate, new string[] { }))
                       .CalculateHoldings();
        Assert.AreEqual(0, holdings.Count);
    }

    [TestMethod]
    public void OneBondHoldingOnFirstQuoteDateWithHoldings() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, InitialBuysDate, new[] { TestDataRepository.BondId }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(1, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void OneShareHoldingOnFirstQuoteDateWithHoldings() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, new[] { TestDataRepository.ShareId }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(1, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void OneShareHoldingOnFirstQuoteDateWithHoldingsAndTransactionsInReverseOrder() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, new[] { TestDataRepository.ShareId }).Reverse().ToList())
                       .CalculateHoldings();
        Assert.AreEqual(1, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void TwoBondHoldingsUntilSecondQuoteDate() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteOnlyDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, QuoteOnlyDate, new[] { TestDataRepository.BondId }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(2, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void TwoBondHoldingsUntilSecondQuoteDateWithTransactionsInReverseOrder() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteOnlyDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, QuoteOnlyDate, new[] { TestDataRepository.BondId }).Reverse().ToList())
                       .CalculateHoldings();
        Assert.AreEqual(2, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void TwoBondHoldingsUntilSecondQuoteDateWithQuotesInReverseOrder() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteOnlyDate).Reverse().ToList())
                       .WithTransactions(TransactionsInPeriod(LowDate, QuoteOnlyDate, new[] { TestDataRepository.BondId }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(2, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void OneBondOneShareHoldingOnFirstQuoteDateWithHoldings() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, new string[] { }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(2, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void OneBondOneShareHoldingOnFirstQuoteDateWithHoldingsInReverseOrder() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate).Reverse().ToList())
                       .WithTransactions(TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, new string[] { }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(2, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void OneEmptyBondHoldingAfterSell() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, LastQuoteDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, LastQuoteDate, new[] { TestDataRepository.BondId }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(3, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[2], LastQuoteDate, BondId, 0, 0, 0, 11.71, 240.7, 0, 0));
    }

    [TestMethod]
    public void StockSplitIsHandledCorrectly() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, LastQuoteDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, LastQuoteDate, new[] { TestDataRepository.ShareId }).ToList())
                       .CalculateHoldings();
        Assert.AreEqual(3, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, ShareId, 7, 1432.9, 1531.81, 67.4, 13.62, 0, 98.91));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[2], LastQuoteDate, ShareId, 70, 1502.9, 1670.2, 67.4, 504.32, 0, 167.30));
    }

    [TestMethod]
    public void StockSplitIsHandledCorrectlyWithTransactionsInReverseOrder() {
        var holdings = HoldingCalculator
                       .WithQuotes(QuotesInPeriod(LowDate, LastQuoteDate))
                       .WithTransactions(TransactionsInPeriod(LowDate, LastQuoteDate, new[] { TestDataRepository.ShareId }).Reverse().ToList())
                       .CalculateHoldings();
        Assert.AreEqual(3, holdings.Count);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, ShareId, 7, 1432.9, 1531.81, 67.4, 13.62, 0, 98.91));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[2], LastQuoteDate, ShareId, 70, 1502.9, 1670.2, 67.4, 504.32, 0, 167.30));
    }

    protected IList<Quote> QuotesInPeriod(DateTime fromDate, DateTime toDate) {
        return Repository.Quotes.Where(x => x.Date >= fromDate && x.Date <= toDate).ToList();
    }

    protected IList<Transaction> TransactionsInPeriod(DateTime fromDate, DateTime toDate, string[] securityIds) {
        var transactions = Repository.Transactions.Where(x => x.Date >= fromDate && x.Date <= toDate);
        if (securityIds.Any()) {
            transactions = transactions.Where(x => securityIds.Contains(x.Security.SecurityId));
        }
        return transactions.ToList();
    }
}