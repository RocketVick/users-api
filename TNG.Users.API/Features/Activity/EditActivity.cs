using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Database;
using TNG.Users.API.Extensions;

namespace TNG.Users.API.Features.Activity;

public class EditActivity
{
    public class Command : IRequest<Result>
    {
        public Command(string description, int activityId)
        {
            Description = description;
            ActivityId = activityId;
        }
        public int ActivityId { get; }
        public string Description { get; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Description).NotEmpty();
        }
    }

    internal sealed class Handler(UserDbContext context, IHttpContextAccessor httpContextAccessor) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var activity =
                await context.Activities.FirstOrDefaultAsync(x => x.Id == request.ActivityId, cancellationToken);
            if (activity == null) return Result.NotFound();
            
            var userId = httpContextAccessor.HttpContext?.GetUserId();
            if (activity.UserId != userId) return Result.Forbidden();

            activity.Description = request.Description;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}

public class EditActivityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/activity",
            async (EditActivity.Command command, ISender sender) => (await sender.Send(command)).ToMinimalApiResult()).WithTags("Activity").RequireAuthorization();
    }
}