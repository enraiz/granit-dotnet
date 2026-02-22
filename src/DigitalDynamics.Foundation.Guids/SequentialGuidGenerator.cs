// =============================================================================
// SequentialGuidGenerator - Sequential GUID generation
// =============================================================================
// Generates sequential GUIDs by combining a millisecond timestamp (6 bytes)
// and cryptographically secure random bytes (10 bytes).
//
// Advantages over Guid.NewGuid():
//   - Clustered index: ordered inserts, no page splits
//   - Uniqueness: 80 bits of entropy (RandomNumberGenerator)
//   - Temporal ordering: successive GUIDs are monotonically increasing
//
// The algorithm handles endianness to guarantee correct sorting on all
// platforms. Three variants exist depending on the database
// (see SequentialGuidType).
//
// Thread-safe: RandomNumberGenerator is static and thread-safe.
// Registered as Singleton.
//
// Ported from Volo.Abp.Guids.SequentialGuidGenerator (MIT).
// =============================================================================

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Implementation of <see cref="IGuidGenerator"/> that generates sequential GUIDs
/// optimized for clustered indexes.
/// </summary>
public sealed class SequentialGuidGenerator : IGuidGenerator
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    private readonly GuidGeneratorOptions _options;

    public SequentialGuidGenerator(IOptions<GuidGeneratorOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Guid Create() => Create(_options.GetDefaultSequentialGuidType());

    /// <summary>
    /// Creates a new sequential GUID of the specified type.
    /// </summary>
<<<<<<< HEAD
#pragma warning disable CA1822 // Intentionally non-static public method for API consistency
=======
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Intentionally non-static for IGuidGenerator interface consistency")]
    [SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Intentionally non-static for IGuidGenerator interface consistency")]
>>>>>>> feature/settings-module
    public Guid Create(SequentialGuidType guidType)
    {
<<<<<<< HEAD
        // 10 cryptographically secure random bytes
        var randomBytes = new byte[10];
        Rng.GetBytes(randomBytes);

        // Timestamp in milliseconds since DateTime.MinValue
        var timestamp = DateTime.UtcNow.Ticks / 10000L;

        // Convert the timestamp to a byte array (8 bytes)
        var timestampBytes = BitConverter.GetBytes(timestamp);
=======
        // 10 octets aleatoires cryptographiquement surs
        byte[] randomBytes = new byte[10];
        Rng.GetBytes(randomBytes);

        // Timestamp en millisecondes depuis DateTime.MinValue
        long timestamp = DateTime.UtcNow.Ticks / 10000L;

        // Convertir le timestamp en tableau d'octets (8 octets)
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);
>>>>>>> feature/settings-module

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
