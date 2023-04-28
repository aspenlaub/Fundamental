using System;
using System.Collections.Generic;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;

public class HoldingCalculator : IHoldingCalculator {
    protected List<Transaction> Transactions;
    protected List<Quote> Quotes;
    protected IList<Holding> Holdings;

    public HoldingCalculator() {
        Transactions = new List<Transaction>();
        Quotes = new List<Quote>();
    }

    public IHoldingCalculator WithTransactions(IList<Transaction> transactions) {
        Transactions.AddRange(transactions);
        return this;
    }

    public IHoldingCalculator WithQuotes(IList<Quote> quotes) {
        Quotes.AddRange(quotes);
        return this;
    }

    public IList<Holding> CalculateHoldings() {
        Holdings = new List<Holding>();
        foreach(var quote in Quotes) {
            UpdateHoldingsWithQuote(quote);
        }
        SortHoldings();
        return Holdings;
    }

    protected void SortHoldings() {
        Holdings = Holdings.OrderBy(x => x.Security.SecurityId).ThenBy(x => x.Date).ToList();
    }

    protected void UpdateHoldingsWithQuote(Quote quote) {
        SortTransactions();
        var transactions = TransactionsForQuote(quote);
        if (!transactions.Any()) { return; }
        var holding = HoldingForQuote(quote, HoldingForQuoteModes.Same);
        foreach(var transaction in transactions) {
            AddTransactionToHoldingWithQuote(transaction, holding, quote);
        }
        if (Math.Abs(holding.NominalBalance) > 0.001) { return; }
        var nextHolding = HoldingForQuote(quote, HoldingForQuoteModes.Next);
        if (nextHolding != null && Math.Abs(nextHolding.NominalBalance) > 0.001) { return; }
        if (nextHolding == null) {
            var lastHolding = HoldingForQuote(quote, HoldingForQuoteModes.LastBefore);
            if (lastHolding != null && Math.Abs(lastHolding.NominalBalance) > 0.001) { return; }
        }
        Holdings.Remove(holding);
    }

    protected void SortTransactions() {
        Transactions = Transactions.OrderBy(SortValue).ToList();
    }

    public string SortValue(Transaction transaction) {
        var s = transaction.Date.ToString("yyyyMMdd") + transaction.Security.SecurityId + (transaction.TransactionType == TransactionType.Sell ? '0' : '1');
        return s;
    }

    protected IList<Transaction> TransactionsForQuote(Quote quote) {
        return Transactions.Where(x => x.Security == quote.Security && x.Date <= quote.Date).ToList();
    }

    protected enum HoldingForQuoteModes {
        LastBefore, Same, Next
    }

    protected Holding HoldingForQuote(Quote quote, HoldingForQuoteModes mode) {
        DateTime date;
        switch (mode) {
            case HoldingForQuoteModes.LastBefore : {
                if (!Holdings.Any(x => x.Security == quote.Security && x.Date < quote.Date)) { return null; }
                date = Holdings.Where(x => x.Security == quote.Security && x.Date < quote.Date).Max(x => x.Date);
            }
                break;
            case HoldingForQuoteModes.Same : {
                date = quote.Date;
            }
                break;
            case HoldingForQuoteModes.Next : {
                if (!Holdings.Any(x => x.Security == quote.Security && x.Date > quote.Date)) { return null; }
                date = Holdings.Where(x => x.Security == quote.Security && x.Date > quote.Date).Min(x => x.Date);
            }
                break;
            default: {
                throw new NotImplementedException();
            }
        }
        var holding = Holdings.FirstOrDefault(x => x.Security.SecurityId == quote.Security.SecurityId && x.Date == date);
        if (holding != null) {
            return holding;
        }
        holding = new Holding { Date = quote.Date, Security = quote.Security, SecurityGuid = quote.SecurityGuid };
        Holdings.Add(holding);
        return holding;
    }

    protected void AddTransactionToHoldingWithQuote(Transaction transaction, Holding holding, Quote quote) {
        var transactionSign = transaction.TransactionType == TransactionType.Buy ? 1 : transaction.TransactionType == TransactionType.Sell ? -1 : 0;
        var nominalChange = transaction.Nominal * transactionSign;
        var newCostValue = holding.CostValueInEuro;
        double realizedProfitOrLoss = 0;
        switch (transactionSign) {
            case 1:
                newCostValue = Math.Round(newCostValue + nominalChange * transaction.PriceInEuro / transaction.Security.QuotedPer, 2);
                break;
            case -1:
                newCostValue = Math.Round((holding.NominalBalance + nominalChange) * holding.CostValueInEuro / holding.NominalBalance, 2);
                realizedProfitOrLoss = Math.Round(nominalChange * holding.CostValueInEuro / holding.NominalBalance
                                                  - nominalChange * transaction.PriceInEuro / transaction.Security.QuotedPer , 2);
                break;
        }
        var realizedLoss = Math.Round(transaction.ExpensesInEuro - (realizedProfitOrLoss < 0 ? realizedProfitOrLoss : 0), 2);
        var realizedProfit = Math.Round(transaction.IncomeInEuro + (realizedProfitOrLoss > 0 ? realizedProfitOrLoss : 0), 2);
        holding.NominalBalance = Math.Round(holding.NominalBalance + nominalChange, 2);
        holding.CostValueInEuro = newCostValue;
        holding.RealizedLossInEuro = Math.Round(holding.RealizedLossInEuro + realizedLoss, 2);
        holding.RealizedProfitInEuro = Math.Round(holding.RealizedProfitInEuro + realizedProfit, 2);
        holding.QuoteValueInEuro = Math.Round(holding.NominalBalance * quote.PriceInEuro / holding.Security.QuotedPer, 2);
        holding.UnrealizedLossInEuro = Math.Round(Math.Max(0, holding.CostValueInEuro - holding.QuoteValueInEuro), 2);
        holding.UnrealizedProfitInEuro = Math.Round(Math.Max(0, holding.QuoteValueInEuro - holding.CostValueInEuro), 2);
    }
}