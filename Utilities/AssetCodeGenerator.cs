namespace SchoolInventoryManager.Utilities;

public static class AssetCodeGenerator
{
    public const string Prefix = "SIM";

    public static string CreateAssetCode(int id, DateTime? createdAt = null)
    {
        var year = (createdAt ?? DateTime.Now).Year;
        return $"{Prefix}-{year}-{id:000000}";
    }

    public static string CreateQrToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}
