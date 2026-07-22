using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;

public class TestHelper(IContextFactory contextFactory) {
    protected IContextFactory ContextFactory = contextFactory;

    public static bool IsHoldingEqualTo(Holding holding, DateTime date, string securityId, double nominal, double costValue, double quoteValue,
                                 double realizedLoss, double realizedProfit, double unrealizedLoss, double unrealizedProfit) {
        return holding.Date == date && holding.Security.SecurityId == securityId && Math.Abs(holding.NominalBalance - nominal) < Constants.ZeroLimit
               && Math.Abs(holding.CostValueInEuro - costValue) < Constants.ZeroLimit && Math.Abs(holding.QuoteValueInEuro - quoteValue) < Constants.ZeroLimit
               && Math.Abs(holding.RealizedLossInEuro - realizedLoss) < Constants.ZeroLimit && Math.Abs(holding.RealizedProfitInEuro - realizedProfit) < Constants.ZeroLimit
               && Math.Abs(holding.UnrealizedLossInEuro - unrealizedLoss) < Constants.ZeroLimit && Math.Abs(holding.UnrealizedProfitInEuro - unrealizedProfit) < Constants.ZeroLimit;
    }

    public async Task PopulateDatabaseWithTestRepositoryDataAsync() {
        await TruncateAllTablesAsync(EnvironmentType.UnitTest);
        var repository = new TestDataRepository();
        await using Context db = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        db.Securities.AddRange(repository.Securities);
        db.Quotes.AddRange(repository.Quotes);
        db.Transactions.AddRange(repository.Transactions);
        db.SaveChanges();
    }

    public async Task TruncateAllTablesAsync(EnvironmentType environmentType) {
        await using Context db = await ContextFactory.CreateAsync(environmentType);
        db.Migrate();
        db.DateSummaries.RemoveRange(db.DateSummaries);
        db.Holdings.RemoveRange(db.Holdings);
        db.Transactions.RemoveRange(db.Transactions);
        db.Quotes.RemoveRange(db.Quotes);
        db.Securities.RemoveRange(db.Securities);
        db.SaveChanges();
    }
}