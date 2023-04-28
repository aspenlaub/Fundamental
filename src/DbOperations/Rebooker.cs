using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.DbOperations;

public class Rebooker {
    protected EnvironmentType EnvironmentType;
    protected IContextFactory ContextFactory;

    public Rebooker(EnvironmentType environmentType, IContextFactory contextFactory) {
        EnvironmentType = environmentType;
        ContextFactory = contextFactory;
    }

    public async Task RebookAsync(HashSet<string> securityIdsWithDataToSave) {
        foreach (var securityId in securityIdsWithDataToSave) {
            await using var rebookContext = await ContextFactory.CreateAsync(EnvironmentType);
            var transactions = rebookContext.Transactions.Where(t => t.Security.SecurityId == securityId).Include(t => t.Security).ToList();
            var quotes = rebookContext.Quotes.Where(q => q.Security.SecurityId == securityId).Include(q => q.Security).ToList();
            var holdings = new HoldingCalculator().WithQuotes(quotes).WithTransactions(transactions).CalculateHoldings().ToList();
            rebookContext.Holdings.RemoveRange(rebookContext.Holdings.Where(h => h.Security.SecurityId == securityId));
            rebookContext.Holdings.AddRange(holdings);
            rebookContext.SaveChanges();
            rebookContext.DateSummaries.RemoveRange(rebookContext.DateSummaries);
            var dateSummaries = new DateSummaryCalculator().WithHoldings(rebookContext.Holdings.Include(h => h.Security).ToList()).CalculateDateSummaries();
            rebookContext.DateSummaries.AddRange(dateSummaries);
            rebookContext.SaveChanges();
        }
    }
}