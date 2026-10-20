using MimeKit;

namespace Aprovizionare.Api.Services;

public record EmailMessage(IReadOnlyList<string> To, string Subject, string Body);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public class EmailOptions
{
    public string From { get; set; } = "aprovizionare@firma.example.com";
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 25;
    public bool SmtpUseSsl { get; set; }
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string PickupDirectory { get; set; } = "emails";

    public MimeMessage ToMime(EmailMessage message)
    {
        var mime = new MimeMessage { Subject = message.Subject, Body = new TextPart("plain") { Text = message.Body } };
        mime.From.Add(MailboxAddress.Parse(From));
        foreach (var to in message.To)
            mime.To.Add(MailboxAddress.Parse(to));
        return mime;
    }
}
