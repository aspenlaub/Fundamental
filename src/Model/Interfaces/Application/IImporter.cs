using System.Threading.Tasks;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;

public interface IImporter {
    Task ImportAFileAsync(string bankStatementInfix);
}