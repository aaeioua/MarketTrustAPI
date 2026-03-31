using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControllerTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public const string SchemeName = "TestScheme";
	public const string UserIdHeaderName = "X-Test-UserId";

	public TestAuthHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder)
		: base(options, logger, encoder)
	{
	}

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		List<Claim> claims = new();
		if (Request.Headers.TryGetValue(UserIdHeaderName, out var userId) && !string.IsNullOrWhiteSpace(userId))
		{
			claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
		}

		ClaimsIdentity identity = new(claims, SchemeName);
		ClaimsPrincipal principal = new(identity);
		AuthenticationTicket ticket = new(principal, SchemeName);

        AuthenticateResult result = AuthenticateResult.Success(ticket);

		return Task.FromResult(result);
	}
}
