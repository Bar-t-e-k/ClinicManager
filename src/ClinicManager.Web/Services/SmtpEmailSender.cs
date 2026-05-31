using ClinicManager.Web.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ClinicManager.Web.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _smtp;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<UpcomingVisitsReportOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _smtp = options.Value.Smtp;
        _logger = logger;
    }

    public async Task SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        string attachmentPath,
        string attachmentFileName,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromDisplayName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = body };
        if (File.Exists(attachmentPath))
            builder.Attachments.Add(attachmentFileName, await File.ReadAllBytesAsync(attachmentPath, cancellationToken));

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocket = _smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

        await client.ConnectAsync(_smtp.Host, _smtp.Port, secureSocket, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtp.Username))
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Wysłano e-mail SMTP do {To} z załącznikiem {File}", toEmail, attachmentFileName);
    }
}
