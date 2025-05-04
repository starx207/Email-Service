namespace EmailService.Api;

public static class ApiEndpoints {
    private const string API_BASE = "/api";

    public static class Email {
        private const string BASE = $"{API_BASE}/email";

        public const string SEND = BASE;
    }
}
