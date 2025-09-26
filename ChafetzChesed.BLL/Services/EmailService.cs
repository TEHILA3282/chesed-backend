using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using ChafetzChesed.BLL.Interfaces;

namespace ChafetzChesed.BLL.Services
{
    public class EmailService: IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendAsync(string to, string subject, string html)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_config["Smtp:SenderName"], _config["Smtp:SenderEmail"]));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;

            var body = new TextPart("html")
            {
                Text = $@"
            <html>
              <body dir='rtl' style='font-family: Arial, sans-serif; text-align: right;'>
                {html}
              </body>
            </html>"
            };

            msg.Body = body;

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["Smtp:Server"], int.Parse(_config["Smtp:Port"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["Smtp:Username"], _config["Smtp:Password"]);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }

    }
}
