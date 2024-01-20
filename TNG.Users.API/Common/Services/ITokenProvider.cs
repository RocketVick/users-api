using TNG.Users.API.Domain;

namespace TNG.Users.API.Common.Services;

public interface ITokenProvider
{
    string Generate(User user);
}