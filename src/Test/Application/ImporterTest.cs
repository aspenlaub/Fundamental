using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Application;

[TestClass]
public class ImporterTest {
    private const string _bankStatementInfix = "007_";

    protected TestHelper Helper;
    protected Importer Importer;
    protected IContainer Container;
    protected readonly IContextFactory ContextFactory = new ContextFactory();

    [TestInitialize]
    public async Task Initialize() {
        Helper = new TestHelper(ContextFactory);
        await Helper.PopulateDatabaseWithTestRepositoryDataAsync();
        var executionContext = new FakeCommandExecutionContext();
        Container =new ContainerBuilder().UsePegh("Fundamental").Build();
        Importer = new Importer(EnvironmentType.UnitTest, executionContext, Container.Resolve<IFolderResolver>(), ContextFactory);
    }

    [TestMethod]
    public async Task CanImportQuotes() {
        var date = new DateTime(2016, 3, 23);
        await VerifyNoQuotesOnAsync(date);
        await VerifyNoHoldingsAsync();
        await ImportQuotesAsync();
        await using var context = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        var quotes = context.Quotes.Where(x => x.Date == date).ToList();
        Assert.HasCount(1, quotes);
        Assert.AreEqual(93, quotes[0].PriceInEuro);
        var holdings = context.Holdings.Where(x => x.Date == date).ToList();
        Assert.HasCount(1, holdings);
        Assert.AreEqual(6510, holdings[0].QuoteValueInEuro);
    }

    [TestMethod]
    public async Task QuotesWithoutHoldingsAreRebookedWhenReimporting() {
        var date = new DateTime(2016, 3, 23);
        await ImportQuotesAsync();
        await using (var context = await ContextFactory.CreateAsync(EnvironmentType.UnitTest)) {
            context.Holdings.RemoveRange(context.Holdings);
            context.SaveChanges();
        }
        await VerifyNoHoldingsAsync();
        await ImportQuotesAsync();
        await using (var context = await ContextFactory.CreateAsync(EnvironmentType.UnitTest)) {
            var holdings = context.Holdings.Where(x => x.Date == date).ToList();
            Assert.HasCount(1, holdings);
            Assert.AreEqual(6510, holdings[0].QuoteValueInEuro);
        }
    }

    private async Task VerifyNoQuotesOnAsync(DateTime date) {
        await using var context = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        Assert.IsFalse(context.Quotes.Any(x => x.Date == date));
    }

    private async Task VerifyNoHoldingsAsync() {
        await using var context = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        Assert.IsFalse(context.Holdings.Any());
    }

    private async Task ImportQuotesAsync() {
        var errorsAndInfos = new ErrorsAndInfos();
        var folder = (await Container.Resolve<IFolderResolver>().ResolveAsync(@"$(GitHub)\Fundamental\src\Test\Application", errorsAndInfos)).FullName + "\\";
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        var directoryInfo = new DirectoryInfo(folder);
        var files = directoryInfo.GetFiles('*' + _bankStatementInfix + "*.csv").ToList();
        Assert.HasCount(1, files);
        var file = files[0];
        var inFolder = (await Container.Resolve<IFolderResolver>().ResolveAsync(@"$(MainUserFolder)\Fundamental\UnitTest\In", errorsAndInfos)).FullName + "\\";
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        File.Copy(file.FullName, inFolder + file.Name, true);
        await Importer.ImportBankStatementAsync(file.Name);
    }
}