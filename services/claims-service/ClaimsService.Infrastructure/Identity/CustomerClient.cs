using ClaimsService.Application.Models;
using ClaimsService.Application.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClaimsService.Infrastructure.Identity;
public sealed class CustomerClient : ICustomerClient
{
    private readonly HttpClient _httpClient;

    private readonly ICustomerIdCache _customerIdCache;

    private readonly ILogger<CustomerClient> _logger;

    public CustomerClient(HttpClient httpClient, ICustomerIdCache customerIdCache, ILogger<CustomerClient> logger)
    {
        _httpClient = httpClient;
        _customerIdCache = customerIdCache;
        _logger = logger;
    }

    public async Task<Guid?> GetCustomerIdByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        try
        {
            var cachedCustomerId = await _customerIdCache.GetAsync(
                email,
                cancellationToken);
        }
        catch (RedisException ex)
        {
            // Log the exception if needed, but do not fail the operation
            _logger.LogWarning(
                ex,
                "Redis cache unavailable while resolving CustomerId for email {Email}. Falling back to Customer Service.",
                email);
        }

        if (cachedCustomerId.HasValue)
        {
            return cachedCustomerId.Value;
        }

        var customer = await GetByEmailAsync(
            email,
            cancellationToken);

        if (customer is null)
        {
            return null;
        }

        try
        {
            await _customerIdCache.SetAsync(
                email,
                customer.CustomerId,
                TimeSpan.FromHours(1),
                cancellationToken);
        }
        catch (RedisException ex)
        {
            // Log the exception if needed, but do not fail the operation
            _logger.LogWarning(
                ex,
                "Failed to populate CustomerId cache for email {Email}. Continuing without cache.",
                email);
        }

        return customer.CustomerId;
    }

    public async Task<CustomerContext?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        try{

            var response = await _httpClient.GetAsync(
                $"customers/by-email/{Uri.EscapeDataString(email)}",
                cancellationToken);
    
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<CustomerContext>(
                    cancellationToken);
        }
        catch (TaskCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The request to the Customer Service timed out.",
                ex);
        }
    }
}