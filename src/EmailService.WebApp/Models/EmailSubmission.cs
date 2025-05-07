using System.ComponentModel.DataAnnotations;

namespace EmailService.WebApp.Models;

public class EmailSubmission {
    [Required, EmailAddress]
    public string Recipient { get; set; } = string.Empty;
    [Required, MinLength(1)]
    public string Subject { get; set; } = string.Empty;
    [Required, MinLength(1)]
    public string Body { get; set; } = string.Empty;
}
