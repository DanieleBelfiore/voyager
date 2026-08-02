using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Arbitrer;
using Common.Core;
using Common.Core.Cache;
using Common.Core.Exceptions;
using Common.Core.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ride.Handlers.Models;
using Common.Core.Diagnostics;

// Arbitrer serializes cross-process request/notification payloads over RabbitMQ using
// Newtonsoft's process-wide JsonConvert.DefaultSettings — it exposes no per-call settings hook.
// Without GeometryConverter here, any response carrying a Point (e.g. GetActiveRide/GetRideETA,
// requested remotely by Hub's VoyagerHub) fails to serialize/deserialize correctly on the other
// end: Newtonsoft's default reflection-based binder can't handle NTS's Point internals.
JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
  Converters = { new StringEnumConverter(), new GeometryConverter() }
};

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var hostingEnvironment = builder.Environment;

Loader.Current.Directories.Add(Directory.GetCurrentDirectory());
Loader.Current.Compose();

var module = configuration["ModuleName"];
var moduleName = string.Concat(module!.Select((c, i) => i > 0 && char.IsUpper(c) ? $" {c}" : c.ToString()));
var modulePath = string.Concat(module!.Select((c, i) =>
  i > 0 && char.IsUpper(c) ? $"-{char.ToLower(c)}" : char.ToLower(c).ToString()));

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(opt =>
    opt.AllowAnyMethod().AllowAnyHeader().WithOrigins(allowedOrigins).AllowCredentials());
});

Loader.Current.ConfigureServices(builder.Services, configuration, hostingEnvironment);

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

// "is_driver" is the custom claim UsersController stamps onto the access token principal
// (see Constants.IS_DRIVER) — AcceptRide is the only endpoint that needs it: without this
// check any authenticated rider could accept their own (or anyone else's) pending ride.
builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("RequireDriver", policy => policy.RequireClaim("is_driver", "True"));
});

builder.Services.AddSignalR(options =>
{
  options.MaximumReceiveMessageSize = null;
  options.EnableDetailedErrors = true;
}).AddNewtonsoftJsonProtocol(options =>
{
  options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter());
  options.PayloadSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
  options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
  options.PayloadSerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
  options.PayloadSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
  options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
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
  g.SwaggerDoc("v1", new OpenApiInfo { Title = $"Voyager {moduleName} API", Version = "v1" });
  g.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
  });

  var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.API.xml";
  var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  g.IncludeXmlComments(xmlPath);

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

builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddCustomRateLimiting(builder.Configuration);

builder.Services.AddHealthChecks().AddDbContextCheck<RideContext>("ride-database", tags: ["ready"]);

// Runs after the server is listening, so /health answers during migration; /ready
// stays unhealthy until it finishes. Every IModule.OnStartup in this variant does nothing but
// run its own EF migration, and reads only app.ApplicationServices off the builder it's handed —
// so a throwaway ApplicationBuilder over the root provider satisfies the contract without
// dragging the real request pipeline into a hosted service.
builder.Services.AddStartupMigration(sp => Loader.Current.AddModules(new ApplicationBuilder(sp)));

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
  options.RouteTemplate = $"{modulePath}/swagger/{{documentName}}/swagger.json";
  options.PreSerializeFilters.Add((swagger, httpReq) =>
  {
    swagger.Servers = new List<OpenApiServer> { new() { Url = $"{scheme}://{httpReq.Host.Value}" } };
  });
});

app.UseSwaggerUI(options =>
{
  options.DocumentTitle = $"Voyager {moduleName} API";
  options.SwaggerEndpoint($"/{modulePath}/swagger/v1/swagger.json", $"Voyager {moduleName} API");
  options.RoutePrefix = $"{modulePath}/swagger";
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
  context.Response.Redirect($"/{modulePath}/swagger/");
  return Task.CompletedTask;
});

app.MapControllers();

foreach (var m in Loader.Current.Modules)
  m.UseEndpoints(app);

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
