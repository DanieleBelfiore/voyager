using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Arbitrer;
using Hub.Api;
using Hub.Api.Middlewares;
using Hub.Application.Ports;
using Hub.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
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

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(opt =>
    opt.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true).AllowCredentials());
});

// Composition root: layers are wired explicitly here instead of being discovered at
// runtime by a plugin loader (contrast with Plugin.Microservices.CQRS's Common.Core.Loader).
builder.Services.AddHubInfrastructure();

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
  options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
  options.KeepAliveInterval = TimeSpan.FromSeconds(15);
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

builder.Services.AddTransient<QueryStringTokenMiddleware>();

var app = builder.Build();

// Only for development
const string scheme = "http";
app.UseDeveloperExceptionPage();

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

app.Run();
