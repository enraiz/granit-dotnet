; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
GRMIGA001 | Migrations | Error | DropColumnWithoutContractAnalyzer, IsEnabledByDefault=True
GRMIGA002 | Migrations | Error | RenameColumnForbiddenAnalyzer, IsEnabledByDefault=True
GRMIGA003 | Migrations | Warning | NullableColumnInExpandAnalyzer, IsEnabledByDefault=True
GRMIGA004 | Migrations | Warning | AlterColumnWithoutContractAnalyzer, IsEnabledByDefault=True
GRSEC001 | Security | Warning | DateTimeNowAnalyzer, IsEnabledByDefault=True
GRSEC002 | Security | Warning | GuidNewGuidAnalyzer, IsEnabledByDefault=True
GRSEC003 | Security | Error | HardcodedSecretAnalyzer, IsEnabledByDefault=True
GREF001 | EntityFramework | Warning | SynchronousSaveChangesAnalyzer, IsEnabledByDefault=True
