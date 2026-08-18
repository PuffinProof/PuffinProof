namespace PuffinProof.Core;

public static class VersionCompare
{
    public static bool IsNewer(string? latest, string? installed)
    {
        if (string.IsNullOrWhiteSpace(latest))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(installed))
        {
            return true;
        }

        if (Version.TryParse(Normalize(latest), out var a) &&
            Version.TryParse(Normalize(installed), out var b))
        {
            return a > b;
        }

        return !string.Equals(latest, installed, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        value = value.Trim().TrimStart('v', 'V');
        var parts = value.Split('.');
        while (parts.Length < 3)
        {
            value += ".0";
            parts = value.Split('.');
        }

        return value;
    }
}
