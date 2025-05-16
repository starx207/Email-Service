using EmailService.Internal.Dto;
using FluentValidation;

namespace EmailService.Internal.Services;

internal class QueuedEmailValidator : AbstractValidator<EmailQueued> {
    public static readonly QueuedEmailValidator Instance = new();

    private QueuedEmailValidator() {
        RuleFor(x => x.Recipient)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Subject).NotEmpty();

        RuleFor(x => x.Message).NotEmpty();
    }
}
