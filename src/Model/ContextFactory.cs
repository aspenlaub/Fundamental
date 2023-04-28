using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;

public class ContextFactory : IContextFactory {
    public async Task<Context> CreateAsync(EnvironmentType environmentType) {
        var dataSources = await Context.GetDataSourcesAsync();
        return new Context(environmentType, dataSources);
    }

    public async Task<Context> CreateAsync(EnvironmentType environmentType, SynchronizationContext synchronizationContext) {
        var dataSources = await Context.GetDataSourcesAsync();
        return new Context(environmentType, synchronizationContext, dataSources);
    }
}