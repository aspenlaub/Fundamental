using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class FundamentalSettings : ISecretResult<FundamentalSettings> {
    [XmlAttribute("bankstatementinfix")]
    public string BankStatementInfix { get; set; }

    public FundamentalSettings Clone() {
        return new FundamentalSettings {
            BankStatementInfix = BankStatementInfix
        };
    }
}