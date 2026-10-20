using Microsoft.Extensions.Options;

namespace Aprovizionare.Api.Services;

public class FileEmailSender(IOptions<EmailOptions> options, IWebHostEnvironment environment, ILogger<FileEmailSender> logger)
    : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(environment.ContentRootPath, options.Value.PickupDirectory);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml");
        await options.Value.ToMime(message).WriteToAsync(path, cancellationToken);

        logger.LogInformation("Email salvat în {Path}: {Subject}", path, message.Subject);
    }
}
