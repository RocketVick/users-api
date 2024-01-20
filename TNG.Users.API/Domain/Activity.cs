namespace TNG.Users.API.Domain;

public class Activity
{
    public int Id { get; set; }
    public string Description { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}