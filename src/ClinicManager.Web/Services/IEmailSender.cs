namespace ClinicManager.Web.Services;

public interface IEmailSender
{
    Task SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        string attachmentPath,
        string attachmentFileName,
        CancellationToken cancellationToken = default);
}
