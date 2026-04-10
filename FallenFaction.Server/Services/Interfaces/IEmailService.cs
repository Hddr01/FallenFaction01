namespace FallenFaction.Server.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink);
        Task SendContactMessageAsync(string fromEmail, string subject, string message);
        Task SendContactConfirmationAsync(string toEmail, string subject);
        Task SendPasswordResetAsync(string toEmail, string userName, string resetLink);
    }
}
