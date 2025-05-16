namespace EmailService.Models;

public record ValidationErr(string Field, object Value, string Problem);
