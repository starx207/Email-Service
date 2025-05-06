using EmailService.Internal;
using EmailService.Models;
using Marten.Events.Aggregation;

namespace EmailService.Events;

public sealed class EmailProjection : SingleStreamProjection<Email> {
    public static Email Create(EmailSubmitted submitted) => new() {
        Sender = submitted.Sender,
        Recipient = submitted.Recipient,
        Subject = submitted.Subject,
        Body = submitted.Body,
        SubmittedOn = submitted.Timestamp
    };

    public static Email Apply(EmailSendAttemptFailed failure, Email email) {
        var currentStatus = email.Status;
        if (currentStatus == EmailStatus.Failed) {
            email.Retries++; // Increment the retry count if the email has already failed before.
        }
        email.Status = email.Retries >= EmailConstants.MAX_RETIRES ? EmailStatus.Undeliverable : EmailStatus.Failed;
        email.LastSendAttempt = failure.Timestamp;
        return email;
    }

    public static Email Apply(EmailSent sent, Email email) {
        var currentStatus = email.Status;
        if (currentStatus == EmailStatus.Failed) {
            email.Retries++; // Increment the retry count if the email has already failed before.
        }
        email.Status = EmailStatus.Delivered;
        email.LastSendAttempt = sent.Timestamp;
        email.DeliveryDate = sent.Timestamp;
        return email;
    }
}
