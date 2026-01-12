using System.Text.Json.Serialization;

namespace GitHubDevOpsLink.Services.Models;

/// <summary>
///     JSON source generation context for AzureDevOpsConfiguration to support Native AOT.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AzureDevOpsConfiguration))]
public partial class AzureDevOpsJsonContext : JsonSerializerContext
{
}