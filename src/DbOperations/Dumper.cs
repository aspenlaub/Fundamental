using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Properties;
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
            string.Join(";", Resources.Securities_Dump_SecurityId, Resources.Securities_Dump_SecurityName, Resources.Securities_Dump_QuotedPer)
        };
        contents.AddRange(securities.OrderBy(x => x.SecurityId).Select(security => security.SecurityId + ';' + security.SecurityName + ';' + security.QuotedPer.ToString(CultureInfo.CurrentCulture)));
        var textFileWriter = new TextFileWriter();
        textFileWriter.WriteAllLines(folder, shortFileName, contents, Encoding.UTF8);
    }

    public IList<Security> ReadSecurities(string folder, IErrorsAndInfos errorsAndInfos) {
        string fileName = folder + '\\' + SecuritiesFileName;
        if (!File.Exists(fileName)) {
            return new List<Security>();
        }

        var securities = new List<Security>();
        int indexSecurityId = -1, indexSecurityName = -1, indexQuotedPer = -1;
        foreach (string line in File.ReadAllLines(fileName)) {
            if (indexSecurityId < 0) {
                var headers = line.Split(';').ToList();
                indexSecurityId = headers.IndexOf(Resources.Securities_Dump_SecurityId);
                indexSecurityName = headers.IndexOf(Resources.Securities_Dump_SecurityName);
                indexQuotedPer = headers.IndexOf(Resources.Securities_Dump_QuotedPer);
                if (indexSecurityId < 0 || indexSecurityName < 0 || indexQuotedPer < 0) {
                    errorsAndInfos.Errors.Add($"Securities file cannot be read. Index ID is {indexSecurityId}, Name is {indexSecurityName}, Quoted is {indexQuotedPer}");
                    return new List<Security>();
                }

                continue;
            }

            var data = line.Split(';').ToList();
            var security = new Security {
                SecurityId = data[indexSecurityId],
                SecurityName = data[indexSecurityName]
            };
            if (!double.TryParse(data[indexQuotedPer], out double quotedPer)) {
                errorsAndInfos.Errors.Add($"Cannot parse {data[indexQuotedPer]} as double");
                continue;
            }

            security.QuotedPer = quotedPer;
            securities.Add(security);
        }

        return securities;
    }

    public void DumpQuotes(IFolder folder, IList<Quote> quotes) {
        const string shortFileName = QuotesFileName;
        var contents = new List<string> {
            string.Join(";", Resources.Quotes_Dump_Date, Resources.Quotes_Dump_SecurityId, Resources.Quotes_Dump_PriceInEuro)
        };
        contents.AddRange(quotes.OrderBy(x => x.Date.ToString("yyyyMMdd") + x.Security.SecurityId).Select(quote => quote.Date.ToString("dd.MM.yyyy") + ';' + quote.Security.SecurityId + ';' + quote.PriceInEuro.ToString(CultureInfo.CurrentCulture)));

        var textFileWriter = new TextFileWriter();
        textFileWriter.WriteAllLines(folder, shortFileName, contents, Encoding.UTF8);
    }

    public IList<Quote> ReadQuotes(string folder, DbSet<Security> securities, IErrorsAndInfos errorsAndInfos) {
        string fileName = folder + '\\' + QuotesFileName;
        if (!File.Exists(fileName)) {
            return new List<Quote>();
        }

        var quotes = new List<Quote>();
        int indexDate = -1, indexSecurityId = -1, indexPriceInEuro = -1;
        foreach (string line in File.ReadAllLines(fileName)) {
            if (indexDate < 0) {
                var headers = line.Split(';').ToList();
                indexDate = headers.IndexOf(Resources.Quotes_Dump_Date);
                indexSecurityId = headers.IndexOf(Resources.Quotes_Dump_SecurityId);
                indexPriceInEuro = headers.IndexOf(Resources.Quotes_Dump_PriceInEuro);
                if (indexDate < 0 || indexSecurityId < 0 || indexPriceInEuro < 0) {
                    errorsAndInfos.Errors.Add($"Quotes file cannot be read. Index Date is {indexDate}, ID is {indexSecurityId}, Price is {indexPriceInEuro}");
                    return new List<Quote>();
                }

                continue;
            }

            var data = line.Split(';').ToList();
            if (!DateTime.TryParse(ConvertDate(data[indexDate]), out DateTime date)) {
                errorsAndInfos.Errors.Add($"Cannot parse {ConvertDate(data[indexDate])} as DateTime");
                continue;
            }

            string securityId = data[indexSecurityId];
            Security security = securities.FirstOrDefault(x => x.SecurityId == securityId);
            if (security == null) {
                errorsAndInfos.Errors.Add($"Cannot find security {securityId}");
                continue;
            }

            if (!double.TryParse(data[indexPriceInEuro], out double price)) {
                errorsAndInfos.Errors.Add($"Cannot parse {data[indexPriceInEuro]} as double");
                continue;
            }

            var quote = new Quote {
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
            string.Join(";", Resources.Transactions_Dump_Date, Resources.Transactions_Dump_SecurityId, Resources.Transactions_Dump_Type, Resources.Transactions_Dump_Nominal, Resources.Transactions_Dump_PriceInEuro, Resources.Transactions_Dump_Loss, Resources.Transactions_Dump_Profit)
        };
        transactions = transactions.OrderBy(x => x.Date.ToString("yyyyMMdd") + x.Security.SecurityId + (x.TransactionType == TransactionType.Sell ? '0' : '1')).ToList();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (Transaction transaction in transactions) {
            contents.Add(transaction.Date.ToString("dd.MM.yyyy") + ';' + transaction.Security.SecurityId
                         + ';' + Enum.GetName(typeof(TransactionType), transaction.TransactionType)
                         + ';' + transaction.Nominal + ';' + transaction.PriceInEuro
                         + ';' + transaction.ExpensesInEuro + ';' + transaction.IncomeInEuro);
        }

        var textFileWriter = new TextFileWriter();
        textFileWriter.WriteAllLines(folder, TransactionsFileName, contents, Encoding.UTF8);

        var transactionDtos = transactions.Select(t => new TransactionDto(t)).ToList();
        string json = JsonSerializer.Serialize(transactionDtos, new JsonSerializerOptions { WriteIndented = true });
        textFileWriter.WriteAllLines(folder, TransactionsJsonFileName, [ json ], Encoding.UTF8);
    }

    public IList<Transaction> ReadTransactions(string folder, DbSet<Security> securities, IErrorsAndInfos errorsAndInfos) {
        string fileName = folder + '\\' + TransactionsFileName;
        if (!File.Exists(fileName)) {
            return new List<Transaction>();
        }

        var transactions = new List<Transaction>();
        int indexDate = -1, indexSecurityId = -1, indexType = -1, indexNominal = -1, indexPriceInEuro = -1, indexLoss = -1, indexProfit = -1;
        foreach (string line in File.ReadAllLines(fileName)) {
            if (indexDate < 0) {
                var headers = line.Split(';').ToList();
                indexDate = headers.IndexOf(Resources.Transactions_Dump_Date);
                indexSecurityId = headers.IndexOf(Resources.Transactions_Dump_SecurityId);
                indexType = headers.IndexOf(Resources.Transactions_Dump_Type);
                indexNominal = headers.IndexOf(Resources.Transactions_Dump_Nominal);
                indexPriceInEuro = headers.IndexOf(Resources.Transactions_Dump_PriceInEuro);
                indexLoss = headers.IndexOf(Resources.Transactions_Dump_Loss);
                indexProfit = headers.IndexOf(Resources.Transactions_Dump_Profit);
                if (indexDate < 0 || indexSecurityId < 0 || indexType < 0) {
                    errorsAndInfos.Errors.Add($"Transactions file cannot be read. Index Date is {indexDate}, ID is {indexSecurityId}, Type is {indexType}");
                    return new List<Transaction>();
                }
                if (indexNominal < 0 || indexPriceInEuro < 0 || indexLoss < 0 || indexProfit < 0) {
                    errorsAndInfos.Errors.Add($"Transactions file cannot be read. Index Nominal is {indexNominal}, Price is {indexPriceInEuro}, Loss is {indexLoss}, Profit is {indexProfit}");
                    return new List<Transaction>();
                }

                continue;
            }

            var data = line.Split(';').ToList();
            if (data.Count != 7) { continue; }

            if (!DateTime.TryParse(ConvertDate(data[indexDate]), out DateTime date)) {
                errorsAndInfos.Errors.Add($"Cannot parse {ConvertDate(data[indexDate])} as DateTime");
                continue;
            }

            string securityId = data[indexSecurityId];
            Security security = securities.FirstOrDefault(x => x.SecurityId == securityId);
            if (security == null) {
                errorsAndInfos.Errors.Add($"Cannot find security {securityId}");
                continue;
            }

            if (!Enum.TryParse(data[indexType], out TransactionType transactionType)) {
                errorsAndInfos.Errors.Add($"Cannot find transaction type {data[indexType]}");
                continue;
            }

            if (!double.TryParse(data[indexNominal], out double nominal)) {
                errorsAndInfos.Errors.Add($"Cannot parse {data[indexNominal]} as double");
                continue;
            }

            if (!double.TryParse(data[indexPriceInEuro], out double price)) {
                errorsAndInfos.Errors.Add($"Cannot parse {data[indexPriceInEuro]} as double");
                continue;
            }

            if (!double.TryParse(data[indexLoss], out double loss)) {
                errorsAndInfos.Errors.Add($"Cannot parse {data[indexLoss]} as double");
                continue;
            }

            if (!double.TryParse(data[indexProfit], out double profit)) {
                errorsAndInfos.Errors.Add($"Cannot parse {data[indexProfit]} as double");
                continue;
            }

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

    public IList<Transaction> ReadTransactionsJson(string folder, DbSet<Security> securities, IErrorsAndInfos errorsAndInfos) {
        string fileName = folder + '\\' + TransactionsJsonFileName;
        if (!File.Exists(fileName)) {
            return new List<Transaction>();
        }

        string json = string.Join(" ", File.ReadAllLines(fileName));
        List<TransactionDto> transactionDtos;

        try {
            transactionDtos = JsonSerializer.Deserialize<List<TransactionDto>>(json);
        } catch (Exception e) {
            errorsAndInfos.Errors.Add($"Could not deserialize import file ({e.Message})");
            return new List<Transaction>();
        }

        var transactions = new List<Transaction>();
        foreach (TransactionDto transactionDto in transactionDtos) {
            var transaction = new Transaction {
                Date = transactionDto.Date,
                Security = securities.FirstOrDefault(x => x.SecurityId == transactionDto.SecurityDto.SecurityId),
                TransactionType = transactionDto.TransactionType,
                Nominal = transactionDto.Nominal,
                PriceInEuro = transactionDto.PriceInEuro,
                ExpensesInEuro = transactionDto.ExpensesInEuro,
                IncomeInEuro = transactionDto.IncomeInEuro
            };
            if (transaction.Security == null) {
                errorsAndInfos.Errors.Add($"Cannot find security {transactionDto.SecurityDto.SecurityId}");
                continue;
            }

            transactions.Add(transaction);

        }

        return transactions;
    }
} 