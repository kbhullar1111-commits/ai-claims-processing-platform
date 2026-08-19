namespace ClaimsService.Application.Models;
public sealed class CustomerContext
{
    public Guid CustomerId { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    
}