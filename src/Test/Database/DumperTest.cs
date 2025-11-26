using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.DbOperations;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Database;

[TestClass]
public class DumperTest {
    private readonly IContainer _Container = new ContainerBuilder().UsePegh("Fundamental", new DummyCsArgumentPrompter()).Build();
    private readonly IContextFactory _ContextFactory = new ContextFactory();

    [TestMethod]
    public async Task CanDumpSecurities() {
        using var executionContext = new DumperTestExecutionContext(_Container.Resolve<IFolderResolver>());
        await executionContext.SetHelperAsync(_ContextFactory);
        Assert.IsFalse(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.SecuritiesFileName));
        await using var context = await _ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        executionContext.Dumper.DumpSecurities(executionContext.DumpFolder, context.Securities.ToList());
        Assert.IsTrue(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.SecuritiesFileName));
        Assert.AreEqual(await File.ReadAllTextAsync(executionContext.MasterFolder.FullName + '\\' + Dumper.SecuritiesFileName), await File.ReadAllTextAsync(executionContext.DumpFolder.FullName + '\\' + Dumper.SecuritiesFileName));
    }

    [TestMethod]
    public async Task CanDumpQuotes() {
        using var executionContext = new DumperTestExecutionContext(_Container.Resolve<IFolderResolver>());
        await executionContext.SetHelperAsync(_ContextFactory);
        Assert.IsFalse(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.QuotesFileName));
        await using var context = await _ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        executionContext.Dumper.DumpQuotes(executionContext.DumpFolder, context.Quotes.Include(x => x.Security).ToList());
        Assert.IsTrue(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.QuotesFileName));
        Assert.AreEqual(await File.ReadAllTextAsync(executionContext.MasterFolder.FullName + '\\' + Dumper.QuotesFileName), await File.ReadAllTextAsync(executionContext.DumpFolder.FullName + '\\' + Dumper.QuotesFileName));
    }

    [TestMethod]
    public async Task CanDumpTransactions() {
        using var executionContext = new DumperTestExecutionContext(_Container.Resolve<IFolderResolver>());
        await executionContext.SetHelperAsync(_ContextFactory);
        Assert.IsFalse(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.TransactionsFileName));
        await using var context = await _ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        executionContext.Dumper.DumpTransactions(executionContext.DumpFolder, context.Transactions.Include(x => x.Security).ToList());
        Assert.IsTrue(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.TransactionsFileName));
        Assert.AreEqual(await File.ReadAllTextAsync(executionContext.MasterFolder.FullName + '\\' + Dumper.TransactionsFileName), await File.ReadAllTextAsync(executionContext.DumpFolder.FullName + '\\' + Dumper.TransactionsFileName));
    }

    [TestMethod]
    public async Task CanDumpTransactionsAsJson() {
        using var executionContext = new DumperTestExecutionContext(_Container.Resolve<IFolderResolver>());
        await executionContext.SetHelperAsync(_ContextFactory);
        Assert.IsFalse(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.TransactionsFileName));
        await using var context = await _ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        executionContext.Dumper.DumpTransactions(executionContext.DumpFolder, context.Transactions.Include(x => x.Security).ToList());
        Assert.IsTrue(File.Exists(executionContext.DumpFolder.FullName + '\\' + Dumper.TransactionsJsonFileName));
        var expectedContents = await File.ReadAllTextAsync(executionContext.MasterFolder.FullName + '\\' + Dumper.TransactionsJsonFileName);
        var actualContents = await File.ReadAllTextAsync(executionContext.DumpFolder.FullName + '\\' + Dumper.TransactionsJsonFileName);
        Assert.AreEqual(expectedContents, actualContents);
    }
}

internal class DumperTestExecutionContext : IDisposable {
    internal IFolder MasterFolder, DumpFolder;
    internal TestHelper Helper;
    internal Dumper Dumper;

    internal DumperTestExecutionContext(IFolderResolver folderResolver) {
        Dumper = new Dumper();
        var errorsAndInfos = new ErrorsAndInfos();
        MasterFolder = folderResolver.ResolveAsync(@"$(GitHub)\Fundamental\src\Test\Application", errorsAndInfos).Result;
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        DumpFolder = folderResolver.ResolveAsync(@"$(GitHub)\Fundamental\src\Test\Application\Temp\", errorsAndInfos).Result;
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        if (DumpFolder.Exists()) {
            new FolderDeleter().DeleteFolder(DumpFolder);
        }
        Directory.CreateDirectory(DumpFolder.FullName);
    }

    public void Dispose() {
        new FolderDeleter().DeleteFolder(DumpFolder);
    }

    public async Task SetHelperAsync(IContextFactory contextFactory) {
        Helper = new TestHelper(contextFactory);
        await Helper.PopulateDatabaseWithTestRepositoryDataAsync();
    }
}