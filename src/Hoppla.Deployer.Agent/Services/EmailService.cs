using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Hoppla.Deployer.Agent.Services
{
    interface IEmailService
    {
        void SendMail(string sender, string reciever, string subject, string body);
        string CreateBody(string test);
    }

    public class EmailService : IEmailService
    {
        string _smtp;

        public EmailService(string smtp)
        {
            _smtp = smtp;
        }

        public virtual void SendMail(string sender, string recievers, string subject, string body)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(sender);
            var reciversArray = recievers.Split(';');
            foreach (var reciver in reciversArray)
            {
                mail.To.Add(new MailAddress(reciver));
            }

            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = _smtp;
            mail.IsBodyHtml = true;
            mail.Subject = subject;
            mail.Body = body;
            client.Send(mail);
        }


        public string CreateBody(string test)
        {
            return test;
        }

    }

    public class EmailServiceFake : EmailService
    {
        string _smtp;

        public EmailServiceFake(string smtp) : base(smtp)
        {
            _smtp = smtp;
        }

        public override void SendMail(string sender, string reciever, string subject, string body)
        {
            Console.WriteLine("Sender: " + sender);
            Console.WriteLine("Reciever: " + reciever);
            Console.WriteLine("Subject: " + subject);
            Console.WriteLine("Body: < ... >");
            //Console.WriteLine("Body: " + body);
        }

    }
}
