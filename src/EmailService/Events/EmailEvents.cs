namespace EmailService.Events;

public sealed record EmailSubmitted(string Sender, string Recipient, string Subject, string Body, DateTime Timestamp);

public sealed record EmailSendAttemptFailed(DateTime Timestamp);

public sealed record EmailSent(DateTime Timestamp);
