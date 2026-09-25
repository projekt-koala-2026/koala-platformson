using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace koala.src.Modules.Account.Services
{
    public class EmailService
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

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            string baseUrl = _configuration["AppUrl"] ?? "https://localhost:8080";

            string resetLink = $"{baseUrl}/api/auth/account/reset-password-links/?token={Uri.EscapeDataString(resetToken)}";

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

            // 4. Send via your existing email service method (marked high priority)
            await SendEmailAsync(toEmail, "Password Reset", htmlMessage, isHighPriority: true);
        }

        public async Task SendRegisterEmailAsync(string toEmail, string registerToken)
        {
            string baseUrl = _configuration["AppUrl"] ?? "https://localhost:8080";

            string resetLink = $"{baseUrl}/api/auth/account/register-links/?token={Uri.EscapeDataString(registerToken)}";

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
                        <a href=""{resetLink}"" class=""button"">Register</a>
                        <p style=""margin-top: 20px; font-size: 13px; color: #666;"">Or copy and paste this link into your browser:<br><a href=""{resetLink}"">{resetLink}</a></p>
                        <div class=""footer"">
                            <p>&copy; 2026 University Engineering Project. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            // 4. Send via your existing email service method (marked high priority)
            await SendEmailAsync(toEmail, "Register Account", htmlMessage, isHighPriority: true);
        }
    }
}