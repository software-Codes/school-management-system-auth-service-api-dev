using Azure;
using Azure.Communication.Sms;
using Azure.Communication.Email;
using AuthService.Abstractions.Common;

namespace AuthService.Infrastructure.Communication
{
    public class AzureCommunicationService : ICommunicationService
    {
        private readonly SmsClient _smsClient;
        private readonly EmailClient _emailClient;
        private readonly string _fromPhoneNumber;
        private readonly string _fromEmailAddress;
        private readonly ILogger<AzureCommunicationService> _logger;

        public AzureCommunicationService(IConfiguration configuration, ILogger<AzureCommunicationService> logger)
        {
            _logger = logger;

            // Read from Key Vault (AzureCommunication--*) or appsettings (AzureCommunication:*)
            string connectionString = configuration["AzureCommunication--ConnectionString"] ?? configuration["AzureCommunication:ConnectionString"]
                                      ?? throw new InvalidOperationException("ACS ConnectionString not configured in Key Vault or appsettings");
            _fromPhoneNumber = configuration["AzureCommunication--FromPhoneNumber"] ?? configuration["AzureCommunication:FromPhoneNumber"]
                               ?? throw new InvalidOperationException("ACS FromPhoneNumber not configured in Key Vault or appsettings");
            _fromEmailAddress = configuration["AzureCommunication--FromEmailAddress"] ?? configuration["AzureCommunication:FromEmailAddress"]
                                ?? throw new InvalidOperationException("ACS FromEmailAddress not configured in Key Vault or appsettings");

            _smsClient = new SmsClient(connectionString);
            _emailClient = new EmailClient(connectionString);

            _logger.LogInformation("Azure Communication Service initialized successfully");
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

                var response = await _smsClient.SendAsync(
                    from: _fromPhoneNumber,
                    to: phoneNumber,
                    message: message
                );

                _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}. Status: {Status}, ErrorCode: {ErrorCode}",
                    phoneNumber, ex.Status, ex.ErrorCode);
                throw new InvalidOperationException($"Failed to send SMS to {phoneNumber}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending SMS to {PhoneNumber}", phoneNumber);
                throw;
            }
        }

        public async Task SendEmailAsync(string emailAddress, string subject, string htmlBody)
        {
            try
            {
                _logger.LogInformation("Sending email to {EmailAddress} with subject '{Subject}'", emailAddress, subject);

                // Create email content and recipient
                var emailContent = new EmailContent(subject)
                {
                    Html = htmlBody,
                    PlainText = StripHtmlTags(htmlBody)
                };
                var sender = _fromEmailAddress;
                var recipient = new EmailAddress(emailAddress);
                var recipients = new EmailRecipients(new List<EmailAddress> { recipient });

                var emailMessage = new EmailMessage(sender, recipients, emailContent);
                var response = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

                _logger.LogInformation("Email sent successfully to {EmailAddress}", emailAddress);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to send email to {EmailAddress}. Status: {Status}, ErrorCode: {ErrorCode}",
                    emailAddress, ex.Status, ex.ErrorCode);
                throw new InvalidOperationException($"Failed to send email to {emailAddress}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {EmailAddress}", emailAddress);
                throw;
            }
        }

        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove HTML tags
            var withoutTags = System.Text.RegularExpressions.Regex.Replace(input, "<[^>]+>", string.Empty);

            // Decode HTML entities (e.g., &nbsp;, &amp;, etc.)
            var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);

            // Clean up excessive whitespace
            var cleaned = System.Text.RegularExpressions.Regex.Replace(decoded, @"\s+", " ");

            return cleaned.Trim();
        }
    }
}
