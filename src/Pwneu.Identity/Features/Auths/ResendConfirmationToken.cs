using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Pwneu.Identity.Shared.Entities;
using Pwneu.Shared.Common;
using Pwneu.Shared.Contracts;

namespace Pwneu.Identity.Features.Auths;

public static class ResendConfirmationToken
{
    public record Command(string Email) : IRequest<Result>;

    private static readonly Error UserNotFound = new("ResendConfirmationToken.NotFound",
        "User with the specified email was not found");

    private static readonly Error EmailAlreadyConfirmed = new("ResendConfirmationEmail.EmailAlreadyConfirmed",
        "Email is already confirmed.");

    private static readonly Error NoEmail = new("ResendConfirmationToken.NoEmail", "No Email specified");

    internal sealed class Handler(UserManager<User> userManager, IPublishEndpoint publishEndpoint)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null)
                return Result.Failure(UserNotFound);

            if (user.Email is null)
                return Result.Failure(NoEmail);

            if (user.EmailConfirmed)
                return Result.Failure(EmailAlreadyConfirmed);

            var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

            await publishEndpoint.Publish(new RegisteredEvent
            {
                Email = user.Email,
                ConfirmationToken = confirmationToken
            }, cancellationToken);

            return Result.Success();
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("resend", async (string email, ISender sender) =>
                {
                    var command = new Command(email);
                    var result = await sender.Send(command);

                    return result.IsFailure ? Results.BadRequest(result.Error) : Results.NoContent();
                })
                .WithTags(nameof(Auths));
        }
    }
}