using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Models;
using MarketTrustAPI.SpatialIndexManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ControllerTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
	public UserManager<User> UserManager { get; private set; } = CreateUserManager();
	public SignInManager<User> SignInManager { get; private set; } = CreateSignInManager();
	public ITokenService TokenService { get; private set; } = Substitute.For<ITokenService>();
	public ISpatialIndexManager<User> SpatialIndexManager { get; private set; } = Substitute.For<ISpatialIndexManager<User>>();
	public IUserRepository UserRepository { get; private set; } = Substitute.For<IUserRepository>();

	public void ResetSubstitutes()
	{
		UserManager = CreateUserManager();
		SignInManager = CreateSignInManager();
		TokenService = Substitute.For<ITokenService>();
		SpatialIndexManager = Substitute.For<ISpatialIndexManager<User>>();
		UserRepository = Substitute.For<IUserRepository>();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureAppConfiguration((_, configBuilder) =>
		{
			configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["AllowedOrigins"] = "",
				["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=MarketTrustAPI_Test;Trusted_Connection=True;MultipleActiveResultSets=true",
				["JWT:SigningKey"] = "TestSigningKey",
				["JWT:Issuer"] = "http://localhost:5167",
				["JWT:Audience"] = "http://localhost:5167"
			});
		});

		builder.ConfigureTestServices(services =>
		{
			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
				options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
			})
			.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName,
                options => { });

			services.RemoveAll<UserManager<User>>();
			services.RemoveAll<SignInManager<User>>();
			services.RemoveAll<ITokenService>();
			services.RemoveAll<ISpatialIndexManager<User>>();
			services.RemoveAll<IUserRepository>();

			services.AddScoped(_ => UserManager);
			services.AddScoped(_ => SignInManager);
			services.AddScoped(_ => TokenService);
			services.AddScoped(_ => SpatialIndexManager);
			services.AddScoped(_ => UserRepository);
		});
	}

	private static UserManager<User> CreateUserManager()
	{
		IUserPasswordStore<User> store = Substitute.For<IUserPasswordStore<User>>();
		IOptions<IdentityOptions> options = Substitute.For<IOptions<IdentityOptions>>();
		options.Value.Returns(new IdentityOptions());

		return Substitute.For<UserManager<User>>(
			store,
			options,
			Substitute.For<IPasswordHasher<User>>(),
			Array.Empty<IUserValidator<User>>(),
			Array.Empty<IPasswordValidator<User>>(),
			Substitute.For<ILookupNormalizer>(),
			new IdentityErrorDescriber(),
			Substitute.For<IServiceProvider>(),
			Substitute.For<ILogger<UserManager<User>>>());
	}

	private static SignInManager<User> CreateSignInManager()
	{
		IOptions<IdentityOptions> options = Substitute.For<IOptions<IdentityOptions>>();
		options.Value.Returns(new IdentityOptions());

		return Substitute.For<SignInManager<User>>(
			CreateUserManager(),
			Substitute.For<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
			Substitute.For<IUserClaimsPrincipalFactory<User>>(),
			options,
			Substitute.For<ILogger<SignInManager<User>>>(),
			Substitute.For<IAuthenticationSchemeProvider>(),
			Substitute.For<IUserConfirmation<User>>());
	}
}
