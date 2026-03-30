using System.Reflection;
using System.Text;
using MarketTrustAPI.Configuration;
using MarketTrustAPI.Data;
using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Models;
using MarketTrustAPI.Repository;
using MarketTrustAPI.ReputationManager;
using MarketTrustAPI.Services;
using MarketTrustAPI.SpatialIndexManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

string allowedSpecificOrigins = "AllowedSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

string allowedOriginsEnv = builder.Configuration["AllowedOrigins"] ?? string.Empty;
string[] allowedOrigins = allowedOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy(allowedSpecificOrigins,
        policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });

    // XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        option.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddDbContext<ApplicationDBContext>(options => {
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite().CommandTimeout(180));
});

builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDBContext>();

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = 
    options.DefaultChallengeScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options => {
        string? signingKey = builder.Configuration["JWT:SigningKey"];
        if (string.IsNullOrEmpty(signingKey))
        {
            throw new ArgumentNullException("JWT:SigningKey is missing or empty.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true
        };
    });

builder.Services.Configure<EigenTrustConfig>(builder.Configuration.GetSection("EigenTrust"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IReputationManager>(sp => {
    EigenTrustConfig config = sp.GetRequiredService<IOptions<EigenTrustConfig>>().Value;
    return new EigenTrust(config.Alpha, config.Epsilon, config.MaxIterations);
});
builder.Services.AddScoped<IReputationService, ReputationService>();
builder.Services.AddScoped<IReputationRepository, ReputationRepository>();
builder.Services.AddScoped<ITrustRatingRepository, TrustRatingRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddSingleton<IGeographicConverter, GeographicConverter>();
builder.Services.AddSingleton<ISpatialIndexManager<User>, QuadtreeManager<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    // https://stackoverflow.com/a/73896840
    using var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
    var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = serviceScope.ServiceProvider.GetRequiredService<ApplicationDBContext>().Database;

    logger.LogInformation("Migrating database...");

    while (!db.CanConnect())
    {
        logger.LogInformation("Database not ready yet; waiting...");
        Thread.Sleep(1000);
    }

    try
    {
        db.Migrate();
        logger.LogInformation("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseHttpsRedirection();

app.UseCors(allowedSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;