using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Hikyaku.Kaido;
using Hub.Api;
using Hub.Api.Middlewares;
using Hub.Application.Ports;
using Hub.Infrastructure.DependencyInjection;
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
using Voyager.Shared.Diagnostics;
using StackExchange.Redis;

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
builder.Services.AddHubInfrastructure(configuration);

// SignalRHubRelay lives in this project (not Hub.Infrastructure) because it's generic over
// the concrete VoyagerHub type — see SignalRHubRelay.cs.
builder.Services.AddScoped<IHubRelay, SignalRHubRelay>();

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

builder.Services.AddSignalR(options =>
{
  options.MaximumReceiveMessageSize = 32 * 1024;
  options.ClientTimeoutInterval = TimeSpan.FromSeconds(configuration.GetValue<double>("SignalR:ClientTimeoutSeconds"));
  options.KeepAliveInterval = TimeSpan.FromSeconds(configuration.GetValue<double>("SignalR:KeepAliveSeconds"));
  options.EnableDetailedErrors = true;
}).AddNewtonsoftJsonProtocol(options =>
{
  options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter());
  // Without this, a Point argument/payload (UpdateDriverLocation, SendToRiderNewDriverLocation)
  // serializes as NTS's internal Coordinate/CoordinateSequence/Factory graph instead of GeoJSON,
  // and won't round-trip back into a Point on the other end. Same converter the MVC pipeline
  // below already registers for controller request/response bodies.
  options.PayloadSerializerSettings.Converters.Add(new GeometryConverter());
  options.PayloadSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
  options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
  options.PayloadSerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
  options.PayloadSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
  options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
// A single Hub instance is fine for this exercise's scale, but the backplane is what makes
// horizontal scaling (Scalability & performance in DESIGN.md) actually work for SignalR: without
// it, a client connected to Hub instance A never receives a group message published from instance
// B, since group membership and Clients.Group(...) dispatch are both in-memory and per-instance.
}).AddStackExchangeRedis(options =>
{
  // Same connect options the cache side already uses (see CacheExtensions):
  // AbortOnConnectFail=false so an instance still starts when Redis is briefly
  // unreachable and reconnects on its own, rather than throwing at startup.
  options.Configuration = new ConfigurationOptions
  {
    EndPoints = { configuration["Redis:ConnectionString"] },
    AbortOnConnectFail = false,
    ConnectTimeout = 6000,
    SyncTimeout = 6000,
    ConnectRetry = 3
  };
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
  g.SwaggerDoc("v1", new OpenApiInfo { Title = "Voyager Hub API", Version = "v1" });
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

var applicationAssembly = typeof(Hub.Application.CQRS.Commands.UpdateDriverLocationHandler).Assembly;

builder.Services.AddHikyaku(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));

builder.Services.AddKaido(options =>
{
  options.Behaviour = HikyakuBehaviourEnum.ImplicitRemote;
  options.InferLocalRequests([applicationAssembly]);
  options.InferLocalNotifications([applicationAssembly]);
});

builder.Services.AddHikyakuRabbitMQMessageDispatcher(o =>
{
  configuration.GetSection("RabbitMQ").Bind(o);
  o.AutoDelete = false;
  o.Durable = true;
  o.ClientName = Assembly.GetExecutingAssembly().FullName;
}).AddRabbitMQRequestManager();

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<QueryStringTokenMiddleware>();

builder.Services.AddHealthChecks().AddRedis(configuration["Redis:ConnectionString"], "redis", tags: ["ready"]);

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
  options.RouteTemplate = "hub/swagger/{documentName}/swagger.json";
  options.PreSerializeFilters.Add((swagger, httpReq) =>
  {
    swagger.Servers = new List<OpenApiServer> { new() { Url = $"{scheme}://{httpReq.Host.Value}" } };
  });
});

app.UseSwaggerUI(options =>
{
  options.DocumentTitle = "Voyager Hub API";
  options.SwaggerEndpoint("/hub/swagger/v1/swagger.json", "Voyager Hub API");
  options.RoutePrefix = "hub/swagger";
  options.DocExpansion(DocExpansion.None);
  options.EnableFilter();
  options.EnablePersistAuthorization();
  options.EnableTryItOutByDefault();
  options.EnableValidator();
  options.EnableDeepLinking();
});

app.UseCors();

app.UseMiddleware<QueryStringTokenMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", context =>
{
  context.Response.Redirect("/hub/swagger/");
  return System.Threading.Tasks.Task.CompletedTask;
});

app.MapHub<VoyagerHub>("/voyagerhub");

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
