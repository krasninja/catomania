using System.Text.Json.Serialization;
using Catomania.Models;

namespace Catomania.Services;

/// <summary>
/// Source generated JSON context, keeps serialization AOT friendly.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class CatomaniaJsonContext : JsonSerializerContext
{
}
