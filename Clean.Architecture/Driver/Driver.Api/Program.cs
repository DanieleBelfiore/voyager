using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Arbitrer;
using Driver.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;
using Voyager.Shared.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Driver.Infrastructure.Persistence;
using Voyager.Shared.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(opt =>
    opt.AllowAnyMethod().AllowAnyHeader().WithOrigins(allowedOrigins).AllowCredentials());
});

// Composition root: layers are wired explicitly here instead of being discovered at
// runtime by a plugin loader (contrast with Plugin.Microservices.CQRS's Common.Core.Loader).
builder.Services.AddDriverInfrastructure(configuration);

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
  g.SwaggerDoc("v1", new OpenApiInfo { Title = "Voyager Driver API", Version = "v1" });
  g.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
  });

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

var applicationAssembly = typeof(Driver.Application.CQRS.Commands.AddDriverHandler).Assembly;

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

builder.Services.AddCustomRateLimiting(builder.Configuration);

builder.Services.AddHealthChecks().AddDbContextCheck<DriverDbContext>("driver-database", tags: ["ready"]);

// Runs after the server is listening, so /health answers during migration; /ready
// stays unhealthy until it finishes.
builder.Services.AddStartupMigration(sp => sp.MigrateDriverDatabase());

var app = builder.Build();

// Only for development
const string scheme = "http";

app.UseDomainExceptionHandler();

app.UseRouting();
app.UseSwagger(options =>
{
  options.RouteTemplate = "driver/swagger/{documentName}/swagger.json";
  options.PreSerializeFilters.Add((swagger, httpReq) =>
  {
    swagger.Servers = new List<OpenApiServer> { new() { Url = $"{scheme}://{httpReq.Host.Value}" } };
  });
});

app.UseSwaggerUI(options =>
{
  options.DocumentTitle = "Voyager Driver API";
  options.SwaggerEndpoint("/driver/swagger/v1/swagger.json", "Voyager Driver API");
  options.RoutePrefix = "driver/swagger";
  options.DocExpansion(DocExpansion.None);
  options.EnableFilter();
  options.EnablePersistAuthorization();
  options.EnableTryItOutByDefault();
  options.EnableValidator();
  options.EnableDeepLinking();
});

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapGet("/", context =>
{
  context.Response.Redirect("/driver/swagger/");
  return Task.CompletedTask;
});

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
