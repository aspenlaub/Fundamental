using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Core;
using Microsoft.EntityFrameworkCore;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.DbOperations;

public class Dumper {
    public const string SecuritiesFileName = "Securities.txt", QuotesFileName = "Quotes.txt";
    public const string TransactionsFileName = "Transactions.txt", TransactionsJsonFileName = "Transactions.json";

    public void DumpSecurities(IFolder folder, IList<Security> securities) {
        const string shortFileName = SecuritiesFileName;
        var contents = new List<string> {
            string.Join(";", Properties.Resources.Securities_Dump_SecurityId, Properties.Resources.Securities_Dump_SecurityName, Properties.Resources.Securities_Dump_QuotedPer)
        };
        contents.AddRange(securities.OrderBy(x => x.SecurityId).Select(security => security.SecurityId + ';' + security.SecurityName + ';' + security.QuotedPer.ToString(CultureInfo.CurrentCulture)));
        var textFileWriter = new TextFileWriter();
        textFileWriter.WriteAllLines(folder, shortFileName, contents, Encoding.UTF8);
    }

    public IList<Security> ReadSecurities(string folder) {
        var fileName = folder + '\\' + SecuritiesFileName;
        if (!File.Exists(fileName)) { return new List<Security>(); }

        var securities = new List<Security>();
        int indexSecurityId = -1, indexSecurityName = -1, indexQuotedPer = -1;
        foreach (var line in File.ReadAllLines(fileName)) {
            if (indexSecurityId < 0) {
                var headers = line.Split(';').ToList();
                indexSecurityId = headers.IndexOf(Properties.Resources.Securities_Dump_SecurityId);
                indexSecurityName = headers.IndexOf(Properties.Resources.Securities_Dump_SecurityName);
                indexQuotedPer = headers.IndexOf(Properties.Resources.Securities_Dump_QuotedPer);
                if (indexSecurityId < 0 || indexSecurityName < 0 || indexQuotedPer < 0) { return new List<Security>(); }

                continue;
            }

            var data = line.Split(';').ToList();
            var security = new Security() {
                SecurityId = data[indexSecurityId],
                SecurityName = data[indexSecurityName]
            };
            if (!double.TryParse(data[indexQuotedPer], out var quotedPer)) { return new List<Security>(); }

            security.QuotedPer = quotedPer;
            securities.Add(security);
        }

        return securities;
    }

    public void DumpQuotes(IFolder folder, IList<Quote> quotes) {
        const string shortFileName = QuotesFileName;
        var contents = new List<string> {
            string.Join(";", Properties.Resources.Quotes_Dump_Date, Properties.Resources.Quotes_Dump_SecurityId, Properties.Resources.Quotes_Dump_PriceInEuro)
        };
        contents.AddRange(quotes.OrderBy(x => x.Date.ToString("yyyyMMdd") + x.Security.SecurityId).Select(quote => quote.Date.ToString("dd.MM.yyyy") + ';' + quote.Security.SecurityId + ';' + quote.PriceInEuro.ToString(CultureInfo.CurrentCulture)));

        var textFileWriter = new TextFileWriter();
        textFileWriter.WriteAllLines(folder, shortFileName, contents, Encoding.UTF8);
    }

    public IList<Quote> ReadQuotes(string folder, DbSet<Security> securities) {
        var fileName = folder + '\\' + QuotesFileName;
        if (!File.Exists(fileName)) { return new List<Quote>(); }

        var quotes = new List<Quote>();
        int indexDate = -1, indexSecurityId = -1, indexPriceInEuro = -1;
        foreach (var line in File.ReadAllLines(fileName)) {
            if (indexDate < 0) {
                var headers = line.Split(';').ToList();
                indexDate = headers.IndexOf(Properties.Resources.Quotes_Dump_Date);
                indexSecurityId = headers.IndexOf(Properties.Resources.Quotes_Dump_SecurityId);
                indexPriceInEuro = headers.IndexOf(Properties.Resources.Quotes_Dump_PriceInEuro);
                if (indexDate < 0 || indexSecurityId < 0 || indexPriceInEuro < 0) { return new List<Quote>(); }

                continue;
            }

            var data = line.Split(';').ToList();
            if (!DateTime.TryParse(ConvertDate(data[indexDate]), out var date)) { return new List<Quote>(); }

            var securityId = data[indexSecurityId];
            var security = securities.FirstOrDefault(x => x.SecurityId == securityId);
            if (security == null) { return new List<Quote>(); }

            if (!double.TryParse(data[indexPriceInEuro], out var price)) { return new List<Quote>(); }

            var quote = new Quote() {
                Date = date,
                Security = security,
                PriceInEuro = price
            };
            quotes.Add(quote);
        }

        return quotes;
    }

    public void DumpTransactions(IFolder folder, IList<Transaction> transactions) {
        var contents = new List<string> {
            string.Join(";", Properties.Resources.Transactions_Dump_Date, Properties.Resources.Transactions_Dump_SecurityId, Properties.Resources.Transactions_Dump_Type, Properties.Resources.Transactions_Dump_Nominal, Properties.Resources.Transactions_Dump_PriceInEuro, Properties.Resources.Transactions_Dump_Loss, Properties.Resources.Transactions_Dump_Profit)
        };
        transactions = transactions.OrderBy(x => x.Date.ToString("yyyyMMdd") + x.Security.SecurityId + (x.TransactionType == TransactionType.Sell ? '0' : '1')).ToList();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var transaction in transactions) {
            contents.Add(transaction.Date.ToString("dd.MM.yyyy") + ';' + transaction.Security.SecurityId
                         + ';' + Enum.GetName(typeof(TransactionType), transaction.TransactionType)
                         + ';' + transaction.Nominal + ';' + transaction.PriceInEuro
                         + ';' + transaction.ExpensesInEuro + ';' + transaction.IncomeInEuro);
        }

        var textFileWriter = new TextFileWriter();
        textFileWriter.WriteAllLines(folder, TransactionsFileName, contents, Encoding.UTF8);

        var transactionDtos = transactions.Select(t => new TransactionDto(t)).ToList();
        var json = JsonSerializer.Serialize(transactionDtos, new JsonSerializerOptions { WriteIndented = true });
        textFileWriter.WriteAllLines(folder, TransactionsJsonFileName, new List<string> { json }, Encoding.UTF8);
    }

    public IList<Transaction> ReadTransactions(string folder, DbSet<Security> securities) {
        var fileName = folder + '\\' + TransactionsFileName;
        if (!File.Exists(fileName)) { return new List<Transaction>(); }

        var transactions = new List<Transaction>();
        int indexDate = -1, indexSecurityId = -1, indexType = -1, indexNominal = -1, indexPriceInEuro = -1, indexLoss = -1, indexProfit = -1;
        foreach (var line in File.ReadAllLines(fileName)) {
            if (indexDate < 0) {
                var headers = line.Split(';').ToList();
                indexDate = headers.IndexOf(Properties.Resources.Transactions_Dump_Date);
                indexSecurityId = headers.IndexOf(Properties.Resources.Transactions_Dump_SecurityId);
                indexType = headers.IndexOf(Properties.Resources.Transactions_Dump_Type);
                indexNominal = headers.IndexOf(Properties.Resources.Transactions_Dump_Nominal);
                indexPriceInEuro = headers.IndexOf(Properties.Resources.Transactions_Dump_PriceInEuro);
                indexLoss = headers.IndexOf(Properties.Resources.Transactions_Dump_Loss);
                indexProfit = headers.IndexOf(Properties.Resources.Transactions_Dump_Profit);
                if (indexDate < 0 || indexSecurityId < 0 || indexType < 0 || indexNominal < 0 || indexPriceInEuro < 0 || indexLoss < 0 || indexProfit < 0) { return new List<Transaction>(); }

                continue;
            }

            var data = line.Split(';').ToList();
            if (data.Count != 7) { continue; }

            if (!DateTime.TryParse(ConvertDate(data[indexDate]), out var date)) { return new List<Transaction>(); }

            var securityId = data[indexSecurityId];
            var security = securities.FirstOrDefault(x => x.SecurityId == securityId);
            if (security == null) { return new List<Transaction>(); }

            if (!Enum.TryParse(data[indexType], out TransactionType transactionType)) { return new List<Transaction>(); }

            if (!double.TryParse(data[indexNominal], out var nominal)) { return new List<Transaction>(); }
            if (!double.TryParse(data[indexPriceInEuro], out var price)) { return new List<Transaction>(); }
            if (!double.TryParse(data[indexLoss], out var loss)) { return new List<Transaction>(); }
            if (!double.TryParse(data[indexProfit], out var profit)) { return new List<Transaction>(); }

            var transaction = new Transaction {
                Date = date,
                Security = security,
                TransactionType = transactionType,
                Nominal = nominal,
                PriceInEuro = price,
                ExpensesInEuro = loss,
                IncomeInEuro = profit
            };
            transactions.Add(transaction);
        }

        return transactions;
    }

    protected string ConvertDate(string s) {
        if (s.Length != 10) { return s; }
        if (s[2] != '.' || s[5] != '.') { return s; }

        s = s.Substring(6) + '-' + s.Substring(3, 2) + '-' + s.Substring(0, 2);
        return s;
    }
}