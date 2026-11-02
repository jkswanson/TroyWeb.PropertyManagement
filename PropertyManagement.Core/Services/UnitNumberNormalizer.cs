namespace PropertyManagement.Core.Services;

public static class UnitNumberNormalizer
{
    public static string Normalize(string value)
    {
        var normalized = value.Trim();

        if (normalized.Length > 0 &&
            normalized.All(c => c >= '0' && c <= '9'))
        {
            normalized = normalized.TrimStart('0');

            if (normalized.Length == 0)
                normalized = "0";

            return normalized.PadLeft(3, '0');
        }

        return normalized;
    }
}
