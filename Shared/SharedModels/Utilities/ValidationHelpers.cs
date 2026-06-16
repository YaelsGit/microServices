using System.Text.RegularExpressions;

namespace SharedModels.Utilities;

public static class ValidationHelpers
{
    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string password)
    {
        // At least 8 characters, 1 uppercase, 1 lowercase, 1 digit
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        var hasUpperCase = Regex.IsMatch(password, "[A-Z]");
        var hasLowerCase = Regex.IsMatch(password, "[a-z]");
        var hasDigit = Regex.IsMatch(password, "[0-9]");

        return hasUpperCase && hasLowerCase && hasDigit;
    }

    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Simple check: at least 7 digits
        var digitsOnly = Regex.Replace(phoneNumber, "[^0-9]", "");
        return digitsOnly.Length >= 7;
    }

    public static bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Length >= 2 && name.Length <= 100;
    }

    public static bool IsValidPrice(decimal price)
    {
        return price > 0 && price <= 999999.99m;
    }

    public static bool IsValidQuantity(int quantity)
    {
        return quantity > 0 && quantity <= 999999;
    }

    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.Trim();
    }
}
