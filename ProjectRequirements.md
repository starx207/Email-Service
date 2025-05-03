# Project Requirements

## Overview
You are tasked with building a feature to handle email sending for a high-volume application. The solution must ensure that users are not delayed or interrupted due to email failures. The email-sending functionality should be efficient, reliable, and reusable across different applications.

## Requirements

### Functional Requirements
1. **Asynchronous Email Sending**: Ensure that email sending does not block or delay other operations in the application.
2. **Reusable DLL**: Implement the email-sending functionality in a reusable DLL that can be used across different applications and entry points.
3. **Logging**: Log or store the following details indefinitely:
   - Sender
   - Recipient
   - Subject
   - Body (excluding attachments)
   - Date
   - Status of the send attempt
4. **Retry Logic**: If an email fails to send, retry up to a maximum of 3 attempts. Retries can occur in succession or over a period of time.
5. **Credential Management**: Store all credentials in `appsettings.json` instead of hardcoding them.

### Minimum Requirements
- The email-sending DLL must be callable from a console application.
- **Test Email Input**: Provide functionality to input a recipient email address and send a test email.

### Extra Credit
1. **API Integration**: Attach the email-sending functionality to an API that can be called from Postman.
2. **Frontend Integration**: Create a frontend (e.g., WPF or ASP.NET web application) that calls the API to send emails.

## Constraints
- Code must be written in C#.
- Use third-party libraries if necessary to meet the requirements.

## Deliverables
- A working implementation of the email-sending feature.
- A reusable DLL for email sending.
- Logging and retry mechanisms.
- A console application, API, and/or frontend integration demonstrating the functionality.