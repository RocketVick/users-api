using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using BCrypt.Net;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Database;

namespace TNG.Users.API.Features.User;

public static class CreateUser
{
    public class Command(string login, string password) : IRequest<Result<int>>
    {
        public string Login { get; } = login.ToLower();
        public string Password { get; } = password;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor((c) => c.Login).NotEmpty().MaximumLength(20).WithMessage("Your Login length must not exceed 20.");
            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Your password cannot be empty")
                .MinimumLength(5).WithMessage("Your password length must be at least 5.")
                .MaximumLength(20).WithMessage("Your password length must not exceed 20.")
                .Matches(@"[A-Z]+").WithMessage("Your password must contain at least one uppercase letter.")
                .Matches(@"[a-z]+").WithMessage("Your password must contain at least one lowercase letter.")
                .Matches(@"[0-9]+").WithMessage("Your password must contain at least one number.")
                .Matches(@"[\!\?\*\.]+").WithMessage("Your password must contain at least one (!? *.).");
        }
    }

    internal sealed class Handler(UserDbContext context) : IRequestHandler<Command, Result<int>>
    {
        public async Task<Result<int>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (await context.Users.AsNoTracking().AnyAsync(x => x.Login == command.Login, cancellationToken))
                return Result.Conflict($"Login {command.Login} is already in use");
            
            var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(command.Password, HashType.SHA512);
            var user = new Domain.User
            {
                Login = command.Login,
                Password = hashedPassword
            };
            var entity = await context.Users.AddAsync(user, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(entity.Entity.Id);
        }
    }
}

public class AddUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/user",
            async (CreateUser.Command command, ISender sender) =>
            {
                var r = await sender.Send(command);
                return r.ToMinimalApiResult();
            }).WithTags("User");
    }
}