using ClaimsService.Application.Models;
using ClaimsService.Application.Interfaces;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ClaimsService.Infrastructure.ClaimValidation;
public sealed class PolicyClient : IPolicyClient
{
    private readonly HttpClient _httpClient;

    private readonly ILogger<PolicyClient> _logger;

    public PolicyClient(HttpClient httpClient, ILogger<PolicyClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    public async Task<ValidateClaimResponse> ValidateClaimAsync(
        ValidateClaimRequest validateClaimRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "policies/validate-claim",
                validateClaimRequest,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var reason = await response.Content.ReadAsStringAsync(cancellationToken);

                return new ValidateClaimResponse(
                    false,
                    null,
                    null,
                    reason
                );
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ValidateClaimResponse>(
                       cancellationToken)
                   ?? throw new InvalidOperationException(
                       "The policy service returned an empty validation response.");
        }
        catch (TaskCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The request to the Policy Service timed out.",
                ex);
        }
    }
}