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
        IContainer container = new ContainerBuilder().UsePegh(nameof(FundamentalSettingsTest)).Build();
        var secret = new SecretFundamentalSettings();
        var errorsAndInfos = new ErrorsAndInfos();
        FundamentalSettings settings = await container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsFalse(string.IsNullOrEmpty(settings.BankStatementInfix));
    }
}