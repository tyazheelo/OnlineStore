using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Order.Infrastructure.Clients;

public sealed class UserAuthorizationForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserAuthorizationForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var headerValue))
            request.Headers.Authorization = headerValue;

        return base.SendAsync(request, cancellationToken);
    }
}
