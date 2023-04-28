using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class SecurityDto : ISecurity {
    public string SecurityId { get; set; }
    public string SecurityName { get; set; }
    public double QuotedPer { get; set; }

    public SecurityDto(ISecurity security) {
        SecurityId = security.SecurityId;
        SecurityName = security.SecurityName;
        QuotedPer = security.QuotedPer;
    }
}