using ClaimsService.Application.Models;
using ClaimsService.Application.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClaimsService.Infrastructure.Identity;
public sealed class CustomerClient : ICustomerClient
{
    private readonly HttpClient _httpClient;

    private readonly ICustomerIdCache _customerIdCache;

    public CustomerClient(HttpClient httpClient, ICustomerIdCache customerIdCache)
    {
        _httpClient = httpClient;
        _customerIdCache = customerIdCache;
    }

    public async Task<Guid?> GetCustomerIdByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var cachedCustomerId = await _customerIdCache.GetAsync(
            email,
            cancellationToken);

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

        await _customerIdCache.SetAsync(
            email,
            customer.CustomerId,
            TimeSpan.FromHours(1),
            cancellationToken);

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