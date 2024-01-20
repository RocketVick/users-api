using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Database;
using TNG.Users.API.Extensions;

namespace TNG.Users.API.Features.User;

public class EditUser
{
    public class Command(int userId, string? firstName, string? lastName, DateTime? birthdate) : IRequest<Result>
    {
        public int UserId { get; } = userId;
        public string? FirstName { get; } = firstName;
        public string? LastName { get; } = lastName;
        public DateTime? Birthdate { get; } = birthdate;
    }

    internal sealed class Handler(UserDbContext context, IHttpContextAccessor httpContextAccessor) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext?.GetUserId();
            if (userId != command.UserId) return Result.Forbidden();
            
            var user = await context.Users.FirstOrDefaultAsync((u) => u.Id == command.UserId, cancellationToken);
            if (user == null) return Result.NotFound("User not found");

            user.FirstName = command.FirstName ?? user.FirstName;
            user.LastName = command.LastName ?? user.LastName;
            user.Birthdate = command.Birthdate ?? user.Birthdate;

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}

public class EditUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/user",
                async (ISender sender, EditUser.Command command) => (await sender.Send(command)).ToMinimalApiResult()).WithTags("User")
            .RequireAuthorization();
    }
}