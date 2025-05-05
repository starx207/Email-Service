namespace EmailService.Events;

public sealed record EmailSubmitted(Guid EmailId, string Sender, string Recipient, string Subject, string Body, DateTime Timestamp);

public sealed record EmailSendAttemptFailed(Guid EmailId, DateTime Timestamp);

public sealed record EmailSent(Guid EmailId, DateTime Timestamp);
