using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;

namespace NotificationService
{
    public class SMTPEmailSender : IEmailSender
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;

        public SMTPEmailSender(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string fromEmail)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUser = smtpUser;
            _smtpPass = smtpPass;
            _fromEmail = fromEmail;
        }

        public async Task SendAsync(IEnumerable<string> recipients, string subject, string body)
        {
            if (recipients == null || !recipients.Any())
            {
                throw new ArgumentException("Lista primalaca ne mo�e biti prazna.");
            }

            try
            {
                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                    client.EnableSsl = true;
                    client.Timeout = 30000; // 30 sekundi timeout

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false
                    };

                    // Dodaj sve primaoce
                    foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
                    {
                        mailMessage.To.Add(recipient);
                    }

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Greška prilikom slanja emaila: {ex.Message}", ex);
            }
        }

        public async Task SendAsync(string recipient, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(recipient))
            {
                throw new ArgumentException("Primalac ne mo�e biti prazan.");
            }

            await SendAsync(new[] { recipient }, subject, body);
        }
    }
}










