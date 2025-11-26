using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Test.Core;

public class TestHelper(IContextFactory contextFactory) {
    protected IContextFactory ContextFactory = contextFactory;

    public bool IsHoldingEqualTo(Holding holding, DateTime date, string securityId, double nominal, double costValue, double quoteValue,
                                 double realizedLoss, double realizedProfit, double unrealizedLoss, double unrealizedProfit) {
        const double tolerance = 0.001;
        return holding.Date == date && holding.Security.SecurityId == securityId && Math.Abs(holding.NominalBalance - nominal) < tolerance
               && Math.Abs(holding.CostValueInEuro - costValue) < tolerance && Math.Abs(holding.QuoteValueInEuro - quoteValue) < tolerance
               && Math.Abs(holding.RealizedLossInEuro - realizedLoss) < tolerance && Math.Abs(holding.RealizedProfitInEuro - realizedProfit) < tolerance
               && Math.Abs(holding.UnrealizedLossInEuro - unrealizedLoss) < tolerance && Math.Abs(holding.UnrealizedProfitInEuro - unrealizedProfit) < tolerance;
    }

    public async Task PopulateDatabaseWithTestRepositoryDataAsync() {
        await TruncateAllTablesAsync(EnvironmentType.UnitTest);
        var repository = new TestDataRepository();
        await using var db = await ContextFactory.CreateAsync(EnvironmentType.UnitTest);
        db.Securities.AddRange(repository.Securities);
        db.Quotes.AddRange(repository.Quotes);
        db.Transactions.AddRange(repository.Transactions);
        db.SaveChanges();
    }

    public async Task TruncateAllTablesAsync(EnvironmentType environmentType) {
        await using var db = await ContextFactory.CreateAsync(environmentType);
        db.Migrate();
        db.DateSummaries.RemoveRange(db.DateSummaries);
        db.Holdings.RemoveRange(db.Holdings);
        db.Transactions.RemoveRange(db.Transactions);
        db.Quotes.RemoveRange(db.Quotes);
        db.Securities.RemoveRange(db.Securities);
        db.SaveChanges();
    }
}