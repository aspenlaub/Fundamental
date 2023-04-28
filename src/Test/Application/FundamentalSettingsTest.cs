using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Application;

[TestClass]
public class FundamentalSettingsTest {
    [TestMethod]
    public async Task CanGetFundamentalSettings() {
        var container = new ContainerBuilder().UsePegh(nameof(FundamentalSettingsTest), new DummyCsArgumentPrompter()).Build();
        var secret = new SecretFundamentalSettings();
        var errorsAndInfos = new ErrorsAndInfos();
        var settings = await container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(!string.IsNullOrEmpty(settings.BankStatementInfix));
    }
}