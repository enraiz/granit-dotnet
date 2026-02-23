namespace Granit.Timing;

/// <summary>
/// Desactive la normalisation automatique des DateTimeOffset sur l'element annote.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class DisableDateTimeNormalizationAttribute : Attribute;
