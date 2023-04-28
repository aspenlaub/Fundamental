namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;

public interface IAddTransaction {
    bool IsSecurityInFocus();
    bool IsAnInertTransactionPresent();
    void AddTransaction();
}