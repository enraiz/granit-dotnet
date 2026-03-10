using System.Security.Cryptography;
using Granit.Guids.Options;
using Granit.Timing;
using Microsoft.Extensions.Options;

namespace Granit.Guids;

/// <summary>
/// Implementation of <see cref="IGuidGenerator"/> that generates sequential GUIDs
/// optimized for clustered indexes.
/// </summary>
public sealed class SequentialGuidGenerator(IOptions<GuidGeneratorOptions> options, IClock clock) : IGuidGenerator
{
    private readonly GuidGeneratorOptions _options = options.Value;
    private readonly IClock _clock = clock;

    /// <inheritdoc />
    public Guid Create() => Create(_options.GetDefaultSequentialGuidType());

    /// <summary>
    /// Creates a new sequential GUID of the specified type.
    /// </summary>
#pragma warning disable CA1822 // Intentionally non-static public method for API consistency
    public Guid Create(SequentialGuidType guidType) // NOSONAR S2325
#pragma warning restore CA1822
    {
        // 10 cryptographically secure random bytes — stack-allocated to avoid GC pressure
        Span<byte> randomBytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(randomBytes);

        // Timestamp in milliseconds since DateTime.MinValue
        long timestamp = _clock.Now.UtcTicks / 10000L;

        // Convert the timestamp to a byte span (8 bytes) — stack-allocated
        Span<byte> timestampBytes = stackalloc byte[8];
        BitConverter.TryWriteBytes(timestampBytes, timestamp);

        // Big-endian for correct sorting
        if (BitConverter.IsLittleEndian)
        {
            timestampBytes.Reverse();
        }

        Span<byte> guidBytes = stackalloc byte[16];

        switch (guidType)
        {
            case SequentialGuidType.SequentialAsString:
            case SequentialGuidType.SequentialAsBinary:
                // Timestamp at the front (6 bytes), then random (10 bytes)
                timestampBytes.Slice(2, 6).CopyTo(guidBytes);
                randomBytes.CopyTo(guidBytes.Slice(6));

                // Endianness correction for the string format
                // Guid(ReadOnlySpan<byte>) interprets Data1 and Data2 as little-endian
                if (guidType == SequentialGuidType.SequentialAsString
                    && BitConverter.IsLittleEndian)
                {
                    guidBytes.Slice(0, 4).Reverse(); // Data1
                    guidBytes.Slice(4, 2).Reverse(); // Data2
                }

                break;

            case SequentialGuidType.SequentialAtEnd:
                // Random at the front (10 bytes), then timestamp (6 bytes)
                randomBytes.CopyTo(guidBytes);
                timestampBytes.Slice(2, 6).CopyTo(guidBytes.Slice(10));
                break;
        }

        return new Guid(guidBytes);
    }
}
