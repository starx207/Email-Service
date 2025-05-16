using System.Threading.Channels;
using EmailService.Internal.Dto;
using EmailService.Internal.Extensions;
using EmailService.Models;

namespace EmailService.Internal.Services;

internal sealed class EmailService : IEmailService {
    private readonly Channel<EmailQueued> _submittedChannel;
    private readonly Channel<EmailIdAssigned> _assignedChannel;

    public EmailService(Channel<EmailQueued> submittedChannel, Channel<EmailIdAssigned> assignedChannel) {
        _submittedChannel = submittedChannel;
        _assignedChannel = assignedChannel;
    }

    public async Task<Result<Guid, ValidationErr[]>> SendEmailAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default) {
        var queued = new EmailQueued {
            Recipient = recipient,
            Subject = subject,
            Message = message
        };
        if ((await QueuedEmailValidator.Instance.ValidateAsync(queued, cancellationToken)) is { IsValid: false } errs) {
            return errs.ToErrArray();
        } 
        
        var responseTask = GetAssignedEmailIdAsync(queued.TraceId, cancellationToken);

        await _submittedChannel.Writer.WriteAsync(queued, cancellationToken);
        var emailId = await responseTask;
        return emailId;
    }

    private async Task<Guid> GetAssignedEmailIdAsync(Guid traceId, CancellationToken token) {
        await Task.Yield();
        while (await _assignedChannel.Reader.WaitToReadAsync(token)) {
            if (_assignedChannel.Reader.TryPeek(out var assignment) && assignment.TraceId == traceId) {
                // We found the assignment for this traceId, so we can read it off the channel and return the emailId.
                // We read it off the channel to remove it from the queue.
                _ = await _assignedChannel.Reader.ReadAsync(token);
                return assignment.EmailId;
            }
        }
        return Guid.Empty;
    }
}
