using System.Text.Json.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class SecurityDto(ISecurity security) : ISecurity {
    [JsonPropertyName("SecurityId")]
    public string SecurityId { get; set; } = security.SecurityId;

    [JsonPropertyName("SecurityName")]
    public string SecurityName { get; set; } = security.SecurityName;

    [JsonPropertyName("QuotedPer")]
    public double QuotedPer { get; set; } = security.QuotedPer;
}