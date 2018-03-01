using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using VirtualCampaign_Manager.Data;

namespace VirtualCampaign_Manager.Managers
{
    public static class EmailManager
    {
        private static string emailTemplate = @"<p>Dear #name#,</p><p>your film '#filmname#' is ready for download.</p><p>Your can also download it following this link:</p><p>#filmlinks#</p><p>Best regards,</p><p>  your Virtualcampaign team</p>";

        public static void NullMail()
        {
            MailAddress email;

            try
            {
                email = new MailAddress("abcdef");
            }
            catch (FormatException e)
            {
                return;
            }

            MailMessage mail = new MailMessage("service@virtualcampaign.de", email.Address);

            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "smtp.1und1.de";
            client.EnableSsl = false;
            client.Credentials = new NetworkCredential("service@virtualcampaign.de", "10Reasons2HateU!");
            mail.IsBodyHtml = true;
            //mail.Subject = "Your Film: " + production.Name + " - is ready for download";
            // mail.Body = GenerateEmailBody(production, emailTemplate);
            try
            {
                client.Send(mail);
            }
            catch
            {
            }
        }

        public static void SendMail(Production production)
        {
            MailAddress email;

            try
            {
                email = new MailAddress(production.Email);
            }
            catch (FormatException e)
            {
                return;
            }

            MailMessage mail = new MailMessage("service@virtualcampaign.de", email.Address);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "smtp.1und1.de";
            client.EnableSsl = false;
            client.Credentials = new NetworkCredential("service@virtualcampaign.de", "10Reasons2HateU!");
            mail.IsBodyHtml = true;
            mail.Subject = "Your Film: " + production.Name + " - is ready for download";
            mail.Body = GenerateEmailBody(production, emailTemplate);
            try
            {
                client.Send(mail);
            }
            catch
            {
            }
        }

        private static string GenerateEmailBody(Production production, string template)
        {
            string result = template;
            result = result.Replace("#name#", production.Username);
            result = result.Replace("#filmname#", production.Name);
            result = result.Replace("#filmlinks#", GenerateFilmLink(production));

            return result;
        }

        private static string GenerateFilmLink(Production production)
        {
            string result = "";
            result += "<p><a href='" + Settings.FilmUrl + @"/index.php?film=" + production.Film.UrlHash + "'>" + production.Name + "</a></p>";

            return result;
        }

        private static string GenerateFilmLinks(Production production)
        {
            string result = "";
            String[] buffer = new String[production.Film.CodecSizes.Count];

            for (int i = 0; i < production.Film.CodecSizes.Count; i++)
            {
                CodecInfo codecSize = production.Film.CodecSizes[i];
                string fileName = "film_" + production.Film.ID + "_" + codecSize.Codec.ID + codecSize.Codec.Extension;
                string targetDirectory = Path.Combine(new string[] { production.AccountID.ToString(), "productions", production.Film.ID.ToString() });
                string targetFile = Path.Combine(targetDirectory, fileName);
                string targetFileName = Path.Combine(production.Film.ID.ToString(), fileName);
                fileName = production.Name + "_" + codecSize.Size + "." + codecSize.Codec.Extension;
                result += "<p><a href='" + Path.Combine(new string[] { Settings.ServerPath, Settings.AccountDirectory, targetFile }) + "'>" + fileName + "</a></p>";
            }
            return result;
        }

        public static void SendErrorMail(Production production)
        {
            MailMessage mail = new MailMessage("service@virtualcampaign.de", "info@ibt-studios.de");
            mail.To.Add("gregor.wenzel@me.com");

            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "smtp.1und1.de";
            client.EnableSsl = false;
            client.Credentials = new NetworkCredential("service@virtualcampaign.de", "10Reasons2HateU!");
            mail.IsBodyHtml = false;
            mail.Subject = "Production " + production.Name + " (" + production.ID + ") has errors.";
            mail.Body = GenerateErrorBody(production);
            try
            {
                client.Send(mail);
            }
            catch
            {
            }
        }

        public static void SendErrorMail(Job job)
        {
            MailMessage mail = new MailMessage("service@virtualcampaign.de", "info@ibt-studios.de");
            mail.To.Add("gregor.wenzel@me.com");

            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "smtp.1und1.de";
            client.EnableSsl = false;
            client.Credentials = new NetworkCredential("service@virtualcampaign.de", "10Reasons2HateU!");
            mail.IsBodyHtml = false;
            mail.Subject = "Job " + job.ID + " has errors.";
            mail.Body = GenerateErrorBody(job);
            try
            {
                client.Send(mail);
            }
            catch
            {
            }
        }

        public static void SendPanicMail()
        {
            SendPanicMail(new Exception());
        }

        public static void SendPanicMail(Exception e)
        {
            MailMessage mail = new MailMessage("service@virtualcampaign.de", "info@ibt-studios.de");
            mail.To.Add("gregor.wenzel@me.com");

            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "smtp.1und1.de";
            client.EnableSsl = false;
            client.Credentials = new NetworkCredential("service@virtualcampaign.de", "10Reasons2HateU!");
            mail.IsBodyHtml = false;
            mail.Subject = "Unhandled Exception occured";
            mail.Body = GeneratePanicBody(e);
            try
            {
                client.Send(mail);
            }
            catch
            {
            }
        }

        private static string GenerateErrorBody(Production production)
        {
            DateTime dt = DateTime.Now;
            string result = "Production ID: " + production.ID + "\r\n";
            result += "Production Name: " + production.Name + "\r\n";
            result += "Error Code: " + production.ErrorCode + "\r\n";
            result += "User ID: " + production.AccountID + "\r\n";
            result += "User Name: " + production.Username + "\r\n";
            result += "Error Date: " + dt.ToLongDateString() + "\r\n";
            result += "Error Time: " + dt.ToLongTimeString() + "\r\n";

            return result;
        }

        private static string GeneratePanicBody(Exception e)
        {
            DateTime dt = DateTime.Now;
            string result = "";
            result += "Error Date: " + dt.ToLongDateString() + "\r\n";
            result += "Error Time: " + dt.ToLongTimeString() + "\r\n";
            result += e.ToString();

            return result;
        }

        private static string GenerateErrorBody(Job job)
        {
            Production production = job.Production;
            DateTime dt = DateTime.Now;
            string result = "Job ID: " + job.ID;
            result += "Product ID: " + job.ProductID;
            result += "Error Code: " + job.ErrorCode + "\r\n";
            result += "Production ID: " + production.ID + "\r\n";
            result += "Production Name: " + production.Name + "\r\n";
            result += "User ID: " + production.AccountID + "\r\n";
            result += "User Name: " + production.Username + "\r\n";
            result += "Error Date: " + dt.ToLongDateString() + "\r\n";
            result += "Error Time: " + dt.ToLongTimeString() + "\r\n";

            return result;
        }
    }
}
