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
    private readonly CreateCustomerHandler _createCustomerHandler;
    private readonly GetCustomerHandler _getCustomerHandler;
    private readonly GetCustomersHandler _getCustomersHandler;

    public CustomersController(
        CreateCustomerHandler createCustomerHandler,
        GetCustomerHandler getCustomerHandler,
        GetCustomersHandler getCustomersHandler)
    {
        _createCustomerHandler = createCustomerHandler;
        _getCustomerHandler = getCustomerHandler;
        _getCustomersHandler = getCustomersHandler;
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

        var response = await _createCustomerHandler.HandleAsync(command, cancellationToken);

        return Created(
        $"/customers/{response.CustomerId}",
        response);
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        CancellationToken cancellationToken)
    {
        var query = new GetCustomersQuery();

        var response = await _getCustomersHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> GetCustomer(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerQuery(customerId);

        var response = await _getCustomerHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(response);
    }

}