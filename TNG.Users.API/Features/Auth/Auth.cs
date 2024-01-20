using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using BCrypt.Net;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Common.Services;
using TNG.Users.API.Database;

namespace TNG.Users.API.Features.Auth;

public class Auth
{
    public class Command(string login, string password) : IRequest<Result<string>>
    {
        public string Login { get; } = login.ToLower();
        public string Password { get; } = password;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor((c) => c.Login).NotEmpty();
            RuleFor((c) => c.Password).NotEmpty();
        }
    }

    internal sealed class Handler(UserDbContext context, ITokenProvider tokenProvider) : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Login == command.Login, cancellationToken);
            
            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(command.Password, user.Password, HashType.SHA512))
            {
                return Result.Error("Login or password is incorrect.");
            }

            var token = tokenProvider.Generate(user);

            return Result<string>.Success(token);
        }
    }
}

public class AuthEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth",
            async (Auth.Command command, ISender sender) =>
            {
                var r = await sender.Send(command);
                return r.ToMinimalApiResult();
            }).WithTags("Auth");
    }
}