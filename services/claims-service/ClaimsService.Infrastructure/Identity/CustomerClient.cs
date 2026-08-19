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

    public CustomerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CustomerContext?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
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
}