using EmailService.Models;

namespace EmailService.Internal.Extensions;

internal static class ValidationExtensions {
    public static ValidationErr[] ToErrArray(this FluentValidation.Results.ValidationResult validationResult)
        => validationResult.IsValid
        ? []
        : [..validationResult.Errors.Select(vr => new ValidationErr(vr.PropertyName, vr.AttemptedValue, vr.ErrorMessage))];
}
