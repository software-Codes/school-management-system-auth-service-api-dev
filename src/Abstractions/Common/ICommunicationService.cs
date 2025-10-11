namespace AuthService.Abstractions.Common;

public interface ICommunicationService
{
    Task SendSmsAsync(string phoneNumber, string message);
    Task SendEmailAsync(string emailAddress, string subject, string htmlBody);
}