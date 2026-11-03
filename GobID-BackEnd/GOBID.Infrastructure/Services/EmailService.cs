using Azure.Identity;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using GOBID.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using System.Text;

namespace GOBID.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly GraphMailSettings _graphSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly GraphServiceClient _graphClient;

    public EmailService(GraphMailSettings graphSettings, ILogger<EmailService> logger)
    {
        _graphSettings = graphSettings;
        _logger = logger;
        _graphClient = new GraphServiceClient(
            new ClientSecretCredential(
                _graphSettings.TenantId,
                _graphSettings.ClientId,
                _graphSettings.ClientSecret));
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            _logger.LogInformation("Intentando enviar email a {ToEmail} con asunto: {Subject}", toEmail, subject);

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = body
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = toEmail } }
                }
            };

            await _graphClient.Users[_graphSettings.FromEmail].SendMail.PostAsync(
                new SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = false
                });

            _logger.LogInformation("Email enviado exitosamente a {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {ToEmail}: {Message}", toEmail, ex.Message);
            throw;
        }
    }

    private async Task SendEmailWithTemplateAsync(
        string toEmail,
        string subject,
        string templateName,
        object model
    )
    {
        var templatePath = Path.Join(
            AppContext.BaseDirectory,
            "Templates",
            "Emails",
            $"{templateName}.html"
        );

        if (!File.Exists(templatePath))
            throw new InvalidOperationException("Email template not found.");

        var template = await File.ReadAllTextAsync(templatePath);
        var body = ReplaceTemplatePlaceholders(template, model);

        await SendEmailAsync(toEmail, subject, body);
    }

    private string ReplaceTemplatePlaceholders(string template, object model)
    {
        var placeholders = model.GetType().GetProperties();
        foreach (var placeholder in placeholders)
        {
            var placeholderValue = placeholder.GetValue(model)?.ToString();
            template = template.Replace($"{{{{{placeholder.Name}}}}}", placeholderValue);
        }

        return template;
    }

    public async Task<Response<object>> SendCredentialsEmailAsync(string toEmail, string userName, string password, List<(string redirectUrl, string appName)>? applications = null, bool showPassword = true)
    {
        try
        {
            var subject = "Tus credenciales de acceso a GOBID";

            // Construir la fila de contraseña solo si showPassword es true
            var passwordRow = string.Empty;
            if (showPassword)
            {
                passwordRow = $@"
                                <tr>
                                    <td style=""width:35%; font-weight:bold; background:#f0f4fa;
                                               border:1px solid #e5e5e5; padding:12px; font-size:15px;"">
                                        Contraseña
                                    </td>
                                    <td style=""border:1px solid #e5e5e5; padding:12px; font-size:15px;"">
                                        {password}
                                    </td>
                                </tr>";
            }

            var applicationsHtml = string.Empty;

            if (applications != null && applications.Any())
            {
                var sb = new StringBuilder();

                sb.AppendLine(@"
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:20px;"">
    <tr>
        <td style=""font-size:18px; font-weight:bold; color:#003876; padding-bottom:10px;"">
            Aplicaciones con acceso:
        </td>
    </tr>");

                foreach (var app in applications)
                {
                    sb.AppendLine($@"
    <tr>
        <td style=""padding:6px 0;"">
            <span style=""font-size:15px; font-weight:bold; color:#333;"">{app.appName}:</span>
            <a href=""{app.redirectUrl}"" style=""font-size:15px; color:#003876; text-decoration:none;"">
                {app.redirectUrl}
            </a>
        </td>
    </tr>");
                }

                sb.AppendLine("</table>");

                applicationsHtml = sb.ToString();
            }

            var model = new
            {
                UserName = userName,
                PasswordRow = passwordRow,
                Applications = applicationsHtml
            };

            await SendEmailWithTemplateAsync(toEmail, subject, "SendPassword", model);

            return new Response<object>(null, "Correo enviado exitosamente.");
        }
        catch (Exception ex)
        {
            return new Response<object>($"Error inesperado al enviar el email: {ex.Message}", false);
        }
    }
    public async Task SendResetPasswordEmailAsync(string to, string userName, string resetLink)
    {
        var subject = "Restablecer contraseña";
        var model = new
        {
            UserName = userName,
            ResetLink = resetLink
        };

        await SendEmailWithTemplateAsync(
            toEmail: to,
            subject: subject,
            templateName: "ResetPassword",
            model: model
        );
    }

    public async Task SendEmailConfirmationAsync(string to, string userName, string confirmationLink)
    {
        var subject = "Confirma tu correo electrónico en GOBID";
        var model = new
        {
            UserName = userName,
            ConfirmationLink = confirmationLink
        };

        await SendEmailWithTemplateAsync(
            toEmail: to,
            subject: subject,
            templateName: "ConfirmEmail",
            model: model
        );
    }

    public async Task SendNewDeviceLoginAsync(string to, string userName, string ip, string userAgent, string dateTime, string applicationName)
    {
        var subject = "Nuevo inicio de sesión detectado en GOBID";
        var model = new
        {
            UserName = userName,
            ApplicationName = applicationName,
            Ip = ip,
            UserAgent = userAgent,
            DateTime = dateTime
        };

        await SendEmailWithTemplateAsync(
            toEmail: to,
            subject: subject,
            templateName: "NewDevice",
            model: model
        );
    }

    public async Task<object> TestSmtpConnectionAsync()
    {
        var settings = new
        {
            tenantId = _graphSettings.TenantId,
            clientId = _graphSettings.ClientId,
            fromEmail = _graphSettings.FromEmail,
            fromName = _graphSettings.FromName,
            secretConfigured = !string.IsNullOrEmpty(_graphSettings.ClientSecret)
        };

        try
        {
            var message = new Message
            {
                Subject = "GOBID - Test de envío de correo",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = "<p>Este es un correo de prueba generado automáticamente por GOBID para verificar la configuración de Graph API.</p>"
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = _graphSettings.FromEmail } }
                }
            };

            await _graphClient.Users[_graphSettings.FromEmail].SendMail.PostAsync(
                new SendMailPostRequestBody { Message = message, SaveToSentItems = false });

            return new { success = true, settings, message = "Mail.Send funciona correctamente. Correo de prueba enviado a " + _graphSettings.FromEmail };
        }
        catch (Exception ex)
        {
            return new
            {
                success = false,
                settings,
                errorType = ex.GetType().FullName,
                errorMessage = ex.Message,
                innerError = ex.InnerException?.Message
            };
        }
    }

}
