using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

public class Email
{
	public static void SendEmail(string subject, string body, string attachment = null)
	{
		var fromAddress = new MailAddress("StructuredXmlEditor@gmail.com", "StructuredXmlEditor");
		var toAddress = new MailAddress("lyeeedar@gmail.com", "Philip Collin");
		const string fromPassword = "EditorEmailAccount";

		var smtp = new SmtpClient
		{
			Host = "smtp.gmail.com",
			Port = 587,
			EnableSsl = true,
			DeliveryMethod = SmtpDeliveryMethod.Network,
			UseDefaultCredentials = false,
			Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
		};
		using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
		{
			if (attachment != null)
			{
				message.Attachments.Add(Attachment.CreateAttachmentFromString(attachment, "Attachment.txt"));
			}

			smtp.Send(message);
		}
	}
}