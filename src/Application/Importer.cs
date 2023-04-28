using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.DbOperations;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Properties;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class Importer : IImporter {
    protected IFolder InFolder, DoneFolder;
    protected EnvironmentType EnvironmentType;
    protected IApplicationCommandExecutionContext ExecutionContext;
    protected IContextFactory ContextFactory;
    protected IFolderResolver FolderResolver;

    public Importer(EnvironmentType environmentType, IApplicationCommandExecutionContext executionContext, IFolderResolver folderResolver, IContextFactory contextFactory) {
        EnvironmentType = environmentType;
        ContextFactory = contextFactory;
        ExecutionContext = executionContext;
        FolderResolver = folderResolver;
    }

    public async Task ImportAFileAsync(string bankStatementInfix) {
        await SetFoldersIfNecessaryAsync();

        var dirInfo = new DirectoryInfo(InFolder.FullName);
        var file = dirInfo.GetFiles('*' + bankStatementInfix + "*.csv", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (file != null) {
            await ImportBankStatementAsync(file.Name, bankStatementInfix);
            return;
        }

        if (File.Exists(InFolder.FullName + '\\' + Dumper.SecuritiesFileName)) {
            await ImportSecuritiesDumpAsync();
            return;
        }

        if (File.Exists(InFolder.FullName + '\\' + Dumper.QuotesFileName)) {
            await ImportQuotesDumpAsync();
            return;
        }

        if (!File.Exists(InFolder.FullName + '\\' + Dumper.TransactionsFileName)) {
            return;
        }

        await ImportTransactionsDumpAsync();
    }

    public async Task ImportBankStatementAsync(string shortFileName, string bankStatementInfix) {
        const char separator = ';';

        await SetFoldersIfNecessaryAsync();

        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, shortFileName) });
        var fileName = InFolder.FullName + '\\' + shortFileName;
        var date = await ReadDateInFileNameAsync(shortFileName, bankStatementInfix);
        if (date == null) { return; }

        var fileContents = (await File.ReadAllLinesAsync(fileName, Encoding.UTF8)).ToList();
        if (fileContents.Count < 2) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Resources.FileTooSmall });
            return;
        }

        var row = fileContents[0].Split(separator).ToList();
        var indexCurrency = KeyWordIndexSync(Resources.BankStatement_Currency, row);
        if (indexCurrency < 0) {
            fileContents = (await File.ReadAllLinesAsync(fileName, Encoding.GetEncoding("ISO-8859-1"))).ToList();
            row = fileContents[0].Split(separator).ToList();
        }

        var indexNominal = await KeyWordIndexAsync(Resources.BankStatement_Nominal, row);
        var indexId = await KeyWordIndexAsync(Resources.BankStatement_SecurityNumber, row);
        var indexName = await KeyWordIndexAsync(Resources.BankStatement_SecurityName, row);
        indexCurrency = await KeyWordIndexAsync(Resources.BankStatement_Currency, row);
        var indexQuote = await KeyWordIndexAsync(Resources.BankStatement_PriceInCurrency, row);
        var indexQuoteValue = await KeyWordIndexAsync(Resources.BankStatement_CurrentValueInEuro, row);
        if (indexNominal < 0 || indexId < 0 || indexName < 0 || indexCurrency < 0 || indexQuote < 0 || indexQuoteValue < 0) { return; }

        var maxIndex = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(indexNominal, indexId), indexCurrency), indexQuote), indexName), indexQuoteValue);
        fileContents.RemoveAt(0);
        uint successes = 0, failures = 0;
        var quotes = new List<Quote>();
        foreach (var s in fileContents) {
            row = s.Split(separator).ToList();
            if (row.Count >= indexId && row[indexId].Length == 0 && row.Count >= indexName && row[indexName].Contains(Resources.BankStatement_BankAccount)) {
                continue;
            }

            if (row.Count < maxIndex || row[indexNominal].Length == 0 || row[indexId].Length == 0 || row[indexCurrency].Length == 0 || row[indexQuote].Length == 0 || row[indexQuoteValue].Length == 0) {
                await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.NotEnoughData, s) });
                failures++;
                continue;
            }

            if (row[indexCurrency] != "EUR") {
                await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.SecurityIsQuotedIn, row[indexId], row[indexCurrency]) });
                failures++;
                continue;
            }

            if (!ReadNominalAndPrice(row, indexNominal, indexQuote, indexQuoteValue, out var nominal, out var price, out var quoteValue)) {
                await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.CouldNotReadNominalOrPriceFor, row[indexId]) });
                failures++;
                continue;
            }

            successes++;
            var security = new Security { SecurityId = row[indexId], SecurityName = row[indexName], QuotedPer = Math.Round(nominal * price / quoteValue, 0) };
            quotes.Add(new Quote { Security = security, Date = (DateTime)date, PriceInEuro = price });
        }

        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportResult, successes, failures) });
        await SaveImportedQuotesAsync(shortFileName, failures, quotes);
    }

    public async Task ImportQuotesDumpAsync() {
        await SetFoldersIfNecessaryAsync();

        await using var importContext = await ContextFactory.CreateAsync(EnvironmentType);
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, Dumper.QuotesFileName) });
        var dumper = new Dumper();
        var quotes = dumper.ReadQuotes(InFolder.FullName + '\\', importContext.Securities);
        if (quotes.Any()) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportResult, quotes.Count, 0) });
            await SaveImportedQuotesAsync(Dumper.QuotesFileName, 0, quotes);
        } else {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.CouldNotImportFile, Dumper.QuotesFileName) });
        }
    }

    protected async Task SaveImportedQuotesAsync(string shortFileName, uint failures, IList<Quote> quotes) {
        var securityIdsWithDataToSave = new HashSet<string>();
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.WriteImportedQuotes, shortFileName) });
        uint addedQuotes = 0, updatedQuotes = 0;
        await using (var importContext = await ContextFactory.CreateAsync(EnvironmentType)) {
            foreach (var quote in quotes) {
                var securityId = quote.Security.SecurityId;
                var security = importContext.Securities.FirstOrDefault(x => x.SecurityId == securityId);
                if (security == null) {
                    importContext.Securities.Add(quote.Security);
                } else {
                    quote.Security = security;
                }
                var existingQuote = importContext.Quotes.FirstOrDefault(x => x.Security.SecurityId == securityId && x.Date == quote.Date);
                if (existingQuote == null) {
                    importContext.Quotes.Add(quote);
                    addedQuotes++;
                } else if (existingQuote.PriceInEuro == quote.PriceInEuro) {
                    continue;
                } else {
                    existingQuote.PriceInEuro = quote.PriceInEuro;
                    updatedQuotes++;
                }

                if (security != null && securityIdsWithDataToSave.Contains(security.SecurityId)) {
                    continue;
                }

                if (security != null) {
                    securityIdsWithDataToSave.Add(security.SecurityId);
                }
            }

            importContext.SaveChanges();
            var holdingDates = importContext.Holdings.Select(x => x.Date).Distinct().ToList();
            foreach(var quoteDateWithoutHoldings in importContext.Quotes.Select(x => x.Date).Distinct().Where(x => !holdingDates.Contains(x))) {
                foreach (var security in importContext.Quotes.Where(x => quoteDateWithoutHoldings == x.Date).Select(x => x.Security)) {
                    if (securityIdsWithDataToSave.Contains(security.SecurityId)) { continue; }

                    securityIdsWithDataToSave.Add(security.SecurityId);
                }
            }
        }

        await new Rebooker(EnvironmentType, ContextFactory).RebookAsync(securityIdsWithDataToSave);
        await SaveToDatabaseEpilogueAsync(shortFileName, failures, quotes.Count, addedQuotes, updatedQuotes);
    }

    private async Task SaveToDatabaseEpilogueAsync(string shortFileName, uint failures, int importedObjects, uint addedObjects, uint updatedObjects) {
        if (failures != 0) { return; }

        var fileName = InFolder.FullName + '\\' + shortFileName;
        if (File.Exists(DoneFolder.FullName + '\\' + shortFileName)) {
            File.Delete(DoneFolder.FullName + '\\' + shortFileName);
        }
        if (File.Exists(fileName)) {
            File.Move(fileName, DoneFolder.FullName + '\\' + shortFileName);
        }
        var message = string.Format(Resources.FileImportedAndMoved, shortFileName, importedObjects, addedObjects, updatedObjects);
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = message });

    }

    private static bool ReadNominalAndPrice(IReadOnlyList<string> row, int indexNominal, int indexQuote, int indexQuoteValue, out double nominal, out double price, out double quoteValue) {
        price = -1;
        quoteValue = -1;
        return double.TryParse(row[indexNominal], out nominal) && double.TryParse(row[indexQuote], out price) && double.TryParse(row[indexQuoteValue], out quoteValue);
    }

    private async Task<DateTime?> ReadDateInFileNameAsync(string shortFileName, string bankStatementInfix) {
        var tag = bankStatementInfix;
        var index = shortFileName.IndexOf(tag, StringComparison.Ordinal);
        if (index < 0) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.DateTagNotFound, tag) });
            return null;
        }

        if (int.TryParse(shortFileName.Substring(index + tag.Length, 4), out var year)
            && int.TryParse(shortFileName.Substring(index + tag.Length + 4, 2), out var month)
            && int.TryParse(shortFileName.Substring(index + tag.Length + 6, 2), out var day)) {
            return new DateTime(year, month, day);
        }

        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.InvalidDate, shortFileName.Substring(index + tag.Length, 8)) });
        return null;
    }

    private int KeyWordIndexSync(string keyWord, IList<string> row) {
        return row.IndexOf(keyWord);
    }

    private async Task<int> KeyWordIndexAsync(string keyWord, IList<string> row) {
        var index = KeyWordIndexSync(keyWord, row);
        if (index >= 0) { return index; }

        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.KeyWordNotFound, keyWord) });
        return index;
    }

    public async Task ImportSecuritiesDumpAsync() {
        await SetFoldersIfNecessaryAsync();

        await using (await ContextFactory.CreateAsync(EnvironmentType)) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, Dumper.SecuritiesFileName) });
            var dumper = new Dumper();
            var securities = dumper.ReadSecurities(InFolder.FullName);
            if (securities.Any()) {
                await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportResult, securities.Count, 0) });
                await SaveImportedSecuritiesAsync(Dumper.SecuritiesFileName, securities);
            } else {
                await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.CouldNotImportFile, Dumper.SecuritiesFileName) });
            }
        }
    }

    protected async Task SaveImportedSecuritiesAsync(string shortFileName, IList<Security> securities) {
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.WriteImportedSecurities, shortFileName) });
        uint addedSecurities = 0, updatedSecurities = 0;
        await using (var importContext = await ContextFactory.CreateAsync(EnvironmentType)) {
            foreach (var security in securities) {
                var securityId = security.SecurityId;
                var existingSecurity = importContext.Securities.FirstOrDefault(x => x.SecurityId == securityId);
                if (existingSecurity == null) {
                    importContext.Securities.Add(security);
                    addedSecurities++;
                } else if (existingSecurity.SecurityName != security.SecurityName || existingSecurity.QuotedPer != security.QuotedPer) {
                    existingSecurity.SecurityName = security.SecurityName;
                    existingSecurity.QuotedPer = security.QuotedPer;
                    updatedSecurities++;
                }
            }

            importContext.SaveChanges();
        }

        await SaveToDatabaseEpilogueAsync(Dumper.SecuritiesFileName, 0, securities.Count, addedSecurities, updatedSecurities);
    }

    public async Task ImportTransactionsDumpAsync() {
        await SetFoldersIfNecessaryAsync();

        await using var importContext = await ContextFactory.CreateAsync(EnvironmentType);
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, Dumper.TransactionsFileName) });
        var dumper = new Dumper();
        var transactions = dumper.ReadTransactions(InFolder.FullName, importContext.Securities);
        if (transactions.Any()) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportResult, transactions.Count, 0) });
            await SaveImportedTransactionsAsync(Dumper.TransactionsFileName, transactions);
        } else {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.CouldNotImportFile, Dumper.TransactionsFileName) });
        }
    }

    protected async Task SaveImportedTransactionsAsync(string shortFileName, IList<Transaction> transactions) {
        var securityIdsWithDataToSave = new HashSet<string>();
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.WriteImportedTransactions, shortFileName) });
        uint addedTransactions = 0, updatedTransactions = 0;
        await using (var importContext = await ContextFactory.CreateAsync(EnvironmentType)) {
            foreach (var transaction in transactions) {
                var securityId = transaction.Security.SecurityId;
                var security = importContext.Securities.FirstOrDefault(x => x.SecurityId == securityId);
                if (security == null) {
                    importContext.Securities.Add(transaction.Security);
                } else {
                    transaction.Security = security;
                }
                var existingTransaction = importContext.Transactions.FirstOrDefault(x => x.Security.SecurityId == securityId && x.Date == transaction.Date && x.TransactionType == transaction.TransactionType);
                if (existingTransaction == null) {
                    importContext.Transactions.Add(transaction);
                    addedTransactions++;
                } else if (existingTransaction.Nominal == transaction.Nominal
                           && existingTransaction.PriceInEuro == transaction.PriceInEuro
                           && existingTransaction.ExpensesInEuro == transaction.ExpensesInEuro
                           && existingTransaction.IncomeInEuro == transaction.IncomeInEuro) {
                    continue;
                } else {
                    existingTransaction.Nominal = transaction.Nominal;
                    existingTransaction.PriceInEuro = transaction.PriceInEuro;
                    existingTransaction.ExpensesInEuro = transaction.ExpensesInEuro;
                    existingTransaction.IncomeInEuro = transaction.IncomeInEuro;
                    updatedTransactions++;
                }

                if (security == null || securityIdsWithDataToSave.Contains(security.SecurityId)) {
                    continue;
                }

                securityIdsWithDataToSave.Add(security.SecurityId);
            }

            importContext.SaveChanges();
        }

        await new Rebooker(EnvironmentType, ContextFactory).RebookAsync(securityIdsWithDataToSave);
        await SaveToDatabaseEpilogueAsync(shortFileName, 0, transactions.Count, addedTransactions, updatedTransactions);
    }

    private async Task SetFoldersIfNecessaryAsync() {
        if (InFolder != null) { return; }

        var errorsAndInfos = new ErrorsAndInfos();
        var folder = (await FolderResolver.ResolveAsync("$(MainUserFolder)", errorsAndInfos)).SubFolder("Fundamental").SubFolder(Enum.GetName(typeof(EnvironmentType), EnvironmentType));
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }
        folder.CreateIfNecessary();
        InFolder = folder.SubFolder("In");
        InFolder.CreateIfNecessary();
        DoneFolder = folder.SubFolder("Done");
        DoneFolder.CreateIfNecessary();
    }
}