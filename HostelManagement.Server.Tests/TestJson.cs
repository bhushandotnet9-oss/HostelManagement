using System.Text.Json;
using System.Text.Json.Serialization;

namespace HostelManagement.Server.Tests;

/// <summary>
/// Matches the server's JsonStringEnumConverter so tests can deserialize enum
/// fields (e.g. CotResponse.Status) the same way the real client does.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
