using ClaimsService.Application.Models;
using ClaimsService.Application.Interfaces;

namespace ClaimsService.Infrastructure.Identity;
public sealed class StaticCustomerResolver
    : ICustomerResolver
{

    private static readonly Dictionary<string, CustomerContext>
    Users = new()
    {
        {
            "424310e3-0169-4311-b644-bcbe013853c7",
            new CustomerContext
            {
                CustomerId = "d9f4cb92-a4b3-4cc6-a8f8-d36f4745af10"
            }
        }
    };

    public CustomerContext Resolve(string userId)
    {
        if (Users.TryGetValue(
                userId,
                out var customerContext))
        {
            return customerContext;
        }

        throw new InvalidOperationException(
            $"No customer mapping exists for user {userId}");
    }
}