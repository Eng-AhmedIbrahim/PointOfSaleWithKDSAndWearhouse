namespace POS.BackOffice.Helpers;

public static class ValidationHelpers
{
    public static bool BeArabic(string? arabicName)
    {
        if (string.IsNullOrEmpty(arabicName))
            return true;

        foreach (char c in arabicName)
        {
            if (!char.IsLetter(c) && !char.IsWhiteSpace(c))
                return false;

            if (c >= 1536 && c <= 1791) // Arabic characters range
                continue;

            return false;
        }

        return true;
    }
}
