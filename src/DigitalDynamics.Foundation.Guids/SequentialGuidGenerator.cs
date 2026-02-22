using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Implementation of <see cref="IGuidGenerator"/> that generates sequential GUIDs
/// optimized for clustered indexes.
/// </summary>
public sealed class SequentialGuidGenerator(IOptions<GuidGeneratorOptions> options) : IGuidGenerator
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    private readonly GuidGeneratorOptions _options = options.Value;

    /// <inheritdoc />
    public Guid Create() => Create(_options.GetDefaultSequentialGuidType());

    /// <summary>
    /// Creates a new sequential GUID of the specified type.
    /// </summary>
#pragma warning disable CA1822 // Intentionally non-static public method for API consistency
    public Guid Create(SequentialGuidType guidType) // NOSONAR S2325
#pragma warning restore CA1822
    {
        // 10 cryptographically secure random bytes
        byte[] randomBytes = new byte[10];
        Rng.GetBytes(randomBytes);

        // Timestamp in milliseconds since DateTime.MinValue
        long timestamp = DateTime.UtcNow.Ticks / 10000L;

        // Convert the timestamp to a byte array (8 bytes)
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);

        // Big-endian for correct sorting
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }

        byte[] guidBytes = new byte[16];

        switch (guidType)
        {
            case SequentialGuidType.SequentialAsString:
            case SequentialGuidType.SequentialAsBinary:
                // Timestamp at the front (6 bytes), then random (10 bytes)
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);

                // Endianness correction for the string format
                // Guid(byte[]) interprets Data1 and Data2 as little-endian
                if (guidType == SequentialGuidType.SequentialAsString
                    && BitConverter.IsLittleEndian)
                {
                    Array.Reverse(guidBytes, 0, 4); // Data1
                    Array.Reverse(guidBytes, 4, 2); // Data2
                }

                break;

            case SequentialGuidType.SequentialAtEnd:
                // Random at the front (10 bytes), then timestamp (6 bytes)
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 10);
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 10, 6);
                break;
        }

        return new Guid(guidBytes);
    }
}
