using EmailService.Internal;
using EmailService.Models;
using Marten.Events.Aggregation;

namespace EmailService.Events;

public sealed class EmailProjection : SingleStreamProjection<Email> {
    public static Email Create(EmailSubmitted submitted) => new() {
        Id = submitted.EmailId,
        Sender = submitted.Sender,
        Recipient = submitted.Recipient,
        Subject = submitted.Subject,
        Body = submitted.Body,
        SubmittedOn = submitted.Timestamp
    };

    public static Email Apply(EmailSendAttemptFailed failure, Email email) {
        email.Retries++;
        email.Status = email.Retries >= EmailConstants.MAX_RETIRES ? EmailStatus.Undeliverable : EmailStatus.Failed;
        email.LastSendAttempt = failure.Timestamp;
        return email;
    }

    public static Email Apply(EmailSent sent, Email email) {
        email.Status = EmailStatus.Delivered;
        email.LastSendAttempt = sent.Timestamp;
        email.DeliveryDate = sent.Timestamp;
        return email;
    }
}
