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
        Assert.HasCount(2, Repository.Securities);
        Assert.HasCount(8, Repository.Transactions);
        Assert.HasCount(8, Repository.Quotes);
    }

    [TestMethod]
    public void NoHoldingsOnDateWithoutHoldings() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, NoHoldingsDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, HighDate, []))
                                  .CalculateHoldings();
        Assert.IsEmpty(holdings);
    }

    [TestMethod]
    public void NoHoldingsWithoutTransactions() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, HighDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, LowDate, []))
                                  .CalculateHoldings();
        Assert.IsEmpty(holdings);
    }

    [TestMethod]
    public void NoHoldingsOnInitialBuysDate() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, InitialBuysDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, InitialBuysDate, []))
                                  .CalculateHoldings();
        Assert.IsEmpty(holdings);
    }

    [TestMethod]
    public void OneBondHoldingOnFirstQuoteDateWithHoldings() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, InitialBuysDate, [TestDataRepository.BondId]))
                                  .CalculateHoldings();
        Assert.HasCount(1, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void OneShareHoldingOnFirstQuoteDateWithHoldings() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, [TestDataRepository.ShareId]))
                                  .CalculateHoldings();
        Assert.HasCount(1, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void OneShareHoldingOnFirstQuoteDateWithHoldingsAndTransactionsInReverseOrder() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                                  .WithTransactions([.. TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, [TestDataRepository.ShareId]).Reverse()])
                                  .CalculateHoldings();
        Assert.HasCount(1, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void TwoBondHoldingsUntilSecondQuoteDate() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, QuoteOnlyDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, QuoteOnlyDate, [TestDataRepository.BondId]))
                                  .CalculateHoldings();
        Assert.HasCount(2, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void TwoBondHoldingsUntilSecondQuoteDateWithTransactionsInReverseOrder() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, QuoteOnlyDate))
                                  .WithTransactions([.. TransactionsInPeriod(LowDate, QuoteOnlyDate, [TestDataRepository.BondId]).Reverse()])
                                  .CalculateHoldings();
        Assert.HasCount(2, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void TwoBondHoldingsUntilSecondQuoteDateWithQuotesInReverseOrder() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes([.. QuotesInPeriod(LowDate, QuoteOnlyDate).Reverse()])
                                  .WithTransactions(TransactionsInPeriod(LowDate, QuoteOnlyDate, [TestDataRepository.BondId]))
                                  .CalculateHoldings();
        Assert.HasCount(2, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
    }

    [TestMethod]
    public void OneBondOneShareHoldingOnFirstQuoteDateWithHoldings() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, QuoteAndPartialSellDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, []))
                                  .CalculateHoldings();
        Assert.HasCount(2, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void OneBondOneShareHoldingOnFirstQuoteDateWithHoldingsInReverseOrder() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes([.. QuotesInPeriod(LowDate, QuoteAndPartialSellDate).Reverse()])
                                  .WithTransactions(TransactionsInPeriod(LowDate, QuoteAndPartialSellDate, []))
                                  .CalculateHoldings();
        Assert.HasCount(2, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
    }

    [TestMethod]
    public void OneEmptyBondHoldingAfterSell() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, LastQuoteDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, LastQuoteDate, [TestDataRepository.BondId]))
                                  .CalculateHoldings();
        Assert.HasCount(3, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, BondId, 2070, 2070, 2070, 4.27, 0, 0, 0));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[2], LastQuoteDate, BondId, 0, 0, 0, 11.71, 240.7, 0, 0));
    }

    [TestMethod]
    public void StockSplitIsHandledCorrectly() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, LastQuoteDate))
                                  .WithTransactions(TransactionsInPeriod(LowDate, LastQuoteDate, [TestDataRepository.ShareId]))
                                  .CalculateHoldings();
        Assert.HasCount(3, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, ShareId, 7, 1432.9, 1531.81, 67.4, 13.62, 0, 98.91));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[2], LastQuoteDate, ShareId, 70, 1502.9, 1670.2, 67.4, 504.32, 0, 167.30));
    }

    [TestMethod]
    public void StockSplitIsHandledCorrectlyWithTransactionsInReverseOrder() {
        IList<Holding> holdings = HoldingCalculator
                                  .WithQuotes(QuotesInPeriod(LowDate, LastQuoteDate))
                                  .WithTransactions([.. TransactionsInPeriod(LowDate, LastQuoteDate, [TestDataRepository.ShareId]).Reverse()])
                                  .CalculateHoldings();
        Assert.HasCount(3, holdings);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[0], QuoteAndPartialSellDate, ShareId, 7, 1432.9, 1463.77, 67.4, 13.62, 0, 30.87));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[1], QuoteOnlyDate, ShareId, 7, 1432.9, 1531.81, 67.4, 13.62, 0, 98.91));
        Assert.IsTrue(Helper.IsHoldingEqualTo(holdings[2], LastQuoteDate, ShareId, 70, 1502.9, 1670.2, 67.4, 504.32, 0, 167.30));
    }

    protected IList<Quote> QuotesInPeriod(DateTime fromDate, DateTime toDate) {
        return [.. Repository.Quotes.Where(x => x.Date >= fromDate && x.Date <= toDate)];
    }

    protected IList<Transaction> TransactionsInPeriod(DateTime fromDate, DateTime toDate, string[] securityIds) {
        IEnumerable<Transaction> transactions = Repository.Transactions.Where(x => x.Date >= fromDate && x.Date <= toDate);
        if (securityIds.Any()) {
            transactions = transactions.Where(x => securityIds.Contains(x.Security.SecurityId));
        }
        return [.. transactions];
    }
}