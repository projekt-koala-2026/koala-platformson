using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace koala.src.Modules.Account.Services
{
    public interface IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage, bool isHighPriority = false);
        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string accountArea);
        public Task SendRegisterEmailAsync(string toEmail, string registerToken);
    }
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, bool isHighPriority = false)
        {
            var message = new MimeMessage();

            // Looks for EmailSettings__SenderEmail environment variable, falls back to default
            string senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@project.local";
            message.From.Add(new MailboxAddress("No-Reply", senderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            if (isHighPriority)
            {
                message.Priority = MessagePriority.Urgent;
                message.XPriority = XMessagePriority.Highest;
            }

            message.Body = new TextPart("html") { Text = htmlMessage };

            using var client = new SmtpClient();

            string host = _configuration["EmailSettings:SmtpHost"] ?? "localhost";
            int port = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "1025");

            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.None);

            string? user = _configuration["EmailSettings:SmtpUser"];
            string? pass = _configuration["EmailSettings:SmtpPass"];

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                await client.AuthenticateAsync(user, pass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string accountArea)
        {
            string frontendUrl = (_configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
            string resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&area={Uri.EscapeDataString(accountArea)}";

            // Professional, responsive HTML email template
            string htmlMessage = $@"
                <!DOCTYPE html>
                <html lang=""pl"">
                <head>
                    <meta charset=""utf-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Resetowanie hasła</title>
                </head>
                <body style=""margin:0; padding:0; background-color:#f1f5f9; font-family:Arial,Helvetica,sans-serif; color:#0f172a;"">
                    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f1f5f9; padding:32px 12px;"">
                        <tr>
                            <td align=""center"">
                                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0""
                                    style=""max-width:600px; background-color:#ffffff; border-radius:16px; overflow:hidden;"">
                                    <tr>
                                        <td style=""padding:28px 32px; background-color:#ecfdf5; border-bottom:1px solid #d1fae5;"">
                                            <div style=""font-size:24px; font-weight:bold; letter-spacing:1px; color:#020617;"">
                                                KOALA
                                            </div>
                                            <div style=""margin-top:5px; font-size:12px; font-weight:bold; color:#047857;"">
                                                Kombinatoryka · Algorytmika · Logika
                                            </div>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding:32px;"">
                                            <h1 style=""margin:0 0 16px; font-size:24px; line-height:1.3; color:#0f172a;"">
                                                Resetowanie hasła
                                            </h1>
                                            <p style=""margin:0 0 16px; font-size:16px; line-height:1.6; color:#334155;"">
                                                Otrzymaliśmy prośbę o zmianę hasła do Twojego konta.
                                            </p>
                                            <p style=""margin:0 0 24px; font-size:16px; line-height:1.6; color:#334155;"">
                                                Kliknij przycisk poniżej, aby ustawić nowe hasło:
                                            </p>
                                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"">
                                                <tr>
                                                    <td align=""center"" style=""border-radius:10px; background-color:#059669;"">
                                                        <a href=""{resetLink}""
                                                        style=""display:inline-block; padding:14px 24px; border:1px solid #059669; border-radius:10px; color:#ffffff; font-size:16px; font-weight:bold; text-decoration:none;"">
                                                            Zmień hasło
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>
                                            <p style=""margin:24px 0 8px; font-size:13px; line-height:1.5; color:#64748b;"">
                                                Jeśli przycisk nie działa, skopiuj ten link i wklej go do przeglądarki:
                                            </p>
                                            <p style=""margin:0; font-size:13px; line-height:1.5; word-break:break-all;"">
                                                <a href=""{resetLink}"" style=""color:#047857;"">{resetLink}</a>
                                            </p>
                                            <p style=""margin:24px 0 0; font-size:14px; line-height:1.6; color:#64748b;"">
                                                Jeśli nie prosiłeś(-aś) o zmianę hasła, zignoruj tę wiadomość. Twoje obecne hasło pozostanie bez zmian.
                                            </p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding:20px 32px; background-color:#f8fafc; text-align:center; color:#94a3b8; font-size:12px; line-height:1.5;"">
                                            <p>&copy; Konkurs Koala. All rights reserved.</p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            // Send via the existing email service with high priority.
            await SendEmailAsync(toEmail, "Resetuj hasło", htmlMessage, isHighPriority: true);
        }

        public async Task SendRegisterEmailAsync(string toEmail, string registerToken)
        {
            string frontendUrl = (_configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');

            string registerLink = $"{frontendUrl}/register?token={Uri.EscapeDataString(registerToken)}";

            // 3. Professional, responsive HTML email template
            string htmlMessage = $@"
                <!DOCTYPE html>
                <html lang=""pl"">
                <head>
                    <meta charset=""utf-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Zaproszenie do Konkursu Koala</title>
                </head>
        <body style=""margin:0; padding:0; background-color:#f1f5f9; font-family:Arial,Helvetica,sans-serif; color:#0f172a;"">
            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f1f5f9; padding:32px 12px;"">
                <tr>
                    <td align=""center"">
                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0""
                            style=""max-width:600px; background-color:#ffffff; border-radius:16px; overflow:hidden;"">
                            <tr>
                                <td style=""padding:28px 32px; background-color:#ecfdf5; border-bottom:1px solid #d1fae5;"">
                                    <div style=""font-size:24px; font-weight:bold; letter-spacing:1px; color:#020617;"">
                                        KOALA
                                    </div>
                                    <div style=""margin-top:5px; font-size:12px; font-weight:bold; color:#047857;"">
                                        Kombinatoryka · Algorytmika · Logika
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding:32px;"">
                                    <h1 style=""margin:0 0 16px; font-size:24px; line-height:1.3; color:#0f172a;"">
                                        Dołącz do Konkursu Koala!
                                    </h1>
                                    <p style=""margin:0 0 16px; font-size:16px; line-height:1.6; color:#334155;"">
                                        Otrzymaliśmy prośbę o rejestrację konta powiązanego z tym adresem e-mail.
                                    </p>
                                    <p style=""margin:0 0 24px; font-size:16px; line-height:1.6; color:#334155;"">
                                        Utwórz konto, jeśli chcesz odczuć radość i satysfakcję z rozwiązywania zadań matematycznych,
                                        szczególnie tych rozwijających logiczne myślenie, algorytmikę i umiejętność rozwiązywania problemów.
                                        Zignoruj tę wiadomość, jeśli nie jesteś zainteresowany(-a) tak ciekawymi wyzwaniami 
                                        lub po prostu nie prosiłeś(-aś) o rejestrację tego konta.
                                    </p>
                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"">
                                        <tr>
                                            <td align=""center"" style=""border-radius:10px; background-color:#059669;"">
                                                <a href=""{registerLink}""
                                                   style=""display:inline-block; padding:14px 24px; border:1px solid #059669; border-radius:10px; color:#ffffff; font-size:16px; font-weight:bold; text-decoration:none;"">
                                                    Zarejestruj się
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style=""margin:24px 0 8px; font-size:13px; line-height:1.5; color:#64748b;"">
                                        Jeśli przycisk nie działa, skopiuj ten link i wklej go do przeglądarki:
                                    </p>
                                    <p style=""margin:0; font-size:13px; line-height:1.5; word-break:break-all;"">
                                        <a href=""{registerLink}"" style=""color:#047857;"">{registerLink}</a>
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding:20px 32px; background-color:#f8fafc; text-align:center; color:#94a3b8; font-size:12px; line-height:1.5;"">
                                   <p>&copy; Konkurs Koala. All rights reserved.</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

            // 4. Send via your existing email service method (marked high priority)
            await SendEmailAsync(toEmail, "Zaproszenie do rejestracji w Konkursie Koala", htmlMessage, isHighPriority: true);
        }
    }
}
