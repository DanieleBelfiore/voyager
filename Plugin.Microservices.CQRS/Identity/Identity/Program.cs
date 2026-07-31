using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbitrer;
using Common.Core;
using Identity.Handlers.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Common.Core.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

Loader.Current.Directories.Add(Directory.GetCurrentDirectory());
Loader.Current.Compose();

Loader.Current.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

builder.Services.Configure<IdentityOptions>(options =>
{
  options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
  options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
  options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
  options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
  options.TokenLifespan = TimeSpan.FromDays(5);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});

builder.Services.AddOpenIddict()
  .AddCore(options =>
  {
    options.UseEntityFrameworkCore().UseDbContext<IdentityContext>();
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

    // Only for development
    options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();

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

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
      options.SetIssuer(configuration["Identity:Issuer"]!);

      options.UseSystemNetHttp();
      options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
  options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

builder.Services.ConfigureApplicationCookie(opts =>
{
  opts.Cookie.SameSite = SameSiteMode.None;
  opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddIdentity<VoyagerUser, VoyagerRole>(identityOptions =>
  {
    identityOptions.SignIn.RequireConfirmedEmail = false;
  })
  .AddEntityFrameworkStores<IdentityContext>()
  .AddDefaultTokenProviders();

builder.Services.AddCors();

builder.Services.AddOptions();

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

  var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.API.xml";
  var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  options.IncludeXmlComments(xmlPath);

  options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement { { new OpenApiSecuritySchemeReference("Bearer"),
    []
  } });

  options.MapType<object>(() => new OpenApiSchema { Type = JsonSchemaType.Object });
});

builder.Services.AddSwaggerGenNewtonsoftSupport();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
var assemblies = Loader.Current.Modules.Select(f => f.GetType().Assembly).ToList();
assemblies.Add(Assembly.GetExecutingAssembly());

builder.Services.AddArbitrer(options =>
{
  options.Behaviour = ArbitrerBehaviourEnum.ImplicitRemote;
  options.InferLocalRequests(assemblies);
  options.InferLocalNotifications(assemblies);
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

var app = builder.Build();

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

app.UseCors(corsPolicyBuilder =>
{
  corsPolicyBuilder.AllowAnyHeader();
  corsPolicyBuilder.AllowAnyMethod();
  corsPolicyBuilder.WithOrigins(allowedOrigins);
});

app.UseForwardedHeaders();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

// Only for development
const string scheme = "http";

if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}
else
{
  // Do not leak stack traces/paths outside Development: a generic response, with
  // UnauthorizedAccessException mapped to 403 since handlers already use it for that.
  app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
  {
    var statusCode = context.Features.Get<IExceptionHandlerFeature>()?.Error is UnauthorizedAccessException
      ? StatusCodes.Status403Forbidden
      : StatusCodes.Status500InternalServerError;

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { error = statusCode == StatusCodes.Status403Forbidden ? "forbidden" : "internal_server_error" });
  }));
}

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

app.UseRewriter(new RewriteOptions().AddRedirect("^$", "swagger"));

using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>();

Loader.Current.AddModules(app);

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
