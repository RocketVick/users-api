using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Database;
using TNG.Users.API.Extensions;

namespace TNG.Users.API.Features.Activity;

public class GetActivities
{
    public class Query(int userId) : IRequest<Result<Response>>
    {
        public int UserId { get; } = userId;
    }

    public class Response(IEnumerable<ActivityResult> activities)
    {
        public IEnumerable<ActivityResult> Activities { get; } = activities;
    }

    public class ActivityResult(int id, string description)
    {
        public int Id { get; } = id;
        public string Description { get; } = description;
    }

    internal sealed class Handler(UserDbContext context, IHttpContextAccessor httpContextAccessor) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext?.GetUserId();
            if (userId != request.UserId) return Result.Forbidden();

            var activities = await context.Activities.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
            return Result.Success(new Response(activities.Select(x => new ActivityResult(x.Id, x.Description))));
        }
    }
}

public class GetActivitiesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/activity/{userId:int}",
            async (ISender sender, int userId) => (await sender.Send(new GetActivities.Query(userId)))
                .ToMinimalApiResult())
            .WithTags("Activity")
            .RequireAuthorization();
    }
}