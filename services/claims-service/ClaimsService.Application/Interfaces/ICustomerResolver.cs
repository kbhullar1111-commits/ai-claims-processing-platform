using ClaimsService.Application.Models;

namespace ClaimsService.Application.Interfaces;
public interface ICustomerResolver
{
    CustomerContext Resolve(string userId);
}