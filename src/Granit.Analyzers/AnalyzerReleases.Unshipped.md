; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
GRMIGA001 | Migrations | Error | DropColumnWithoutContractAnalyzer, IsEnabledByDefault=True
GRMIGA002 | Migrations | Error | RenameColumnForbiddenAnalyzer, IsEnabledByDefault=True
GRMIGA003 | Migrations | Warning | NullableColumnInExpandAnalyzer, IsEnabledByDefault=True
GRMIGA004 | Migrations | Warning | AlterColumnWithoutContractAnalyzer, IsEnabledByDefault=True
