using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IEmailService
{
    public Task<Response<object>> SendCredentialsEmailAsync(string toEmail, string userName, string password, List<(string redirectUrl, string appName)>? applications = null, bool showPassword = true);
    public Task SendResetPasswordEmailAsync(string to, string userName, string resetLink);
    public Task SendEmailConfirmationAsync(string to, string userName, string confirmationLink);
    public Task SendNewDeviceLoginAsync(string to, string userName, string ip, string userAgent, string dateTime, string applicationName);
    public Task<object> TestSmtpConnectionAsync();
}
