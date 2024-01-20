using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Database;
using TNG.Users.API.Extensions;

namespace TNG.Users.API.Features.Activity;

public class DeleteActivity
{
    public class Command(int id) : IRequest<Result<int>>
    {
        public int Id { get; } = id;
    }

    internal sealed class Handler(UserDbContext context, IHttpContextAccessor httpContextAccessor) : IRequestHandler<Command, Result<int>>
    {
        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            var activity = await context.Activities.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (activity == null) return Result.Success(request.Id);

            var userId = httpContextAccessor.HttpContext?.GetUserId();
            if (userId != activity.UserId) return Result<int>.Forbidden();

            context.Remove(activity);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(request.Id);
        }
    }
}

public class DeleteActivityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/activity/{id:int}",
            async (int id, ISender sender) => (await sender.Send(new DeleteActivity.Command(id))).ToMinimalApiResult())
            .WithTags("Activity")
            .RequireAuthorization();
    }
}