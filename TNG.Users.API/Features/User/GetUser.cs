using System.Security.Claims;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Database;
using TNG.Users.API.Extensions;

namespace TNG.Users.API.Features.User;

public class GetUser
{
    public class Query(int id) : IRequest<Result<UserInfo>>
    {
        public int Id { get; } = id;
    }

    public class UserInfo
    {
        public required string Login { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? Birthdate { get; set; }
    }

    internal sealed class Handler(UserDbContext context, IHttpContextAccessor httpContextAccessor) : IRequestHandler<Query, Result<UserInfo>>
    {
        public async Task<Result<UserInfo>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext?.GetUserId();
            if (userId != request.Id) return Result.Forbidden();
            
            var user = await context.Users.Include(x => x.Activities).FirstOrDefaultAsync((u) => u.Id == request.Id, cancellationToken);
            return user == null ? Result.NotFound() : Result<UserInfo>.Success(new UserInfo
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Birthdate = user.Birthdate,
                Login = user.Login,
            });
        }
    }
}

public class GetUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/user/{id:int}",
                async (ISender sender, int id) => (await sender.Send(new GetUser.Query(id))).ToMinimalApiResult()).WithTags("User")
            .RequireAuthorization();
    }
}