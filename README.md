# Email-Service

## Overview
The Email-Service application is designed to handle high-volume email sending efficiently and reliably. It includes features such as retry logic, logging, and API integration.

## Features
- Asynchronous email sending
- Retry logic for failed emails
- Logging of email details
- API and frontend integration
- SDK for easy consumption of the API

## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/)
- [PostgreSQL](https://www.postgresql.org/)

## Configuration
1. **Environment Variables**:
   - Set the following environment variables in your system or directly in the `docker-compose.override.yml` file:
     - **EMAIL__SENDER**: The email account from which to send emails.
     - **EMAIL__PASSWORD**: The password for the sender email account.
     - **CONNECTIONSTRINGS__USER**: The user to use for connecting to the Postgres database.
     - **CONNECTIONSTRINGS__PASSWORD**: The password to use for connecting to the Postgres database.
     
     In `docker-compose.override.yml`:
     ```yml
     emailservice.api:
       environment:
         - ASPNETCORE_ENVIRONMENT=Development
         - ASPNETCORE_URLS=http://+:80
         - EMAIL__SENDER=${EMAIL__SENDER}
         - EMAIL__PASSWORD=${EMAIL__PASSWORD}
         - CONNECTIONSTRINGS__PASSWORD=${CONNECTIONSTRINGS__PASSWORD}
         - CONNECTIONSTRINGS__USER=${CONNECTIONSTRINGS__USER}


      db:
        image: postgres:17.4
        ports:
          - "5432:5432"
        environment:
          POSTGRES_USER: ${CONNECTIONSTRINGS__USER}
          POSTGRES_PASSWORD: ${CONNECTIONSTRINGS__PASSWORD}
          POSTGRES_DB: EmailServiceDb
     ```
        > NOTE: Underscores in each variable name are doubled.
2. **Email Settings**
   - In the `appsettings.Development.json` file, configure the following settings under the `Email` section:
     - **Host**: The SMTP server to use for sending emails. For example, `smtp.gmail.com` for Gmail.
     - **Port**: The port to connect to the SMTP server. Common values are `587` for StartTLS or `465` for SSL.
     - **SecureSocket**: The security protocol to use. Options include:
       - `None`: No SSL or TLS encryption should be used.
       - `Auto`: Automatically decide which SSL or TLS options to use. If the server does not support SSL or TLS, the connection will continue without encryption.
       - `SslOnConnect`: Use SSL or TLS encryption immediately upon connection.
       - `StartTls`: Elevate the connection to use TLS encryption immediately after reading the server's greeting and capabilities. If the server does not support STARTTLS, the connection will fail.
       - `StartTlsWhenAvailable`: Elevate the connection to use TLS encryption immediately after reading the server's greeting and capabilities, but only if the server supports STARTTLS.

   Example configuration:

   ```json
   "Email": {
     "Host": "smtp.gmail.com",
     "Port": 587,
     "SecureSocket": "StartTls"
   }
   ```
   >NOTE: Different SMTP servers may require additional configuration to be able to send emails from their accounts. For example, I used a Gmail account and had to first setup an [App Password](https://support.google.com/accounts/answer/185833?hl=en) to bypass Google's MFA policy.

## Build and Run
1. Build and run the Docker containers:
   ```bash
   $ docker compose up --build
   ```
2. Access the API at `http://localhost:5000` and the web application at `http://localhost:5001`.
    
    > NOTE: I chose to forego HTTPS as I was developing using GitHub Codespaces and wasn't sure how to handle the dev certificates in that environment - I didn't want to get bogged down on that detail.

## Testing
To run the tests, execute:
```bash
$ dotnet test
```