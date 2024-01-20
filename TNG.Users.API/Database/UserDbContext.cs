using Microsoft.EntityFrameworkCore;
using TNG.Users.API.Domain;

namespace TNG.Users.API.Database;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }
    public DbSet<Activity> Activities { get; init; }
}
