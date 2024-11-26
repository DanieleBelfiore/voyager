using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Arbitrer;
using Common.Core;
using Hub.API;
using Hub.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var hostingEnvironment = builder.Environment;

Loader.Current.Directories.Add(Directory.GetCurrentDirectory());
Loader.Current.Compose();

var module = configuration["ModuleName"];
var moduleName = string.Concat(module!.Select((c, i) => i > 0 && char.IsUpper(c) ? $" {c}" : c.ToString()));
var modulePath = string.Concat(module!.Select((c, i) =>
  i > 0 && char.IsUpper(c) ? $"-{char.ToLower(c)}" : char.ToLower(c).ToString()));

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(opt =>
    opt.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true).AllowCredentials());
});

Loader.Current.ConfigureServices(builder.Services, configuration, hostingEnvironment);

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

  g.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }, Scheme = "oauth2", Name = "Bearer", In = ParameterLocation.Header }, new List<string>() } });

  g.CustomSchemaIds(x => x.FullName);

  g.MapType<object>(() => new OpenApiSchema { Type = "object" });

  g.MapType<Point>(() => new OpenApiSchema
  {
    Type = "object",
    Properties = new Dictionary<string, OpenApiSchema>
    {
      ["type"] = new() { Type = "string", Default = new OpenApiString("Point") },
      ["coordinates"] = new()
      {
        Type = "array",
        Items = new OpenApiSchema { Type = "number", Format = "double" },
        MinItems = 2,
        MaxItems = 2
      }
    },
    Required = new HashSet<string> { "type", "coordinates" }
  });
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

builder.Services.AddTransient<QueryStringTokenMiddleware>();

var app = builder.Build();

// Only for development
var scheme = "http";
app.UseDeveloperExceptionPage();

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

app.UseMiddleware<QueryStringTokenMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

Loader.Current.AddModules(app);

app.MapGet("/", context =>
{
  context.Response.Redirect($"/{modulePath}/swagger/");
  return Task.CompletedTask;
});

app.MapHub<VoyagerHub>("/voyagerhub");

foreach (var m in Loader.Current.Modules)
  m.UseEndpoints(app);

app.Run();
