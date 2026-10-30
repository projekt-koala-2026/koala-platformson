using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Hangfire;
using koala.src.Shared.Core;
using koala.src.Shared;
using System.Security.Claims;
using koala.src.Modules.Account.Dtos;

namespace koala.src.Modules.Account.Services
{
    public interface IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage, bool isHighPriority = false);
        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string accountArea);
        public Task SendRegisterEmailAsync(string toEmail, string registerToken);
        public Task SendBulkEmailToSchoolsAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto);
    }
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ICoreModule _coreModule;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public EmailService(IConfiguration configuration, ICoreModule coreModule, IBackgroundJobClient backgroundJobClient)
        {
            _configuration = configuration;
            _coreModule = coreModule;
            _backgroundJobClient = backgroundJobClient;
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
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f7; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
                        .button {{ display: inline-block; background-color: #007bff; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 4px; font-weight: bold; margin-top: 20px; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #888; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <h2>Password Reset Request</h2>
                        <p>We received a request to reset your password. If you didn't make this request, you can safely ignore this email.</p>
                        <p>To reset your password, click the button below:</p>
                        <a href=""{resetLink}"" class=""button"">Reset Password</a>
                        <p style=""margin-top: 20px; font-size: 13px; color: #666;"">Or copy and paste this link into your browser:<br><a href=""{resetLink}"">{resetLink}</a></p>
                        <div class=""footer"">
                            <p>&copy; 2026 University Engineering Project. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            BackgroundJob.Enqueue<EmailService>("critical", x => x.SendEmailAsync(toEmail, "Password Reset", htmlMessage, true));
            await Task.CompletedTask;
        }

        public async Task SendRegisterEmailAsync(string toEmail, string registerToken)
        {
            string frontendUrl = (_configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');

            string registerLink = $"{frontendUrl}/register?token={Uri.EscapeDataString(registerToken)}";

            // 3. Professional, responsive HTML email template
            string htmlMessage = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f7; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
                        .button {{ display: inline-block; background-color: #007bff; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 4px; font-weight: bold; margin-top: 20px; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #888; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <h2>Register Request</h2>
                        <p>We received a request to register an account for this email. If you didn't make this request, you can safely ignore this email.</p>
                        <p>To register, click the button below:</p>
                        <a href=""{registerLink}"" class=""button"">Register</a>
                        <p style=""margin-top: 20px; font-size: 13px; color: #666;"">Or copy and paste this link into your browser:<br><a href=""{registerLink}"">{registerLink}</a></p>
                        <div class=""footer"">
                            <p>&copy; 2026 University Engineering Project. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            BackgroundJob.Enqueue<EmailService>("critical", x => x.SendEmailAsync(toEmail, "Register Account", htmlMessage, true));
            await Task.CompletedTask;
        }

        public async Task SendBulkEmailToSchoolsAsync(ClaimsPrincipal? claimsPrincipal, EmailSendRequestDto requestDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only admin can send emails");
            }

            string htmlMessage = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f7; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
                        .button {{ display: inline-block; background-color: #007bff; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 4px; font-weight: bold; margin-top: 20px; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #888; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <h2>Register Request</h2>
                        <p>We received a request to register an account for this email. If you didn't make this request, you can safely ignore this email.</p>
                        <p>To register, click the button below:</p>
                        <a href=""{requestDto.MessageBody}"" class=""button"">Register</a>
                        <p style=""margin-top: 20px; font-size: 13px; color: #666;"">Or copy and paste this link into your browser:<br><a href=""{requestDto.MessageBody}"">{requestDto.MessageBody}</a></p>
                        <div class=""footer"">
                            <p>&copy; 2026 University Engineering Project. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";
            _backgroundJobClient.Enqueue<EmailService>(processor => processor.SchoolEmailSendBulkJobAsync(80, 0, requestDto.MessageSubject, htmlMessage, false));
            await Task.CompletedTask;
        }

        public async Task SchoolEmailSendBulkJobAsync(int pageSize, int pageNumber, string subject, string htmlMessage, bool isHighPriority)
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
                    service => service.SendEmailAsync(schoolEmail, subject, htmlMessage, isHighPriority)
                );
            }

            if (pagination.PageNumber * pagination.PageSize + response.Count < pagination.TotalNumber)
            {
                _backgroundJobClient.Enqueue<EmailService>(
                    processor => processor.SchoolEmailSendBulkJobAsync(pageSize, pageNumber + 1, subject, htmlMessage, isHighPriority)
                );
            }
        }

        public async Task SendBulkEmailToTeamCaptainsAsync(string body)
        {

        }

        public async Task SendBulkEmailToTeamGuardiansAsync(string body)
        {

        }
    }
}