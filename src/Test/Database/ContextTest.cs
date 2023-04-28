using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Database;

[TestClass]
public class ContextTest {
    protected TestHelper Helper;
    protected IContextFactory ContextFactory;

    [TestInitialize]
    public void Initialize() {
        ContextFactory = new ContextFactory();
        Helper = new TestHelper(ContextFactory);
    }

    [TestMethod]
    public async Task CanWorkWithContext() {
        await using var db = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        db.Migrate();
    }

    [TestMethod]
    public async Task CanTruncateDatabase() {
        await Helper.TruncateAllTablesAsync(EnvironmentType.UnitTest);
        await using var db = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        Assert.IsFalse(db.Holdings.Any());
        Assert.IsFalse(db.Transactions.Any());
        Assert.IsFalse(db.Quotes.Any());
        Assert.IsFalse(db.Securities.Any());
        Assert.IsFalse(db.DateSummaries.Any());
    }

    [TestMethod]
    public async Task CanPopulateDatabaseWithTestRepositoryData() {
        await Helper.PopulateDatabaseWithTestRepositoryDataAsync();
        await using var db = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        Assert.IsFalse(db.DateSummaries.Any());
        Assert.IsFalse(db.Holdings.Any());
        Assert.IsTrue(db.Transactions.Any());
        Assert.IsTrue(db.Quotes.Any());
        Assert.IsTrue(db.Securities.Any());
    }
}