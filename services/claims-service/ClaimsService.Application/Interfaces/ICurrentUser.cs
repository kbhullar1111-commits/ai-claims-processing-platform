public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    string? Name { get; }
}