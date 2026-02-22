using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalDynamics.Foundation.Idempotency.Models;

namespace DigitalDynamics.Foundation.Idempotency.Internal;

[JsonSerializable(typeof(IdempotencyEntry))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class IdempotencyJsonContext : JsonSerializerContext
{
}
