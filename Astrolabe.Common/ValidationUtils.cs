using System.Text.RegularExpressions;

namespace Astrolabe.Common;

public static class ValidationUtils
{
    private static readonly Regex EmailRegex = new(@"^\S+@\S+$");
    
    public static bool IsValidEmail(string? email)
    {
        return email != null && EmailRegex.IsMatch(email);
    }
}