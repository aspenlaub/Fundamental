using System;
using System.Windows;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental;

public partial class App {
    protected override async void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        try {
            var contextFactory = new ContextFactory();
            await using var db = await contextFactory.CreateAsync(Context.DefaultEnvironmentType);
            db.Migrate();
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "Exception on startup", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
}