using MailKit.Security;
using Microsoft.Extensions.Options;

namespace Aprovizionare.Api.Services;

public class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(
            settings.SmtpHost!,
            settings.SmtpPort,
            settings.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);

        if (!string.IsNullOrEmpty(settings.SmtpUser))
            await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword ?? "", cancellationToken);

        await client.SendAsync(settings.ToMime(message), cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
