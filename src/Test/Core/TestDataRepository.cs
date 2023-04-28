using System;
using System.Collections.ObjectModel;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;

public class TestDataRepository {
    public const string ShareId = "593703", BondId = "113973";
    public const string ShareGuid = "E4A3C7CF-9660-CCE1-8197-EAB4E5D2655D", BondGuid = "A8E59EAB-E816-6211-FF77-A9C0CF697AC3";

    public static readonly DateTime LowDate = new(1970, 7, 24);
    public static readonly DateTime NoHoldingsDate = new(1997, 6, 2), InitialBuysDate = new(1997, 7, 24), QuoteAndPartialSellDate = new(1997, 9, 1);
    public static readonly DateTime QuoteOnlyDate = new(1997, 12, 3), CouponAndDividendDate = new(1997, 12, 31), SellBondDate = new(1998, 1, 24);
    public static readonly DateTime StockSplitDate = new(1998, 2, 7), LastQuoteDate = new(1998, 4, 1), HighDate = new(2040, 7, 24);

    public ObservableCollection<Security> Securities { get; }
    public ObservableCollection<Quote> Quotes { get; }
    public ObservableCollection<Transaction> Transactions { get; }

    public TestDataRepository() {
        Securities = new ObservableCollection<Security>() {
            new() { SecurityId = ShareId, SecurityName ="MAN VZ", QuotedPer = 1, Guid = ShareGuid },
            new() { SecurityId = BondId, SecurityName = "3,00-7,00% BSB A 97/3 SL1/1", QuotedPer = 100, Guid = BondGuid },
        };
        Quotes = new ObservableCollection<Quote>();
        AddQuote(NoHoldingsDate, ShareId, 202.98);
        AddQuote(NoHoldingsDate, BondId, 100);
        AddQuote(QuoteAndPartialSellDate, ShareId, 209.11);
        AddQuote(QuoteAndPartialSellDate, BondId, 100);
        AddQuote(QuoteOnlyDate, ShareId, 218.83);
        AddQuote(QuoteOnlyDate, BondId, 100);
        AddQuote(LastQuoteDate, ShareId, 23.86);
        AddQuote(LastQuoteDate, BondId, 100);
        Transactions = new ObservableCollection<Transaction>();
        AddTransaction(InitialBuysDate, ShareId, TransactionType.Buy, 10, 204.7, 42.7, 0);
        AddTransaction(InitialBuysDate, BondId, TransactionType.Buy, 2070, 100, 4.27, 0);
        AddTransaction(QuoteAndPartialSellDate, ShareId, TransactionType.Sell, 3, 209.24, 24.7, 0);
        AddTransaction(CouponAndDividendDate, BondId, TransactionType.Coupon, 0, 0, 0, 240.7);
        AddTransaction(CouponAndDividendDate, ShareId, TransactionType.Dividend, 0, 0, 0, 420.7);
        AddTransaction(SellBondDate, BondId, TransactionType.Sell, 2070, 100, 7.44, 0);
        AddTransaction(StockSplitDate, ShareId, TransactionType.Sell, 7, 214.7, 0, 0);
        AddTransaction(StockSplitDate, ShareId, TransactionType.Buy, 70, 21.47, 0, 0);
    }

    protected void AddQuote(DateTime date, string securityId, double price) {
        var security = Securities.FirstOrDefault(x => x.SecurityId == securityId);
        Assert.IsNotNull(security);
        Quotes.Add(new Quote() { Date = date, Security = security, PriceInEuro = price });
    }

    protected void AddTransaction(DateTime date, string securityId, TransactionType transactionType, double nominal, double price, double expenses, double income) {
        var security = Securities.FirstOrDefault(x => x.SecurityId == securityId);
        Assert.IsNotNull(security);
        Transactions.Add(new Transaction { Date = date, Security = security, TransactionType = transactionType, Nominal = nominal, PriceInEuro = price, ExpensesInEuro = expenses, IncomeInEuro = income });
    }
}