using System.Text.Json.Serialization;
using EarthBackground.Captors;

namespace EarthBackground;

/// <summary>
/// AOT/trim-friendly JSON metadata for System.Text.Json source generation.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(LastestTimes))]
[JsonSerializable(typeof(CDNOperationResult))]
[JsonSerializable(typeof(DateResult))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
