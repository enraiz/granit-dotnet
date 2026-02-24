using System.Text.Json;
using System.Text.Json.Serialization;
using Granit.Idempotency.Models;

namespace Granit.Idempotency.Internal;

[JsonSerializable(typeof(IdempotencyEntry))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class IdempotencyJsonContext : JsonSerializerContext
{
}
