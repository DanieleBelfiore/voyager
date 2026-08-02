using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Driver.Module.DependencyInjection;
using FluentValidation;
using Hub.Module.DependencyInjection;
using Identity.Module.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ride.Module.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Voyager.Shared.RateLimiting;
using Voyager.Shared.Validation;
using Voyager.Shared.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(opt =>
    opt.AllowAnyMethod().AllowAnyHeader().WithOrigins(allowedOrigins).AllowCredentials());
});

// Composition root: each module wires its own infrastructure through its own public
// Add*Module extension. Host never references a module's internal types — Identity.Module
// owns the entire OpenIddict setup (issuance + local validation) that every other module's
// [Authorize] attribute relies on.
builder.Services.AddIdentityModule(configuration, builder.Environment);
builder.Services.AddDriverModule(configuration);
builder.Services.AddRideModule(configuration);
builder.Services.AddHubModule(configuration);

// "is_driver" is the custom claim Identity's AuthenticateUserController stamps onto the access
// token (see Constants.IsDriverClaimType) — AcceptRide is the only endpoint that needs it:
// without this check any authenticated rider could accept their own (or anyone else's) pending ride.
builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("RequireDriver", policy => policy.RequireClaim("is_driver", "True"));
});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
  options.SerializerSettings.Converters.Add(new StringEnumConverter());
  options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
  options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
  options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
  options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
  options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
  options.SerializerSettings.Converters.Add(new GeometryConverter());
});

builder.Services.AddSwaggerGen(g =>
{
  g.SwaggerDoc("v1", new OpenApiInfo { Title = "Voyager API (Modular Monolith)", Version = "v1" });
  g.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
  });
  g.OperationFilter<AddPasswordGrantParams>();

  g.AddSecurityRequirement(_ => new OpenApiSecurityRequirement { { new OpenApiSecuritySchemeReference("Bearer"), [] } });

  g.CustomSchemaIds(x => x.FullName);

  g.MapType<object>(() => new OpenApiSchema { Type = JsonSchemaType.Object });

  g.MapType<Point>(() => new OpenApiSchema
  {
    Type = JsonSchemaType.Object,
    Properties = new Dictionary<string, IOpenApiSchema>
    {
      ["type"] = new OpenApiSchema { Type = JsonSchemaType.String, Default = JsonValue.Create("Point") },
      ["coordinates"] = new OpenApiSchema
      {
        Type = JsonSchemaType.Array,
        Items = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" },
        MinItems = 2,
        MaxItems = 2
      }
    },
    Required = new HashSet<string> { "type", "coordinates" }
  });
});

builder.Services.AddSwaggerGenNewtonsoftSupport();

// One shared mediator across all four modules — this is what replaces Arbitrer. A handler in
// one module's assembly calling IMediator.Send/Publish is resolved directly against a handler
// registered from a different module's assembly, in the same DI container, no message bus.
var moduleAssemblies = new[]
{
  typeof(IdentityModuleExtensions).Assembly,
  typeof(DriverModuleExtensions).Assembly,
  typeof(RideModuleExtensions).Assembly,
  typeof(HubModuleExtensions).Assembly
};

builder.Services.AddMediatR(cfg =>
{
  cfg.RegisterServicesFromAssemblies(moduleAssemblies);
  cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
// includeInternalTypes: true is required, not optional. Every validator in this variant is
// internal by design (see CLAUDE.md — validators are never part of a public signature), and the
// default scan uses GetExportedTypes(), which skips them. Without this the ValidationBehavior
// above resolves an empty IEnumerable<IValidator<T>> and silently validates nothing.
builder.Services.AddValidatorsFromAssemblies(moduleAssemblies, includeInternalTypes: true);

builder.Services.AddHttpContextAccessor();

builder.Services.AddCustomRateLimiting(configuration);

// Runs after the server is listening, so /health answers during migration; /ready
// stays unhealthy until it finishes.
builder.Services.AddStartupMigration(sp =>
{
  sp.MigrateIdentityDatabase();
  sp.MigrateDriverDatabase();
  sp.MigrateRideDatabase();
});

builder.Services.AddForwardedHeaders(configuration);

var app = builder.Build();

// First in the pipeline: everything downstream (rate-limit partitioning, the
// issuer/redirect scheme) reads the client IP and scheme this corrects.
app.UseForwardedHeaders();

// Only for development
const string scheme = "http";

app.UseDomainExceptionHandler();

app.UseRouting();
app.UseSwagger(options =>
{
  options.PreSerializeFilters.Add((swagger, httpReq) =>
  {
    swagger.Servers = new List<OpenApiServer> { new() { Url = $"{scheme}://{httpReq.Host.Value}" } };
  });
});

app.UseSwaggerUI(options =>
{
  options.DocumentTitle = "Voyager API (Modular Monolith)";
  options.SwaggerEndpoint("/swagger/v1/swagger.json", "Voyager API");
  options.DocExpansion(DocExpansion.None);
  options.EnableFilter();
  options.EnablePersistAuthorization();
  options.EnableTryItOutByDefault();
  options.EnableValidator();
  options.EnableDeepLinking();
});

app.UseCors();

app.UseHubModule(); // must run before UseAuthentication — see Hub.Module's HubModuleExtensions

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapGet("/", context =>
{
  context.Response.Redirect("/swagger/");
  return System.Threading.Tasks.Task.CompletedTask;
});

app.MapControllers();
app.MapHubModule();

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
