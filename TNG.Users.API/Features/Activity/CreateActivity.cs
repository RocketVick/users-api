using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Carter;
using EasyNetQ;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Contracts;
using TNG.Users.API.Database;
using TNG.Users.API.Extensions;

namespace TNG.Users.API.Features.Activity;

public class CreateActivity
{
    public class Command : IRequest<Result<int>>
    {
        public Command(int userId, string description)
        {
            UserId = userId;
            Description = description;
        }

        public int UserId { get; }
        public string Description { get; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Description).NotEmpty();
        }
    }

    internal sealed class Handler(UserDbContext context, IHttpContextAccessor httpContextAccessor, IBus bus) : IRequestHandler<Command, Result<int>>
    {
        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext?.GetUserId();
            if (userId != request.UserId) return Result.Forbidden();
            
            if (!await context.Users.AnyAsync(x => x.Id == userId, cancellationToken)) return Result.Conflict("User not found");

            var entity = await context.Activities.AddAsync(new Domain.Activity() { UserId = request.UserId, Description = request.Description}, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await bus.PubSub.PublishAsync(new ActivityCreated{Id = entity.Entity.Id}, cancellationToken: cancellationToken);
            
            return Result.Success(entity.Entity.Id);
        }
    }
}

public class CreateActivityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/activity",
            async (CreateActivity.Command command, ISender sender) => (await sender.Send(command)).ToMinimalApiResult()).WithTags("Activity").RequireAuthorization();
    }
}