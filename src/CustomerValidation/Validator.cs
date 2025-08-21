using System.Text.RegularExpressions;

namespace CustomerValidation
{
    public record ValidationResult(bool IsValid, string Message);

    public static class Validator
    {
        public static ValidationResult ValidateEmail(string email)
            => string.IsNullOrWhiteSpace(email)
               ? new(false, "Email is required.")
               : Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")
                 ? new(true, "Email is valid")
                 : new(false, "Invalid email format");

        public static ValidationResult ValidatePhoneNumber(string phoneNumber)
            => string.IsNullOrWhiteSpace(phoneNumber)
               ? new(false, "Phone number is required.")
               : Regex.IsMatch(phoneNumber, @"^(\(\d{3}\)\s?\d{3}-\d{4}|\d{3}-\d{3}-\d{4})$")
                 ? new(true, "Phone number is valid")
                 : new(false, "Invalid phone number format");
    }
}

