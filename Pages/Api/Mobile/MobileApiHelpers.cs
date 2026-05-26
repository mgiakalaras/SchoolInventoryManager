namespace SchoolInventoryManager.Pages.Api.Mobile;

public static class MobileApiHelpers
{
    public static string ExtractCode(string value)
    {
        var input = (value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length >= 2 &&
                segments[^2].Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(segments[^1]).Trim();
            }

            if (segments.Length >= 3 &&
                segments[^3].Equals("Items", StringComparison.OrdinalIgnoreCase) &&
                segments[^2].Equals("Qr", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(segments[^1]).Trim();
            }

            if (segments.Length > 0)
            {
                return Uri.UnescapeDataString(segments[^1]).Trim();
            }
        }

        if (input.Contains("/q/", StringComparison.OrdinalIgnoreCase))
        {
            return input[(input.LastIndexOf("/q/", StringComparison.OrdinalIgnoreCase) + 3)..]
                .Trim()
                .Trim('/');
        }

        if (input.Contains("/Items/Qr/", StringComparison.OrdinalIgnoreCase))
        {
            return input[(input.LastIndexOf("/Items/Qr/", StringComparison.OrdinalIgnoreCase) + 10)..]
                .Trim()
                .Trim('/');
        }

        return input;
    }
}
