using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Pwneu.Chat.Shared.Data;
using Pwneu.Chat.Shared.Extensions;
using Pwneu.Chat.Shared.Options;
using Pwneu.Shared.Common;
using Pwneu.Shared.Extensions;
using Serilog;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// Serilog.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// App options.
builder.Services
    .AddOptions<ChatOptions>()
    .BindConfiguration($"{nameof(ChatOptions)}")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// OpenTelemetry.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(nameof(Pwneu.Chat)))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    });

builder.Services.AddOpenAIChatCompletion("gpt-4o-mini", builder.Configuration[Consts.ChatOptionsOpenAiApiKey]!);

// Swagger UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// CORS (Cross-Origin Resource Sharing).
builder.Services.AddCors();

// Postgres Database.
var sqlite = builder.Configuration.GetConnectionString(Consts.Sqlite) ??
             throw new InvalidOperationException("No Sqlite connection found");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(sqlite));

var assembly = typeof(Program).Assembly;

// RabbitMQ.
builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();
    busConfigurator.AddConsumers(assembly);
    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(new Uri(builder.Configuration[Consts.MessageBrokerHost]!), h =>
        {
            h.Username(builder.Configuration[Consts.MessageBrokerUsername]!);
            h.Password(builder.Configuration[Consts.MessageBrokerPassword]!);
        });
        configurator.ConfigureEndpoints(context);
    });
});

// Assembly scanning of Mediator and Fluent Validations.
builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));
builder.Services.AddValidatorsFromAssembly(assembly);

// Add endpoints from the Features folder (Vertical Slice).
builder.Services.AddEndpoints(assembly);

// Authentication and Authorization (JSON Web Token).
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(0),
            ValidIssuer = builder.Configuration[Consts.JwtOptionsIssuer],
            ValidAudience = builder.Configuration[Consts.JwtOptionsAudience],
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[Consts.JwtOptionsSigningKey]!)),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Consts.AdminOnly, policy => { policy.RequireRole(Consts.Admin); })
    .AddPolicy(Consts.ManagerAdminOnly, policy => { policy.RequireRole(Consts.Manager); })
    .AddPolicy(Consts.MemberOnly, policy => { policy.RequireRole(Consts.Member); });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(Consts.Fixed, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.GetLoggedInUserId<string>(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = TimeSpan.FromSeconds(7),
            }));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/healthz");

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.ApplyMigrations();

app.UseCors(policy => policy.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin());

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
    app.MapGet("/api", async context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var forwardedForHeader = context.Request.Headers["X-Forwarded-For"].ToString();
        var forwardedProtoHeader = context.Request.Headers["X-Forwarded-Proto"].ToString();
        var forwardedHostHeader = context.Request.Headers["X-Forwarded-Host"].ToString();
        var cfConnectingIp = context.Request.Headers[Consts.CfConnectingIp].ToString();

        var response = new
        {
            Service = "Pwneu Chat",
            ClientIp = clientIp,
            ForwardedFor = forwardedForHeader,
            ForwardedProto = forwardedProtoHeader,
            ForwardedHost = forwardedHostHeader,
            CfConnectingIp = cfConnectingIp
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    });

app.MapEndpoints();

app.Run();

public partial class Program;