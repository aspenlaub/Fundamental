using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class SecretFundamentalSettings : ISecret<FundamentalSettings> {
    private static FundamentalSettings _defaultFundamentalSettings;
    public FundamentalSettings DefaultValue
        => _defaultFundamentalSettings
            ??= new FundamentalSettings { BankStatementInfix = @"007_"};

    public string Guid => "CE02A78A-C4A5-4F66-B2E1-679567F6A879";
}