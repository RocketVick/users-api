namespace TNG.Users.API.Contracts;

public abstract class Message
{
    public Guid MessageId { get; } = Guid.NewGuid();
}