using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Arbitrer;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenIddict.Validation.AspNetCore;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Swashbuckle.AspNetCore.SwaggerUI;
using Voyager.Shared.RateLimiting;
using Voyager.Shared.Validation;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(opt =>
    opt.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true).AllowCredentials());
});

// No repository, no event-publisher/rating/driver-location port: every feature's handler takes
// RideDbContext and IMediator directly — cross-service calls and event publishing both go
// through IMediator.Send/Publish, same object either way.
builder.Services.AddDbContext<RideDbContext>((provider, options) =>
{
  options.UseSqlServer(configuration.GetConnectionString("RideContext"), a => a.UseNetTopologySuite());
  options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
});
builder.Services.AddScoped<SlowQueryInterceptor>();

builder.Services.Configure<EtaConfig>(configuration);

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
  g.SwaggerDoc("v1", new OpenApiInfo { Title = "Voyager Ride API", Version = "v1" });
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

var apiAssembly = Assembly.GetExecutingAssembly();

builder.Services.AddMediatR(cfg =>
{
  cfg.RegisterServicesFromAssembly(apiAssembly);
  cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(apiAssembly);

builder.Services.AddArbitrer(options =>
{
  options.Behaviour = ArbitrerBehaviourEnum.ImplicitRemote;
  options.InferLocalRequests([apiAssembly]);
  options.InferLocalNotifications([apiAssembly]);
});

builder.Services.AddArbitrerRabbitMQMessageDispatcher(o =>
{
  configuration.GetSection("RabbitMQ").Bind(o);
  o.AutoDelete = false;
  o.Durable = true;
  o.ClientName = apiAssembly.FullName;
}).AddRabbitMQRequestManager();

builder.Services.AddHttpContextAccessor();

builder.Services.AddCustomRateLimiting(builder.Configuration);

var app = builder.Build();

// Only for development
const string scheme = "http";
app.UseDeveloperExceptionPage();

app.UseRouting();
app.UseSwagger(options =>
{
  options.RouteTemplate = "ride/swagger/{documentName}/swagger.json";
  options.PreSerializeFilters.Add((swagger, httpReq) =>
  {
    swagger.Servers = new List<OpenApiServer> { new() { Url = $"{scheme}://{httpReq.Host.Value}" } };
  });
});

app.UseSwaggerUI(options =>
{
  options.DocumentTitle = "Voyager Ride API";
  options.SwaggerEndpoint("/ride/swagger/v1/swagger.json", "Voyager Ride API");
  options.RoutePrefix = "ride/swagger";
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

using (var scope = app.Services.CreateScope())
{
  scope.ServiceProvider.GetRequiredService<RideDbContext>().Database.Migrate();
}

app.MapGet("/", context =>
{
  context.Response.Redirect("/ride/swagger/");
  return System.Threading.Tasks.Task.CompletedTask;
});

app.MapControllers();

app.Run();
