using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ChatApp.Server.Services
{
    public class MemoryDataAndEmailService
    {
        public Dictionary<string, int> ResetAccounts { get; set; } = new();
        public Dictionary<string, Timer> TimerReset { get; set; } = new();
        public Dictionary<string, Timer> OnlineTrack { get; set; } = new();

        public async Task SendEmailAsync(string username, string mail)
        {
            var code = new Random().Next(100000, 999999);
            var login = new NetworkCredential() { UserName = "bobleechatroom@gmail.com", Password = "Bob@2002" };
            var sender = new SmtpSender(() => new SmtpClient()
            {
                Host = "smtp.gmail.com",
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Port = 587,
                UseDefaultCredentials = false,
                Credentials = login
            });

            StringBuilder template = new();
            template.AppendLine("Dear @Model.FirstName,");
            template.AppendLine("<p>Welcome to @Model.SocialMedia. @Model.Code is your verification code, the code will be expired after 5 minutes.</p>");
            template.AppendLine("- Bob Lee");

            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();

            await Email
            .From("bobleechatroom@gmail.com")
            .To(mail, username)
            .Subject("Verification Code")
            .UsingTemplate(template.ToString(), new { FirstName = username, SocialMedia = "Bob Lee's Chatroom", Code = code.ToString() })
            .SendAsync();
            ResetAccounts.Add(username, code);
            Timer timer = new(300000);
            timer.Elapsed += (s, e) =>
            {
                ResetAccounts.Remove(username);
                timer.Enabled = false;
                timer.Close();
                TimerReset.Remove(username);
            };
            timer.Enabled = true;
            TimerReset[username] = timer;
        }
    }
}
