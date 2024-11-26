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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

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
           .SetLogoutEndpointUris("connect/logout");

    options.RegisterScopes(OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles);

    options.AllowPasswordFlow();

    options.UseAspNetCore()
      .EnableAuthorizationEndpointPassthrough()
      .EnableLogoutEndpointPassthrough()
      .EnableTokenEndpointPassthrough()
      .DisableTransportSecurityRequirement();

    options.SetAccessTokenLifetime(TimeSpan.FromHours(12));
    options.SetIdentityTokenLifetime(TimeSpan.FromHours(12));
    options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

    options.DisableAccessTokenEncryption();

    // Only for development
    options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();

    options.Configure(options =>
    {
      options.TokenValidationParameters.ValidIssuers =
      [
        configuration["Identity:Issuer"]
      ];
    });

    options.SetIssuer(configuration["Identity:Issuer"]);
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
      options.SetIssuer(configuration["Identity:Issuer"]);

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
    identityOptions.Password.RequiredLength = 8;
    identityOptions.Password.RequireNonAlphanumeric = true;
    identityOptions.Password.RequireDigit = true;
    identityOptions.Password.RequireUppercase = true;
    identityOptions.Password.RequireLowercase = true;
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

  options.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }, Scheme = "oauth2", Name = "Bearer", In = ParameterLocation.Header }, new List<string>() } });

  options.MapType<object>(() => new OpenApiSchema { Type = "object" });
});

builder.Services.AddSwaggerGenNewtonsoftSupport();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddAutoMapper(Loader.Current.Assemblies);

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

var app = builder.Build();

app.UseCors(corsPolicyBuilder =>
{
  corsPolicyBuilder.AllowAnyHeader();
  corsPolicyBuilder.AllowAnyMethod();
  corsPolicyBuilder.AllowAnyOrigin();
});

app.UseForwardedHeaders();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Only for development
var scheme = "http";
app.UseDeveloperExceptionPage();

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
                            Type = "object",
                            Properties = {
                                ["client_id"] = new OpenApiSchema { Type = "string", Enum = [new OpenApiString("voyager_app")] },
                                ["grant_type"] = new OpenApiSchema { Type = "string", Enum = [new OpenApiString("password")] },
                                ["username"] = new OpenApiSchema { Type = "string" },
                                ["password"] = new OpenApiSchema { Type = "string", Format = "password" }
                            },
                            Required = new HashSet<string> { "client_id", "grant_type", "username", "password" }
                        }
                    }
                }
      };
    }
  }
}
