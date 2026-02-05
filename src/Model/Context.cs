using System;
using System.Threading;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Data;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Entities.Data;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;

public class Context : ContextBase {
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Holding> Holdings { get; set; }
    public DbSet<DateSummary> DateSummaries { get; set; }
    public DbSet<Security> Securities { get; set; }

    public static EnvironmentType DefaultEnvironmentType =
#if DEBUG
        EnvironmentType.Qualification;
#else
        EnvironmentType.Production;
#endif

    public Context() : this(DefaultEnvironmentType, SynchronizationContext.Current) { }
    public Context(EnvironmentType environmentType) : this(environmentType, SynchronizationContext.Current) { }
    public Context(EnvironmentType environmentType, SynchronizationContext uiSynchronizationContext) : this(environmentType, uiSynchronizationContext, DefaultDataSources()) { }
    public Context(DataSources dataSources) : this(DefaultEnvironmentType, SynchronizationContext.Current, dataSources) { }
    public Context(EnvironmentType environmentType, DataSources dataSources) : this(environmentType, SynchronizationContext.Current, dataSources) { }

    public Context(EnvironmentType environmentType, SynchronizationContext uiSynchronizationContext, DataSources dataSources)
        : base(environmentType, uiSynchronizationContext, "Aspenlaub.Net.GitHub.CSharp.Fundamental",
            [], dataSources) {
        DefaultEnvironmentType = environmentType;
        EnvironmentType = environmentType;
    }

    public static async Task<DataSources> GetDataSourcesAsync() {
        IContainer container = new ContainerBuilder().UsePegh("Fundamental").Build();
        ISecretRepository secretRepository = container.Resolve<ISecretRepository>();
        var secretDataSources = new SecretDataSources();
        var errorsAndInfos = new ErrorsAndInfos();
        DataSources dataSources = await secretRepository.GetAsync(secretDataSources, errorsAndInfos);
        return errorsAndInfos.AnyErrors()
            ? throw new Exception(string.Join("\r\n", errorsAndInfos.Errors))
            : dataSources;
    }

    public static DataSources DefaultDataSources() {
        return [ new DataSource { MachineId = Environment.MachineName, TheDataSource = $"{Environment.MachineName}\\SQLEXPRESS" } ];
    }
}