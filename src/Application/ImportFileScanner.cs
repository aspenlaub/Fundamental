using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Application;

public class ImportFileScanner(EnvironmentType environmentType, IApplicationCommandExecutionContext executionContext,
            IFolderResolver folderResolver) : IImportFileScanner {
    protected IFolder RootFolder, InFolder;
    protected EnvironmentType EnvironmentType = environmentType;
    protected IApplicationCommandExecutionContext ExecutionContext = executionContext;
    protected IFolderResolver FolderResolver = folderResolver;

    public async Task<bool> AnythingToImportAsync() {
        await SetFoldersIfNecessaryAsync();

        var dirInfo = new DirectoryInfo(InFolder.FullName);
        return dirInfo.GetFiles("*.csv", SearchOption.TopDirectoryOnly).Any()
               || dirInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly).Any()
               || dirInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly).Any();
    }

    public async Task CopyInputFilesToEnvironmentsAsync() {
        await SetFoldersIfNecessaryAsync();

        var dirInfo = new DirectoryInfo(RootFolder.FullName);
        foreach (FileInfo file in dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly)) {
            await ExecutionContext.ReportAsync(new FeedbackToApplication() { Type = FeedbackType.LogInformation, Message = string.Format(Properties.Resources.FoundNewFile, file.Name) });
            foreach (EnvironmentType environmentType in Enum.GetValues(typeof(EnvironmentType))) {
                CopyFileWithinEnvironment(environmentType, file);
            }
            File.Delete(file.FullName);
            await ExecutionContext.ReportAsync(new FeedbackToApplication() { Type = FeedbackType.LogInformation, Message = string.Format(Properties.Resources.NewFileSpreadThenDeleted, file.Name) });
        }
    }

    private void CopyFileWithinEnvironment(EnvironmentType environmentType, FileSystemInfo file) {
        IFolder inFolder = RootFolder.SubFolder(Enum.GetName(typeof(EnvironmentType), environmentType)).SubFolder("In");
        if (environmentType == EnvironmentType.UnitTest) {
            return;
        }

        File.Copy(file.FullName, inFolder + file.Name, true);
    }

    private async Task SetFoldersIfNecessaryAsync() {
        if (RootFolder != null) { return; }

        var errorsAndInfos = new ErrorsAndInfos();
        RootFolder = (await FolderResolver.ResolveAsync("$(MainUserFolder)", errorsAndInfos)).SubFolder("Fundamental");
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }
        RootFolder.CreateIfNecessary();
        InFolder = RootFolder.SubFolder(Enum.GetName(typeof(EnvironmentType), EnvironmentType)).SubFolder("In");
        InFolder.CreateIfNecessary();
    }
}