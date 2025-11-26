using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.DbOperations;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
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

public class Importer(EnvironmentType environmentType, IApplicationCommandExecutionContext executionContext,
        IFolderResolver folderResolver, IContextFactory contextFactory) : IImporter {
    protected IFolder InFolder, DoneFolder;
    protected EnvironmentType EnvironmentType = environmentType;
    protected IApplicationCommandExecutionContext ExecutionContext = executionContext;
    protected IContextFactory ContextFactory = contextFactory;
    protected IFolderResolver FolderResolver = folderResolver;

    public async Task ImportAFileAsync(string bankStatementInfix) {
        await SetFoldersIfNecessaryAsync();

        var dirInfo = new DirectoryInfo(InFolder.FullName);
        FileInfo file = dirInfo.GetFiles('*' + bankStatementInfix + "*.csv", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (file != null) {
            await ImportBankStatementAsync(file.Name);
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

        if (!(File.Exists(InFolder.FullName + '\\' + Dumper.TransactionsFileName) || File.Exists(InFolder.FullName + '\\' + Dumper.TransactionsJsonFileName))) {
            return;
        }

        await ImportTransactionsDumpAsync();
    }

    public async Task ImportBankStatementAsync(string shortFileName) {
        const char separator = ';';

        await SetFoldersIfNecessaryAsync();

        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, shortFileName) });
        string fileName = InFolder.FullName + '\\' + shortFileName;
        DateTime? date = await ReadDateInFileNameAsync(shortFileName);
        if (date == null) { return; }

        var fileContents = (await File.ReadAllLinesAsync(fileName, Encoding.UTF8)).ToList();
        if (fileContents.Count < 2) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = Resources.FileTooSmall });
            return;
        }

        var row = fileContents[0].Split(separator).ToList();
        int indexCurrency = KeyWordIndexSync(Resources.BankStatement_Currency, row);
        if (indexCurrency < 0) {
            fileContents = (await File.ReadAllLinesAsync(fileName, Encoding.GetEncoding("ISO-8859-1"))).ToList();
            row = fileContents[0].Split(separator).ToList();
        }

        int indexNominal = await KeyWordIndexAsync(Resources.BankStatement_Nominal, row);
        int indexId = await KeyWordIndexAsync(Resources.BankStatement_SecurityNumber, row);
        int indexName = await KeyWordIndexAsync(Resources.BankStatement_SecurityName, row);
        indexCurrency = await KeyWordIndexAsync(Resources.BankStatement_Currency, row);
        int indexQuote = await KeyWordIndexAsync(Resources.BankStatement_PriceInCurrency, row);
        int indexQuoteValue = await KeyWordIndexAsync(Resources.BankStatement_CurrentValueInEuro, row);
        if (indexNominal < 0 || indexId < 0 || indexName < 0 || indexCurrency < 0 || indexQuote < 0 || indexQuoteValue < 0) { return; }

        int maxIndex = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(indexNominal, indexId), indexCurrency), indexQuote), indexName), indexQuoteValue);
        fileContents.RemoveAt(0);
        uint successes = 0, failures = 0;
        var quotes = new List<Quote>();
        foreach (string s in fileContents) {
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

            if (!ReadNominalAndPrice(row, indexNominal, indexQuote, indexQuoteValue, out double nominal, out double price, out double quoteValue)) {
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

        await using Context importContext = await ContextFactory.CreateAsync(EnvironmentType);
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, Dumper.QuotesFileName) });
        var dumper = new Dumper();
        var errorsAndInfos = new ErrorsAndInfos();
        IList<Quote> quotes = dumper.ReadQuotes(InFolder.FullName + '\\', importContext.Securities, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            foreach (string error in errorsAndInfos.Errors) {
                await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = error });
            }

            return;
        }
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
        await using (Context importContext = await ContextFactory.CreateAsync(EnvironmentType)) {
            foreach (Quote quote in quotes) {
                string securityId = quote.Security.SecurityId;
                Security security = importContext.Securities.FirstOrDefault(x => x.SecurityId == securityId);
                if (security == null) {
                    importContext.Securities.Add(quote.Security);
                } else {
                    quote.Security = security;
                }
                Quote existingQuote = importContext.Quotes.FirstOrDefault(x => x.Security.SecurityId == securityId && x.Date == quote.Date);
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
            var quoteDatesWithoutHoldings = importContext.Quotes.Select(x => x.Date).Distinct().ToList();
            quoteDatesWithoutHoldings = quoteDatesWithoutHoldings.Where(x => !holdingDates.Contains(x)).ToList();
            foreach (DateTime quoteDateWithoutHoldings in quoteDatesWithoutHoldings) {
                foreach (Security security in importContext.Quotes.Where(x => quoteDateWithoutHoldings == x.Date).Select(x => x.Security)) {
                    securityIdsWithDataToSave.Add(security.SecurityId);
                }
            }
        }

        await new Rebooker(EnvironmentType, ContextFactory).RebookAsync(securityIdsWithDataToSave);
        await SaveToDatabaseEpilogueAsync(shortFileName, failures, quotes.Count, addedQuotes, updatedQuotes);
    }

    private async Task SaveToDatabaseEpilogueAsync(string shortFileName, uint failures, int importedObjects, uint addedObjects, uint updatedObjects) {
        if (failures != 0) { return; }

        string fileName = InFolder.FullName + '\\' + shortFileName;
        if (File.Exists(DoneFolder.FullName + '\\' + shortFileName)) {
            File.Delete(DoneFolder.FullName + '\\' + shortFileName);
        }
        if (File.Exists(fileName)) {
            File.Move(fileName, DoneFolder.FullName + '\\' + shortFileName);
        }
        string message = string.Format(Resources.FileImportedAndMoved, shortFileName, importedObjects, addedObjects, updatedObjects);
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = message });

    }

    private static bool ReadNominalAndPrice(IReadOnlyList<string> row, int indexNominal, int indexQuote, int indexQuoteValue, out double nominal, out double price, out double quoteValue) {
        price = -1;
        quoteValue = -1;
        return double.TryParse(row[indexNominal], out nominal) && double.TryParse(row[indexQuote], out price) && double.TryParse(row[indexQuoteValue], out quoteValue);
    }

    private async Task<DateTime?> ReadDateInFileNameAsync(string shortFileName) {
        var indices = shortFileName
                      .Select((c, i) => new { c, i })
                      .Where(x => x.c == '_')
                      .Select(x => x.i + 1)
                      .ToList();
        foreach(int index in indices) {
            if (index + 8 >= shortFileName.Length) {
                continue;
            }

            if (int.TryParse(shortFileName.Substring(index, 4), out int year)
                && int.TryParse(shortFileName.Substring(index + 4, 2), out int month)
                && int.TryParse(shortFileName.Substring(index + 6, 2), out int day)) {
                return new DateTime(year, month, day);
            }
        }

        await ExecutionContext.ReportAsync(new FeedbackToApplication {
            Type = FeedbackType.LogError,
            Message = string.Format(Resources.DateCouldNotBeExtractedFromFileName)
        });
        return null;
    }

    private int KeyWordIndexSync(string keyWord, IList<string> row) {
        return row.IndexOf(keyWord);
    }

    private async Task<int> KeyWordIndexAsync(string keyWord, IList<string> row) {
        int index = KeyWordIndexSync(keyWord, row);
        if (index >= 0) { return index; }

        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = string.Format(Resources.KeyWordNotFound, keyWord) });
        return index;
    }

    public async Task ImportSecuritiesDumpAsync() {
        await SetFoldersIfNecessaryAsync();

        await using (await ContextFactory.CreateAsync(EnvironmentType)) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, Dumper.SecuritiesFileName) });
            var dumper = new Dumper();
            var errorsAndInfos = new ErrorsAndInfos();
            IList<Security> securities = dumper.ReadSecurities(InFolder.FullName, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                foreach (string error in errorsAndInfos.Errors) {
                    await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = error });
                }

                return;
            }
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
        await using (Context importContext = await ContextFactory.CreateAsync(EnvironmentType)) {
            foreach (Security security in securities) {
                string securityId = security.SecurityId;
                Security existingSecurity = importContext.Securities.FirstOrDefault(x => x.SecurityId == securityId);
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

        await using Context importContext = await ContextFactory.CreateAsync(EnvironmentType);
        await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = string.Format(Resources.ImportingFile, Dumper.TransactionsFileName) });
        var dumper = new Dumper();
        var errorsAndInfos = new ErrorsAndInfos();
        IList<Transaction> transactions = dumper.ReadTransactions(InFolder.FullName, importContext.Securities, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            foreach (string error in errorsAndInfos.Errors) {
                await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = error });
            }

            return;
        }

        if (!transactions.Any()) {
            errorsAndInfos = new ErrorsAndInfos();
            transactions = dumper.ReadTransactionsJson(InFolder.FullName, importContext.Securities, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                foreach (string error in errorsAndInfos.Errors) {
                    await ExecutionContext.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogError, Message = error });
                }

                return;
            }
        }

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
        await using (Context importContext = await ContextFactory.CreateAsync(EnvironmentType)) {
            foreach (Transaction transaction in transactions) {
                string securityId = transaction.Security.SecurityId;
                Security security = importContext.Securities.FirstOrDefault(x => x.SecurityId == securityId);
                if (security == null) {
                    importContext.Securities.Add(transaction.Security);
                } else {
                    transaction.Security = security;
                }
                Transaction existingTransaction = importContext.Transactions.FirstOrDefault(x => x.Security.SecurityId == securityId && x.Date == transaction.Date && x.TransactionType == transaction.TransactionType);
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

                if (security == null) {
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
        IFolder folder = (await FolderResolver.ResolveAsync("$(MainUserFolder)", errorsAndInfos)).SubFolder("Fundamental").SubFolder(Enum.GetName(typeof(EnvironmentType), EnvironmentType));
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