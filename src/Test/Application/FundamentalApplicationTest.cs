using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Application;

public class WhenWorkingWithFundamentalApplication {
    protected ApplicationCommandController Controller;
    protected FundamentalApplication Application;
    protected TestHelper Helper;
    protected IContainer Container;
    protected IContextFactory ContextFactory;

    public WhenWorkingWithFundamentalApplication(IContextFactory contextFactory) {
        ContextFactory = contextFactory;
        Container = new ContainerBuilder().UsePegh("Fundamental", new DummyCsArgumentPrompter()).Build();
        Controller = new ApplicationCommandController(Container.Resolve<ISimpleLogger>(), HandleFeedbackToApplicationAsync);
    }

    public async Task HandleFeedbackToApplicationAsync(IFeedbackToApplication feedbackToApplication) {
        await Task.CompletedTask;
    }

    protected void FocusOnSecurity(string securityId) {
        var security = Application.Securities().FirstOrDefault(x => x.SecurityId == securityId);
        Assert.IsNotNull(security);
        Application.FocusOnSecurity(security);
    }
}

[TestClass]
public class FundamentalApplicationTest : WhenWorkingWithFundamentalApplication {
    public FundamentalApplicationTest() : base(new ContextFactory()) {
    }

    [TestInitialize]
    public async Task Initialize() {
        Helper = new TestHelper(ContextFactory);
        await Helper.PopulateDatabaseWithTestRepositoryDataAsync();
        Application = new FundamentalApplication(EnvironmentType.UnitTest, Controller,
            Controller, SynchronizationContext.Current,
            Container.Resolve<IFolderResolver>(), ContextFactory,
            Container.Resolve<ISecretRepository>());
        await Application.OnLoadedAsync();
    }

    [TestMethod]
    public async Task SaveCommandIsDisabledInitially() {
        await Controller.ExecuteAsync(typeof(RefreshContextCommand));
        Assert.IsFalse(await Controller.EnabledAsync(typeof(SaveCommand)));
    }

    [TestMethod]
    public async Task CanFocusOnSecurity() {
        await Controller.ExecuteAsync(typeof(RefreshContextCommand));
        FocusOnSecurity(TestDataRepository.ShareId);
        Assert.AreEqual(Application.SecurityInFocus.SecurityId, TestDataRepository.ShareId);
    }

    [TestMethod]
    public async Task InitialDataDoNotContainHoldings() {
        await Controller.ExecuteAsync(typeof(RefreshContextCommand));
        Assert.IsFalse(Application.Holdings().Any());
    }

    [TestMethod]
    public async Task HoldingsAreUpdatedWhenModifiedTransactionIsSaved() {
        await Controller.ExecuteAsync(typeof(RefreshContextCommand));
        FocusOnSecurity(TestDataRepository.ShareId);
        var transaction = Application.Transactions().FirstOrDefault(x => x.Security == Application.SecurityInFocus && x.Date == TestDataRepository.CouponAndDividendDate && x.IncomeInEuro > 0);
        Assert.IsNotNull(transaction);
        transaction.Nominal = transaction.Nominal;
        Assert.IsTrue(await Controller.EnabledAsync(typeof(SaveCommand)));
        await Controller.ExecuteAsync(typeof(SaveCommand));
        await using var context = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        var holding = context.Holdings.Include(h => h.Security).FirstOrDefault(x => x.Security == Application.SecurityInFocus && x.Date == TestDataRepository.LastQuoteDate);
        Assert.IsNotNull(holding);
        Assert.IsTrue(Helper.IsHoldingEqualTo(holding, TestDataRepository.LastQuoteDate, TestDataRepository.ShareId, 70, 1502.9, 1670.2, 67.4, 504.32, 0, 167.30));
    }
}