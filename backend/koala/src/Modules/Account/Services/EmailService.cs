using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Hangfire;
using koala.src.Shared.Core;
using koala.src.Shared;
using System.Security.Claims;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net;

namespace koala.src.Modules.Account.Services
{
    public interface IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage, string textMessage, bool isHighPriority = false);
        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string accountArea);
        public Task SendRegisterEmailAsync(string toEmail, string registerToken);
        public Task SendBulkEmailToSchoolsAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto);
        public Task SendBulkEmailToCaptainsAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto);
        public Task SendBulkEmailToGuardiansAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto);
    }
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ICoreModule _coreModule;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly AccountDbContext _db;
        private readonly byte[] _headerImageBytes;
        private readonly string _emailSender;
        private readonly string _smtpHost;
        private readonly int _smtpPort;         
        private readonly string _smtpUser;          
        private readonly string _smtpPass; 
        private readonly string _frontEndUrl;
        private readonly string _htmlHeader;
        private readonly string _htmlFooter;

        public static string FormatPlainTextAsHtmlBody(string plainText, string? title = null)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(title))
            {
                var encodedTitle = WebUtility.HtmlEncode(title.Trim());
                sb.Append($@"
                    <h1 style=""margin:0 0 16px; font-size:22px; line-height:1.3; color:#0f172a;"">
                        {encodedTitle}
                    </h1>");
            }

            var normalized = (plainText ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();

            var paragraphs = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < paragraphs.Length; i++)
            {
                var encodedParagraph = WebUtility.HtmlEncode(paragraphs[i].Trim())
                    .Replace("\n", "<br>");

                var marginBottom = i == paragraphs.Length - 1 ? "0" : "14px";

                sb.Append($@"
                    <p style=""margin:0 0 {marginBottom}; font-size:15px; line-height:1.65; color:#334155;"">
                        {encodedParagraph}
                    </p>");
            }

            return sb.ToString();
        }
        public EmailService(IConfiguration configuration, ICoreModule coreModule, IBackgroundJobClient backgroundJobClient, AccountDbContext db)
        {
            _configuration = configuration;
            _coreModule = coreModule;
            _backgroundJobClient = backgroundJobClient;
            _db = db;

            var headerImagePath = Path.Combine(
                AppContext.BaseDirectory,
                "src",
                "Modules",
                "Account",
                "Assets",
                "header_image.png"
            );

            if (!File.Exists(headerImagePath))
                throw new KoalaException(KoalaErrorCodes.FileNotFound ,$"Email header image {headerImagePath} not found.");

            _headerImageBytes = File.ReadAllBytes(headerImagePath);

            _frontEndUrl = _configuration["Urls:Frontend"] ?? "http://localhost:5173";
            _emailSender = _configuration["EmailSettings:SenderEmail"] ?? "noreply-koala@project.local";
            _smtpUser = _configuration["EmailSettings:SmtpUser"] ?? "localUser";
            _smtpPass = _configuration["EmailSettings:SmtpPass"] ?? "localPass";
            _smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "localhost";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "1025");

            _htmlHeader = @"
                <!DOCTYPE html>
                <html lang=""pl"">
                <head>
                    <meta charset=""utf-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Konkurs Koala</title>
                </head>
                <body style=""margin:0; padding:0; background-color:#f1f5f9; font-family:Arial,Helvetica,sans-serif; color:#0f172a;"">
                    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f1f5f9; padding:32px 12px;"">
                        <tr>
                            <td align=""center"">
                                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0""
                                    style=""max-width:600px; background-color:#ffffff; border-radius:16px; overflow:hidden;"">
                                    <tr>
                                        <td style=""padding:24px 32px; background-color:#ecfdf5; border-bottom:1px solid #d1fae5;"">
                                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                                <tr>
                                                    <td valign=""middle"" style=""padding-right:16px;"">
                                                        <div style=""font-size:24px; font-weight:bold; letter-spacing:1px; color:#020617;"">
                                                            KOALA
                                                        </div>
                                                        <div style=""margin-top:5px; font-size:12px; font-weight:bold; color:#047857;"">
                                                            Kombinatoryka · Algorytmika · Logika
                                                        </div>
                                                    </td>
                                                    <td align=""right"" valign=""middle"" width=""64"" style=""width:64px;"">
                                                        <img src=""cid:header-image-png@proj-koala.projektstudencki.pl""
                                                             alt=""Koala""
                                                             width=""56""
                                                             height=""80""
                                                             style=""display:block; width:80px; height:80px; max-width:56px; border-radius:12px;"" />
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding:32px;"">";
            _htmlFooter = @"
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding:20px 32px; background-color:#f8fafc; text-align:center; color:#94a3b8; font-size:12px; line-height:1.5;"">
                                            <p style=""margin:0;"">&copy; Konkurs Koala. All rights reserved.</p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, string txtMessage, bool isHighPriority = false)
        {
            var message = new MimeMessage();
            
            message.From.Add(new MailboxAddress("Koala-No-Reply", _emailSender));
            message.To.Add(new MailboxAddress("Recipient", toEmail));
            message.Subject = subject;

            if (isHighPriority)
            {
                message.Priority = MessagePriority.Urgent;
                message.XPriority = XMessagePriority.Highest;
            }

            var builder = new BodyBuilder
            {
                HtmlBody = _htmlHeader+htmlMessage+_htmlFooter,
                TextBody = txtMessage
            };

            if (_headerImageBytes.Length > 0)
            {
                var image = builder.LinkedResources.Add("avatar.png", _headerImageBytes, ContentType.Parse("image/png"));
                image.ContentId = "header-image-png@proj-koala.projektstudencki.pl";
                image.ContentDisposition = new ContentDisposition(ContentDisposition.Inline)
                {
                    Size = _headerImageBytes.Length
                };
                image.IsAttachment = false;
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.SslOnConnect);

            if (!string.IsNullOrEmpty(_smtpUser) && !string.IsNullOrEmpty(_smtpPass))
            {
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string accountArea)
        {
            string resetLink = $"{_frontEndUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&area={Uri.EscapeDataString(accountArea)}";

            string htmlMessage = $@"
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
                </p>";

            string textMessage = $@"
                    Resetowanie hasła
                
                    Otrzymaliśmy prośbę o zmianę hasła do Twojego konta.
                    
                    Skopiuj ten link i wklej go do przeglądarki:
                    
                    {resetLink}
                    
                    Jeśli nie prosiłeś(-aś) o zmianę hasła, zignoruj tę wiadomość. Twoje obecne hasło pozostanie bez zmian.";

            BackgroundJob.Enqueue<EmailService>("critical", x => x.SendEmailAsync(toEmail, "Password Reset", htmlMessage, textMessage, true));
            await Task.CompletedTask;
        }

        public async Task SendRegisterEmailAsync(string toEmail, string registerToken)
        {

            string registerLink = $"{_frontEndUrl}/register?token={Uri.EscapeDataString(registerToken)}";
            string htmlMessage = $@"
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
                </p>";
            string textMessage = $@"
                Dołącz do Konkursu Koala!

                Otrzymaliśmy prośbę o rejestrację konta powiązanego z tym adresem e-mail.

                Utwórz konto, jeśli chcesz odczuć radość i satysfakcję z rozwiązywania zadań matematycznych,
                szczególnie tych rozwijających logiczne myślenie, algorytmikę i umiejętność rozwiązywania problemów.
                Zignoruj tę wiadomość, jeśli nie jesteś zainteresowany(-a) tak ciekawymi wyzwaniami 
                lub po prostu nie prosiłeś(-aś) o rejestrację tego konta.
                
                Skopiuj ten link i wklej go do przeglądarki:
                {registerLink}";

            BackgroundJob.Enqueue<EmailService>("critical", x => x.SendEmailAsync(toEmail, "Zaproszenie do wzięcia udziału w konkursie Koala", htmlMessage, textMessage, true));
            await Task.CompletedTask;
        }

        public async Task SendBulkEmailToSchoolsAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only admin can send emails");
            }

            _backgroundJobClient.Enqueue<EmailService>(processor => processor.SchoolEmailSendBulkJobAsync(80, 0, requestDto.MessageSubject, FormatPlainTextAsHtmlBody(requestDto.MessageBody, requestDto.MessageSubject), requestDto.MessageBody, false));
            await Task.CompletedTask;
        }

        public async Task SchoolEmailSendBulkJobAsync(int pageSize, int pageNumber, string subject, string htmlMessage, string textMessage,  bool isHighPriority)
        {
            (var response, var pagination) = await _coreModule.Internal_GetSchoolsEmailsAsync(new PageQueryDto(pageNumber, pageSize));

            if (response == null || response.Count == 0 || !response.Any())
            {
                return;
            }

            string queueName = isHighPriority ? "critical" : "default";

            foreach (var schoolEmail in response)
            {
                _backgroundJobClient.Enqueue<IEmailService>(
                    queueName,
                    service => service.SendEmailAsync(schoolEmail, subject, htmlMessage, textMessage, isHighPriority)
                );
            }

            if (pagination.PageNumber * pagination.PageSize + response.Count < pagination.TotalNumber)
            {
                _backgroundJobClient.Enqueue<EmailService>(
                    processor => processor.SchoolEmailSendBulkJobAsync(pageSize, pageNumber + 1, subject, htmlMessage, textMessage, isHighPriority)
                );
            }
        }

        public async Task SendBulkEmailToCaptainsAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only admin can send emails");
            }

            _backgroundJobClient.Enqueue<EmailService>(processor => processor.CaptainsEmailSendBulkJobAsync(80, 0, requestDto.MessageSubject, FormatPlainTextAsHtmlBody(requestDto.MessageBody, requestDto.MessageSubject), requestDto.MessageBody, false));
            await Task.CompletedTask;
        }

        public async Task CaptainsEmailSendBulkJobAsync(int pageSize, int pageNumber, string subject, string htmlMessage, string textMessage,  bool isHighPriority)
        {
            var response = await _db.Users
                .Where(u => u.Verified && u.UserRoles.Any(ur => ur.Role.Name == RoleNames.NAME_TEAM_CAPTAIN))
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .Select(u => u.Email)
                .ToListAsync();

            if (response == null || response.Count == 0 || !response.Any())
            {
                return;
            }

            string queueName = isHighPriority ? "critical" : "default";

            foreach (var userEmail in response)
            {
                _backgroundJobClient.Enqueue<IEmailService>(
                    queueName,
                    service => service.SendEmailAsync(userEmail, subject, htmlMessage, textMessage, isHighPriority)
                );
            }

            if (pageNumber * pageSize + response.Count < await _db.Users.CountAsync(u => u.Verified && u.UserRoles.Any(ur => ur.Role.Name == RoleNames.NAME_TEAM_CAPTAIN)))
            {
                _backgroundJobClient.Enqueue<EmailService>(
                    processor => processor.CaptainsEmailSendBulkJobAsync(pageSize, pageNumber + 1, subject, htmlMessage, textMessage, isHighPriority)
                );
            }
        }

        public async Task SendBulkEmailToGuardiansAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only admin can send emails");
            }

            _backgroundJobClient.Enqueue<EmailService>(processor => processor.GuardiansEmailSendBulkJobAsync(80, 0, requestDto.MessageSubject, FormatPlainTextAsHtmlBody(requestDto.MessageBody, requestDto.MessageSubject), requestDto.MessageBody, false));
            await Task.CompletedTask;
        }

        public async Task GuardiansEmailSendBulkJobAsync(int pageSize, int pageNumber, string subject, string htmlMessage, string textMessage, bool isHighPriority)
        {
            var response = await _db.Users
                .Where(u => u.Verified && u.UserRoles.Any(ur => ur.Role.Name == RoleNames.NAME_TEAM_GUARDIAN))
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .Select(u => u.Email)
                .ToListAsync();

            if (response == null || response.Count == 0 || !response.Any())
            {
                return;
            }

            string queueName = isHighPriority ? "critical" : "default";

            foreach (var userEmail in response)
            {
                _backgroundJobClient.Enqueue<IEmailService>(
                    queueName,
                    service => service.SendEmailAsync(userEmail, subject, htmlMessage, textMessage, isHighPriority)
                );
            }

            if (pageNumber * pageSize + response.Count < await _db.Users.CountAsync(u => u.Verified && u.UserRoles.Any(ur => ur.Role.Name == RoleNames.NAME_TEAM_GUARDIAN)))
            {
                _backgroundJobClient.Enqueue<EmailService>(
                    processor => processor.GuardiansEmailSendBulkJobAsync(pageSize, pageNumber + 1, subject, htmlMessage, textMessage, isHighPriority)
                );
            }
        }
    }
}