using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ClaimsService.Infrastructure.Identity;

public sealed class CustomerServiceAuthenticationHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string _scope;

    public CustomerServiceAuthenticationHandler(
        TokenCredential credential,
        IConfiguration configuration)
    {
        _credential = credential;

        _scope = configuration["Services:CustomerService:Scope"]
            ?? throw new InvalidOperationException(
                "Customer Service scope is not configured.");
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(new[]
            {
                _scope
            }),
            cancellationToken);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}