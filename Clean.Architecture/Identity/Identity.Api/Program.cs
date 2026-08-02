using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json.Nodes;
using Arbitrer;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Voyager.Shared.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Voyager.Shared.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});

// Composition root: layers are wired explicitly here instead of being discovered at
// runtime by a plugin loader (contrast with Plugin.Microservices.CQRS's Common.Core.Loader).
builder.Services.AddIdentityInfrastructure(configuration);

builder.Services.AddOpenIddict()
  .AddCore(options =>
  {
    options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>();
  })
  .AddServer(options =>
  {
    options.SetTokenEndpointUris("connect/token")
           .SetEndSessionEndpointUris("connect/logout");

    options.RegisterScopes(OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles);

    options.AllowPasswordFlow();

    options.UseAspNetCore()
      .EnableAuthorizationEndpointPassthrough()
      .EnableEndSessionEndpointPassthrough()
      .EnableTokenEndpointPassthrough()
      .DisableTransportSecurityRequirement();

    // Demo-scope relaxations, not for production: DisableTransportSecurityRequirement allows the
    // token endpoint over plain HTTP (services talk over the docker-compose network, not TLS);
    // the long token lifetimes and DisableAccessTokenEncryption below keep the JWT plaintext and
    // long-lived so it's easy to inspect while developing. Tighten all of this before any
    // non-demo deployment.
    options.SetAccessTokenLifetime(TimeSpan.FromHours(configuration.GetValue<double>("Identity:AccessTokenLifetimeHours")));
    options.SetIdentityTokenLifetime(TimeSpan.FromHours(configuration.GetValue<double>("Identity:IdentityTokenLifetimeHours")));
    options.SetRefreshTokenLifetime(TimeSpan.FromDays(configuration.GetValue<double>("Identity:RefreshTokenLifetimeDays")));

    options.DisableAccessTokenEncryption();

    if (builder.Environment.IsDevelopment())
    {
      options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
    }
    else
    {
      // No persisted-certificate loading path exists yet in this codebase (no config-bound
      // thumbprint/path pattern to follow). Fail fast rather than silently falling back to
      // ephemeral development certificates outside Development.
      throw new InvalidOperationException("Signing certificate must be configured for non-development environments");
    }

    options.Configure(openIddictServerOptions =>
    {
      openIddictServerOptions.TokenValidationParameters.ValidIssuers =
      [
        configuration["Identity:Issuer"]
      ];
    });

    options.SetIssuer(configuration["Identity:Issuer"]!);
  })
  .AddValidation(options =>
  {
    options.UseLocalServer();
    options.UseAspNetCore();
  });

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
{
  options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddCors();

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
  options.SerializerSettings.Converters.Add(new StringEnumConverter());
  options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
  options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
  options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
  options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
  options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});

builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc("v1", new OpenApiInfo { Version = "v1", Title = "Voyager Identity API" });
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
  });
  options.OperationFilter<AddPasswordGrantParams>();

  options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement { { new OpenApiSecuritySchemeReference("Bearer"), [] } });

  options.MapType<object>(() => new OpenApiSchema { Type = JsonSchemaType.Object });
});

builder.Services.AddSwaggerGenNewtonsoftSupport();

var applicationAssembly = typeof(Identity.Application.CQRS.Commands.RegisterUserHandler).Assembly;

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));

builder.Services.AddArbitrer(options =>
{
  options.Behaviour = ArbitrerBehaviourEnum.ImplicitRemote;
  options.InferLocalRequests([applicationAssembly]);
  options.InferLocalNotifications([applicationAssembly]);
});

builder.Services.AddArbitrerRabbitMQMessageDispatcher(o =>
{
  configuration.GetSection("RabbitMQ").Bind(o);
  o.AutoDelete = false;
  o.Durable = true;
  o.ClientName = Assembly.GetExecutingAssembly().FullName;
}).AddRabbitMQRequestManager();

builder.Services.AddHttpContextAccessor();

// The password grant at /connect/token has no other brute-force protection (no lockout,
// no CAPTCHA) — rate limiting is the only thing standing between it and credential stuffing.
builder.Services.AddCustomRateLimiting(configuration);

builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>("identity-database", tags: ["ready"]);

// Runs after the server is listening, so /health answers during migration; /ready
// stays unhealthy until it finishes.
builder.Services.AddStartupMigration(sp => sp.MigrateIdentityDatabase());

var app = builder.Build();

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

app.UseCors(corsPolicyBuilder =>
{
  corsPolicyBuilder.AllowAnyHeader();
  corsPolicyBuilder.AllowAnyMethod();
  corsPolicyBuilder.WithOrigins(allowedOrigins);
});

app.UseForwardedHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

// Only for development
const string scheme = "http";

app.UseDomainExceptionHandler();

app.UseSwagger(options =>
{
  options.PreSerializeFilters.Add((swagger, httpReq) =>
  {
    swagger.Servers = new List<OpenApiServer> { new() { Url = $"{scheme}://{httpReq.Host.Value}" } };
  });
});
app.UseSwaggerUI(options =>
{
  options.DocumentTitle = "Voyager Identity API";
  options.SwaggerEndpoint("/swagger/v1/swagger.json", "Voyager Identity API");
  options.DocExpansion(DocExpansion.None);
  options.EnableFilter();
  options.EnablePersistAuthorization();
  options.EnableTryItOutByDefault();
  options.EnableValidator();
  options.EnableDeepLinking();
});

app.MapGet("/", context =>
{
  context.Response.Redirect("/swagger/");
  return System.Threading.Tasks.Task.CompletedTask;
});

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

public class AddPasswordGrantParams : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    if (context.ApiDescription.RelativePath == "connect/token" && context.MethodInfo.Name == "Exchange")
    {
      operation.RequestBody = new OpenApiRequestBody
      {
        Content = {
                    ["application/x-www-form-urlencoded"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = {
                                ["client_id"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("voyager_app")] },
                                ["grant_type"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("password")] },
                                ["username"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                ["password"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "password" }
                            },
                            Required = new HashSet<string> { "client_id", "grant_type", "username", "password" }
                        }
                    }
                }
      };
    }
  }
}
