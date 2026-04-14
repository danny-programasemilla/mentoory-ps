# Developer Quickstart: Notification Email System

**Date**: 2026-04-13

## Prerequisites

1. .NET 10.0 SDK
2. SQL Server (via Docker/Aspire or local instance)
3. Mailtrap.io account (free tier is sufficient for development)

## Setup Steps

### 1. Mailtrap Account

1. Sign up at [mailtrap.io](https://mailtrap.io)
2. Create an inbox (or use the default one)
3. Go to **SMTP Settings** and note:
   - Host: `sandbox.smtp.mailtrap.io`
   - Port: `2525`
   - Username: `<your-username>`
   - Password: `<your-password>`

### 2. Configuration

Add SMTP settings to `Mentoory.Web/appsettings.Development.json`:

```json
{
  "Smtp": {
    "Host": "sandbox.smtp.mailtrap.io",
    "Port": 2525,
    "Username": "<your-mailtrap-username>",
    "Password": "<your-mailtrap-password>",
    "FromAddress": "noreply@mentoory.com",
    "FromName": "Mentoory",
    "UseSsl": false
  },
  "Notification": {
    "PollingIntervalSeconds": 15,
    "MaxRetryAttempts": 5,
    "BaseUrl": "https://localhost:7001"
  }
}
```

> **Do not commit credentials.** Use User Secrets or environment variables for sensitive values.

### 3. Database

After publishing the DACPAC (which includes the new `notification` schema tables):

```bash
cd Mentoory.Db && ./publish-mentoorydb.sh
```

This creates:
- `[notification].[Notifications]`
- `[notification].[NotificationRecipients]`
- `[notification].[DeliveryAttempts]`
- `[notification].[NotificationPreferences]`

### 4. Run

```bash
# With Aspire (recommended)
dotnet run --project Mentoory.Aspire.AppHost

# Web only
dotnet run --project Mentoory.Web
```

The `NotificationProcessorService` starts automatically as a hosted service and polls every 15 seconds.

## Verification

### Test Registration Email

1. Log in as GlobalAdmin or ProjectCoordinator
2. Create a new user via the user management interface
3. Check your Mailtrap inbox -- a welcome/verification email should arrive within ~30 seconds
4. Verify the email contains:
   - User's name in the greeting
   - Project name
   - Clickable verification link
   - Expiration timeframe
   - Spanish text throughout
   - Branded layout (header, card, footer)

### Test Login Alert

1. Log in as any user
2. Check Mailtrap -- a login alert should arrive within ~30 seconds
3. Verify it contains IP address, browser name, OS name, and timestamp

### Test Suspicious Login Alert

1. Attempt 3+ failed logins with wrong password
2. Log in successfully
3. Check Mailtrap -- the alert should have subject "Actividad sospechosa detectada" with urgency styling

### Test Notification Preferences

1. Disable login alerts for a user (via API/command)
2. Log in as that user
3. Verify no login alert email is sent
4. Verify registration/invitation emails still send regardless

## Troubleshooting

| Symptom | Check |
|---------|-------|
| No emails arriving | Verify SMTP credentials in appsettings. Check Mailtrap inbox (not spam). Check application logs for SMTP errors |
| Emails delayed > 2 min | Check `NotificationProcessorService` logs. Verify polling interval is 15s. Check if notifications are stuck in `Pending` |
| Template rendering errors | Check logs for RazorLight compilation errors. Verify template `.cshtml` files are embedded resources |
| "Navegador desconocido" in login alerts | UAParser couldn't parse the User-Agent. Check the raw `UserAgentString` in the event payload |

## Adding a New Notification Type

1. Add value to `NotificationType` enum
2. Create a Razor template in `Mentoory.Notification.Infrastructure/Templates/`
3. Create a strongly-typed model for the template
4. Create an integration event handler in `Mentoory.Notification.Application/IntegrationEvents/`
5. No changes needed to infrastructure code (email service, processor, renderer)
