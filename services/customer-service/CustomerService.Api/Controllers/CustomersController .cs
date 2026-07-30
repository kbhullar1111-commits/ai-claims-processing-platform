using Microsoft.AspNetCore.Mvc;
using CustomerService.API.Models;
using CustomerService.Application.Customers.CreateCustomer;

namespace CustomerService.API.Controllers;

/// <summary>
/// Handles customer submission endpoints.
/// </summary>
[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    private readonly CreateCustomerHandler _handler;

    public CustomersController(CreateCustomerHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Email,
            request.Phone,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            request.PreferredCommunication
        );

        var response = await _handler.HandleAsync(command, cancellationToken);

        return Created(
        $"/customers/{response.CustomerId}",
        response);
    }

}