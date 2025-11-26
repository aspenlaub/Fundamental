using System.Text.Json.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class SecurityDto : ISecurity {
    public SecurityDto() {
    }

    public SecurityDto(ISecurity security) : this() {
        SecurityId = security.SecurityId;
        SecurityName = security.SecurityName;
        QuotedPer = security.QuotedPer;
    }

    [JsonPropertyName("SecurityId")]
    public string SecurityId { get; set; }

    [JsonPropertyName("SecurityName")]
    public string SecurityName { get; set; }

    [JsonPropertyName("QuotedPer")]
    public double QuotedPer { get; set; }
}