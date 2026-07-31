using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Arbitrer;
using Driver.Adapters.Secondary.DependencyInjection;
using Driver.Core.Ports.Primary;
using Driver.Core.UseCases;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;
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

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(opt =>
    opt.AllowAnyMethod().AllowAnyHeader().WithOrigins(allowedOrigins).AllowCredentials());
});

// Composition root: wires secondary adapters into Core's secondary ports, and primary ports
// directly to their use case implementations. Controllers depend on the primary ports
// (constructor injection), not on IMediator — see Driver.Api/Controllers/DriversController.cs.
builder.Services.AddDriverSecondaryAdapters(configuration);

builder.Services.AddScoped<IAddDriverUseCase, AddDriverUseCase>();
builder.Services.AddScoped<IUpdateAvailabilityUseCase, UpdateAvailabilityUseCase>();
builder.Services.AddScoped<IUpdateLocationUseCase, UpdateLocationUseCase>();
builder.Services.AddScoped<IGetDriverStatusUseCase, GetDriverStatusUseCase>();
builder.Services.AddScoped<ISearchBestDriverUseCase, SearchBestDriverUseCase>();

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

var coreAssembly = typeof(AddDriverUseCase).Assembly;

// MediatR still scans Core for IRequestHandler<T> implementations — every use case implements
// one via its primary port (IAddDriverUseCase : IRequestHandler<AddDriver>, etc.) — so this is
// what makes them reachable as Arbitrer's remote entry point, independent of the direct
// injection above.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(coreAssembly));

builder.Services.AddArbitrer(options =>
{
  options.Behaviour = ArbitrerBehaviourEnum.ImplicitRemote;
  options.InferLocalRequests([coreAssembly]);
  options.InferLocalNotifications([coreAssembly]);
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

var app = builder.Build();

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

app.Services.MigrateDriverDatabase();

app.MapGet("/", context =>
{
  context.Response.Redirect("/driver/swagger/");
  return System.Threading.Tasks.Task.CompletedTask;
});

app.MapControllers();

app.Run();
