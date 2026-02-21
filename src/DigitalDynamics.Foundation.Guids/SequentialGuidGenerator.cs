// =============================================================================
// SequentialGuidGenerator - Generation de GUID sequentiels
// =============================================================================
// Genere des GUID sequentiels en combinant un timestamp milliseconde (6 octets)
// et des octets aleatoires cryptographiquement surs (10 octets).
//
// Avantages par rapport a Guid.NewGuid() :
//   - Index clustered : insertions ordonnees, pas de page splits
//   - Unicite : 80 bits d'entropie (RandomNumberGenerator)
//   - Ordonnancement temporel : les GUID successifs sont croissants
//
// L'algorithme gere l'endianness pour garantir un tri correct sur toutes
// les plateformes. Trois variantes existent selon la base de donnees
// (voir SequentialGuidType).
//
// Thread-safe : RandomNumberGenerator est statique et thread-safe.
// Enregistre en Singleton.
//
// Porte de Volo.Abp.Guids.SequentialGuidGenerator (MIT).
// =============================================================================

using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Implementation de <see cref="IGuidGenerator"/> qui genere des GUID sequentiels
/// optimises pour les index clustered.
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
    /// Cree un nouveau GUID sequentiel du type specifie.
    /// </summary>
#pragma warning disable CA1822 // Methode publique intentionnellement non-statique pour coherence API
    public Guid Create(SequentialGuidType guidType)
#pragma warning restore CA1822
    {
        // 10 octets aleatoires cryptographiquement surs
        byte[] randomBytes = new byte[10];
        Rng.GetBytes(randomBytes);

        // Timestamp en millisecondes depuis DateTime.MinValue
        long timestamp = DateTime.UtcNow.Ticks / 10000L;

        // Convertir le timestamp en tableau d'octets (8 octets)
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);

        // Big-endian pour un tri correct
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }

        byte[] guidBytes = new byte[16];

        switch (guidType)
        {
            case SequentialGuidType.SequentialAsString:
            case SequentialGuidType.SequentialAsBinary:
                // Timestamp en tete (6 octets), puis random (10 octets)
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);

                // Correction endianness pour le format string
                // Guid(byte[]) interprete Data1 et Data2 en little-endian
                if (guidType == SequentialGuidType.SequentialAsString
                    && BitConverter.IsLittleEndian)
                {
                    Array.Reverse(guidBytes, 0, 4); // Data1
                    Array.Reverse(guidBytes, 4, 2); // Data2
                }

                break;

            case SequentialGuidType.SequentialAtEnd:
                // Random en tete (10 octets), puis timestamp (6 octets)
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 10);
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 10, 6);
                break;
        }

        return new Guid(guidBytes);
    }
}
