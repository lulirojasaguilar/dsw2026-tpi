using System.Text.RegularExpressions;

namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class ValidationsExtensions
{
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";
    public static bool IsEmailValid(
        this string? email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email, EmailPattern);
    }

    public static bool DigitCountBetween(
        this long value, 
        int minDigits, 
        int maxDigits)
    {
        var length = value.ToString().Length;
        return value > 0 && 
            length >= minDigits &&
            length <= maxDigits;
    }
}
