using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CustomerService.API.Models;
using CustomerService.Application.Customers.CreateCustomer;
using CustomerService.Application.Customers.GetCustomer;
using CustomerService.Application.Customers.GetCustomers;
using CustomerService.Application.Customers.UpdateCustomer;

namespace CustomerService.API.Controllers;

/// <summary>
/// Handles customer submission endpoints.
/// </summary>
[Authorize(Policy = "CustomerServiceAccess")]
[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    private readonly CreateCustomerHandler _createCustomerHandler;
    private readonly GetCustomerQueryHandler _getCustomerHandler;
    private readonly GetCustomersQueryHandler _getCustomersHandler;
    private readonly UpdateCustomerHandler _updateCustomerHandler;

    public CustomersController(
        CreateCustomerHandler createCustomerHandler,
        GetCustomerQueryHandler getCustomerHandler,
        GetCustomersQueryHandler getCustomersHandler,
        UpdateCustomerHandler updateCustomerHandler)
    {
        _createCustomerHandler = createCustomerHandler;
        _getCustomerHandler = getCustomerHandler;
        _getCustomersHandler = getCustomersHandler;
        _updateCustomerHandler = updateCustomerHandler;
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
        var query = new GetCustomerQuery(customerId, null);

        var response = await _getCustomerHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("/customers/by-email/{email}")]
    public async Task<IActionResult> GetCustomerByEmail(
        string email,
        CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

        // var query = new GetCustomerQuery(null, email);

        // var response = await _getCustomerHandler.HandleAsync(
        //     query,
        //     cancellationToken);

        // return Ok(new { response.CustomerId, response.Status, response.FirstName, response.LastName, response.Email });
    }

    [HttpPut("{customerId:guid}")]
    public async Task<IActionResult> UpdateCustomer(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand(
            customerId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            request.PreferredCommunication,
            request.Status);

        var response = await _updateCustomerHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(response);
    }

}