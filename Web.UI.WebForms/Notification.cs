using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Text;
using ParadimeWeb.WorkflowGen.Web.UI.WebForms.Model;

namespace ParadimeWeb.WorkflowGen.Web.UI.WebForms
{
    public class Notification : IDisposable
    {
        public class Message
        {
            public string Subject { get; set; }
            public string Body { get; set; }
        }

        private SmtpClient smtpClient;
        private MailMessage mailMessage;
        private bool enabled;

        public Notification()
        {
            smtpClient = new SmtpClient(ConfigurationManager.AppSettings["ApplicationSmtpServer"], Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationSmtpPort"]));
            mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(ConfigurationManager.AppSettings["EngineNotificationDefaultSender"], ConfigurationManager.AppSettings["EngineNotificationDefaultSenderName"]);
            mailMessage.BodyEncoding = Encoding.UTF8;
            mailMessage.SubjectEncoding = Encoding.UTF8;
            enabled = ConfigurationManager.AppSettings["EngineNotificationEnabled"] == "Y";
        }

        public void AddAttachment(Attachment attachment)
        {
            mailMessage.Attachments.Add(attachment);
        }
        public void ClearAttachments()
        {
            mailMessage.Attachments.Clear();
        }
        public void AddFromGroupCC(string name, int top = 100)
        {
            mailMessage.CC.AddFromGroup(name, top);
        }
        public void AddFromProcessParticipantCC(string name, int processId, int top = 100)
        {
            mailMessage.CC.AddFromProcessParticipant(name, processId, top);
        }
        public void AddCC(MailAddress mailAddress)
        {
            mailMessage.CC.Add(mailAddress);
        }
        public void ClearCC()
        {
            mailMessage.CC.Clear();
        }
        public void AddFromGroupBcc(string name, int top = 100)
        {
            mailMessage.Bcc.AddFromGroup(name, top);
        }
        public void AddFromProcessParticipantBcc(string name, int processId, int top = 100)
        {
            mailMessage.Bcc.AddFromProcessParticipant(name, processId, top);
        }
        public void AddBcc(MailAddress mailAddress)
        {
            mailMessage.Bcc.Add(mailAddress);
        }
        public void ClearBcc()
        {
            mailMessage.Bcc.Clear();
        }

        public void Send(User user, Dictionary<string, Message> messages, bool isBodyHtml = false)
        {
            var lang = user.Locale.Split('-')[0];
            Message message = messages.ContainsKey(user.Locale) ? messages[user.Locale] : messages.ContainsKey(lang) ? messages[lang] : messages.First().Value;
            mailMessage.To.Clear();
            mailMessage.To.Add(new MailAddress(user.Email, user.CommonName));
            send(message.Subject, message.Body, isBodyHtml);
        }

        public void SendToGroup(string name, string subject, string body, bool isBodyHtml = false)
        {
            mailMessage.To.Clear();
            mailMessage.To.AddFromGroup(name);
            send(subject, body, isBodyHtml);
        }
        public void SendToProcessParticipant(string name, int processId, string subject, string body, bool isBodyHtml = false)
        {
            mailMessage.To.Clear();
            mailMessage.To.AddFromProcessParticipant(name, processId);
            send(subject, body, isBodyHtml);
        }

        public void Send(IList<MailAddress> to, string subject, string body, bool isBodyHtml = false)
        {
            mailMessage.To.Clear();
            foreach (var mailAddress in to)
            {
                mailMessage.To.Add(mailAddress);
            }
            send(subject, body, isBodyHtml);
        }
        private void send(string subject, string body, bool isBodyHtml = false)
        {
            if (enabled && mailMessage.To.Count > 0)
            {
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = isBodyHtml;
                try
                {
                    smtpClient.Send(mailMessage);
                }
                catch (Exception err)
                {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(err);
                }
            }
        }

        public void Dispose()
        {
            if (mailMessage != null)
            {
                mailMessage.Dispose();
            }
            if (smtpClient != null)
            {
                smtpClient.Dispose();
            }
        }
    }
}
