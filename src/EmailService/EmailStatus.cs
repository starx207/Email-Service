namespace EmailService;

public enum EmailStatus {
    /// <summary>
    /// Email submitted, but no send attempt has been made.
    /// </summary>
    Pending,

    /// <summary>
    /// 1 or more send attempts made and failed. Will be retried later.
    /// </summary>
    Failed,

    /// <summary>
    /// Email sent successfully.
    /// </summary>
    Delivered,

    /// <summary>
    /// Email failed to send on all retry attempts. Will no longer be retried.
    /// </summary>
    Undeliverable
}
