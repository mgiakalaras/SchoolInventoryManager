$ErrorActionPreference = "Stop"

$root = Get-Location
$schemaPath = Join-Path $root "Data\SchemaUpgrade.cs"

if (-not (Test-Path $schemaPath)) {
    throw "Data\SchemaUpgrade.cs not found. Run this from the web app project root."
}

Remove-Item (Join-Path $root "tools\Apply_Patch41fFix_SchemaUpgradeWarnings.ps1") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $root "docs\WEB_41F_FIX_SCHEMA_WARNING_SCRIPT.md") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $root "PATCH_WEB_41F_FIX_SCHEMA_WARNING_SCRIPT.txt") -Force -ErrorAction SilentlyContinue

$content = [System.IO.File]::ReadAllText($schemaPath)

$method = @'
    private static void AddColumnIfMissing(AppDbContext db, string tableName, string columnName, string columnDefinition)
    {
        EnsureSafeSqlIdentifier(tableName, nameof(tableName));
        EnsureSafeSqlIdentifier(columnName, nameof(columnName));
        EnsureSafeSqlColumnDefinition(columnDefinition);

        var tableExists = db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = @tableName",
                new SqliteParameter("@tableName", tableName))
            .AsEnumerable()
            .FirstOrDefault();

        if (tableExists == 0)
        {
            return;
        }

        var existingColumns = db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info(@tableName)",
                new SqliteParameter("@tableName", tableName))
            .AsEnumerable()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingColumns.Contains(columnName))
        {
            var sql = "ALTER TABLE "
                + QuoteSqlIdentifier(tableName)
                + " ADD COLUMN "
                + QuoteSqlIdentifier(columnName)
                + " "
                + columnDefinition
                + ";";

            db.Database.ExecuteSqlRaw(sql);
        }
    }

    private static void EnsureSafeSqlIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SQL identifier cannot be empty.", parameterName);
        }

        if (value.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_'))
        {
            throw new ArgumentException($"Unsafe SQL identifier: {value}", parameterName);
        }
    }

    private static void EnsureSafeSqlColumnDefinition(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SQL column definition cannot be empty.", nameof(value));
        }

        if (value.Contains(';') || value.Contains("--") || value.Contains("/*") || value.Contains("*/"))
        {
            throw new ArgumentException($"Unsafe SQL column definition: {value}", nameof(value));
        }
    }

    private static string QuoteSqlIdentifier(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
'@

$pattern = '(?ms)^    private static void AddColumnIfMissing\(AppDbContext db, string tableName, string columnName, string columnDefinition\)\s*\{.*?^    \}\s*(?=^\})'
$matches = [regex]::Matches($content, $pattern)

if ($matches.Count -ne 1) {
    throw "Patch stopped. Expected 1 AddColumnIfMissing method, found $($matches.Count)."
}

$backupPath = "$schemaPath.bak_41f2_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $schemaPath $backupPath

$updated = [regex]::Replace($content, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $method }, 1)

$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText($schemaPath, $updated, $utf8Bom)

Write-Host "OK: Data\SchemaUpgrade.cs patched."
Write-Host "Backup: $backupPath"
Write-Host "Next: dotnet build"
