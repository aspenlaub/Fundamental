using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Entities.Data;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;

public class ContextFactory : IContextFactory {
    public async Task<Context> CreateAsync(EnvironmentType environmentType) {
        DataSources dataSources = await Context.GetDataSourcesAsync();
        return new Context(environmentType, dataSources);
    }

    public async Task<Context> CreateAsync(EnvironmentType environmentType, SynchronizationContext synchronizationContext) {
        DataSources dataSources = await Context.GetDataSourcesAsync();
        return new Context(environmentType, synchronizationContext, dataSources);
    }
}