using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ContractHome.Helper.Validation
{
    public class ValidDateAttribute(string dateFormat) : ValidationAttribute
    {
        public string DateFormat { get; set; } = dateFormat;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var dateStr = value as string;

            if (string.IsNullOrEmpty(dateStr))
            {
                return ValidationResult.Success;
            }

            if (DateTime.TryParseExact(dateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return ValidationResult.Success;
            }
            
            return new ValidationResult($"{ErrorMessage} 格式須為{DateFormat}，且必須是有效日期");
        }
    }
}
