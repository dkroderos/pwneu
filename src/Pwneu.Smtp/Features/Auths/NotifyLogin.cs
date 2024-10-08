using System.Net;
using System.Net.Mail;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;
using Pwneu.Shared.Common;
using Pwneu.Shared.Contracts;
using Pwneu.Smtp.Shared.Options;

namespace Pwneu.Smtp.Features.Auths;

public static class NotifyLogin
{
    public record Command(
        string FullName,
        string? Email,
        string? IpAddress = null,
        string? UserAgent = null,
        string? Referer = null) : IRequest<Result>;

    private static readonly Error NoEmail = new("NotifyLogin.NoEmail", "No Email specified");
    private static readonly Error Disabled = new("NotifyLogin.Disabled", "Notify login is disabled");

    internal sealed class Handler(IOptions<SmtpOptions> smtpOptions) : IRequestHandler<Command, Result>
    {
        private readonly SmtpOptions _smtpOptions = smtpOptions.Value;

        public Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (request.Email is null)
                return Task.FromResult(Result.Failure(NoEmail));

            if (_smtpOptions.NotifyLoginIsEnabled is false)
                return Task.FromResult(Result.Failure(Disabled));

            var smtpClient = new SmtpClient("smtp.gmail.com", Consts.GmailSmtpPort)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtpOptions.SenderAddress, _smtpOptions.SenderPassword)
            };

            using var mailMessage = new MailMessage(_smtpOptions.SenderAddress, request.Email);
            mailMessage.Subject = "Hello Pwneu!";
            mailMessage.Body = $"{request.FullName} || {request.IpAddress} || {request.UserAgent} || {request.Referer}";

            try
            {
                smtpClient.Send(mailMessage);
                return Task.FromResult(Result.Success());
            }
            catch (Exception e)
            {
                return Task.FromResult(Result.Failure(new Error("NotifyLogin.Failed", e.Message)));
            }
        }
    }
}

public class LoggedInEventConsumer(ISender sender, ILogger<LoggedInEventConsumer> logger)
    : IConsumer<LoggedInEvent>
{
    public async Task Consume(ConsumeContext<LoggedInEvent> context)
    {
        try
        {
            logger.LogInformation("Received logged in event message");

            var message = context.Message;
            var command = new NotifyLogin.Command(message.FullName, message.Email, message.IpAddress, message.UserAgent,
                message.Referer);
            var result = await sender.Send(command);

            if (result.IsSuccess)
            {
                logger.LogInformation("Sent login notification to {email}", context.Message.Email);
                return;
            }

            logger.LogError(
                "Failed to send login notification to {email}: {error}", message.Email, result.Error.Message);
        }
        catch (Exception e)
        {
            logger.LogError("{e}", e.Message);
        }
    }
}