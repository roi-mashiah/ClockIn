using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Forms;

namespace ClockIn.Extensions
{
    public class MailHelper
    {
        public static MailMessage GetMailMessage()
        {
            MailMessage newMail = new MailMessage();
            newMail.From = new MailAddress(ConfigurationManager.AppSettings["FromMail"]);
            newMail.To.Add(new MailAddress(ConfigurationManager.AppSettings["ToMail"]));
            newMail.Subject = ConfigurationManager.AppSettings["Subject"];
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ReportPath"]))
            {
                newMail.Attachments.Add(new Attachment(ConfigurationManager.AppSettings["ReportPath"]));
            }
            string emailFooter = "תודה מראש, רועי משיח" + "\n" + $"Sent from {System.Reflection.Assembly.GetExecutingAssembly().GetName().Name} on {DateTime.Now}";
            newMail.Body = ConfigurationManager.AppSettings["Body"] + "\n" + emailFooter;
            return newMail;
        }
        public static void SendEmail(NetworkCredential credentials, MailMessage message)
        {
            using (var client = new SmtpClient("smtp.gmail.com"))
            {
                client.Port = 587;
                client.Credentials = credentials;
                client.EnableSsl = true;
                client.Send(message);
            }
        }
    }
}
