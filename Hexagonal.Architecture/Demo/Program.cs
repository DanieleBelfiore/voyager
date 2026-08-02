using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;

namespace Demo;

public class Program
{
  private static readonly HttpClient _httpClient = new();
  private static HubConnection _hubConnection;
  private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();

  private const string HUB_API = "http://localhost:5200/voyagerhub";
  private const string IDENTITY_API = "http://localhost:5201";
  private const string DRIVER_API = "http://localhost:5202";
  private const string RIDE_API = "http://localhost:5203";

  private static async Task Main()
  {
    try
    {
      _logger.LogInformation("Starting Voyager Demo...");

      // 1. Register and authenticate users
      _logger.LogInformation("Registering and authenticating users...");

      var riderToken = await RegisterAndAuthenticateUser($"rider-{Guid.NewGuid()}@test.com", "Rider123!", false);
      var driverToken = await RegisterAndAuthenticateUser($"driver-{Guid.NewGuid()}@test.com", "Driver123!", true);

      // Configure HTTP client with rider token initially
      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", riderToken);

      // 2. Set up SignalR connection for real-time updates. Authenticated as the driver, since
      // this connection also submits location updates over the socket (see UpdateDriverLocation)
      // — it doubles as a stand-in for the rider's client too, for the events this demo listens to.
      _hubConnection = new HubConnectionBuilder()
        .WithUrl(HUB_API, options =>
        {
          options.AccessTokenProvider = () => Task.FromResult(driverToken);
        })
        .AddNewtonsoftJsonProtocol(options =>
        {
          // Must match the server's GeometryConverter registration (Hub/Program.cs) so a Point
          // argument (UpdateDriverLocation) and Point payloads (SendToRiderNewDriverLocation)
          // round-trip as GeoJSON instead of NTS's internal object graph.
          options.PayloadSerializerSettings.Converters.Add(new GeometryConverter());
        })
        .WithAutomaticReconnect()
        .Build();

      SetupSignalRHandlers();

      _hubConnection.Closed += error =>
      {
        _logger.LogError(error, "SignalR connection closed");

        return Task.CompletedTask;
      };

      try
      {
        await _hubConnection.StartAsync();

        _logger.LogInformation("SignalR connection established");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to connect to SignalR hub. URL: {url}", HUB_API);
        throw;
      }

      // 3. Update driver location and status
      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

      var driverLocation = new Point(12.4964, 41.9028) { SRID = 4326 }; // Rome coordinates
      await UpdateDriverLocation(driverLocation);
      await UpdateDriverStatus(true);

      _logger.LogInformation("Driver status and location updated");

      // 4. Search for nearby drivers and request ride
      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", riderToken);

      var riderLocation = new Point(12.4963, 41.9027) { SRID = 4326 }; // Near Rome coordinates
      var drivers = await SearchNearbyDrivers(riderLocation);

      if (drivers == null || drivers.Count == 0)
      {
        _logger.LogError("No drivers found nearby");
        return;
      }

      var selectedDriver = drivers.First();
      var destination = new Point(12.4920, 41.8902) { SRID = 4326 }; // Colosseum coordinates

      var rideId = await RequestRide(selectedDriver.DriverId, riderLocation, destination);
      _logger.LogInformation("Ride requested. ID: {RideId}", rideId);

      // 5. Driver accepts and starts ride
      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

      await AcceptRide(rideId);
      _logger.LogInformation("Ride accepted by driver");

      // Only reachable once the ride is DriverAssigned/InProgress for this caller — see
      // VoyagerHub.JoinRideGroup — which is why this can't happen any earlier than here.
      await _hubConnection.InvokeAsync("JoinRideGroup", rideId.ToString());
      _logger.LogInformation("Joined ride group for real-time updates");

      // 6. Simulate ride progress with location updates, sent over the hub connection (not
      // REST) so the WebSocket path actually carries traffic both ways, not just server->client.
      var progress = SimulateRideProgress(riderLocation, destination);
      foreach (var location in progress)
      {
        await UpdateDriverLocation(location);

        _logger.LogInformation("Driver location updated");

        await Task.Delay(2000); // Simulate movement every 2 seconds
      }

      // 7. Start and Complete ride
      await StartRide(rideId, riderLocation);
      _logger.LogInformation("Ride started");

      await CompleteRide(rideId, destination);
      _logger.LogInformation("Ride completed");

      // 8. Rate each other
      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", riderToken);
      await RateDriver(rideId, 5);

      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);
      await RateRider(rideId, 5);

      _logger.LogInformation("Ratings submitted");

      // Wait for a moment to receive any final real-time updates
      await Task.Delay(2000);

      _logger.LogInformation("Demo completed successfully!");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred during demo");
    }
  }

  private static async Task<string> RegisterAndAuthenticateUser(string email, string password, bool isDriver)
  {
    // Register user
    var registerData = new
    {
      email,
      password,
      confirmPassword = password,
      firstName = isDriver ? "Demo Driver" : "Demo Rider",
      lastName = "Test",
      isDriver
    };

    var response = await _httpClient.PostAsync(
      $"{IDENTITY_API}/connect/register",
      new StringContent(JsonConvert.SerializeObject(registerData), Encoding.UTF8, "application/json")
    );

    if (!response.IsSuccessStatusCode)
    {
      var error = await response.Content.ReadAsStringAsync();
      _logger.LogError("Registration failed: {Error}", error);
      throw new Exception("Registration failed");
    }

    // Authenticate and get token
    var tokenRequest = new Dictionary<string, string>
    {
      ["grant_type"] = "password",
      ["client_id"] = "voyager_app",
      ["username"] = email,
      ["password"] = password
    };

    response = await _httpClient.PostAsync(
      $"{IDENTITY_API}/connect/token",
      new FormUrlEncodedContent(tokenRequest)
    );

    if (!response.IsSuccessStatusCode)
    {
      throw new Exception("Authentication failed");
    }

    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(
      await response.Content.ReadAsStringAsync()
    );

    return tokenResponse?.AccessToken ??
           throw new Exception("Token not received");
  }

  private static void SetupSignalRHandlers()
  {
    // Driver-facing events — deliverable to this connection now that NewRideRequested routes
    // to the driver's own user_{id} group instead of a ride_{id} group nobody has joined yet.
    _hubConnection.On<Guid>("SendToDriverNewRideRequest", rideId =>
    {
      _logger.LogInformation("New ride request received for ride {RideId}", rideId);
    });

    _hubConnection.On<Guid>("SendToDriverRideCancel", rideId =>
    {
      _logger.LogInformation("Ride {RideId} was cancelled", rideId);
    });

    _hubConnection.On<int>("SendToDriverNewRateReceived", rating =>
    {
      _logger.LogInformation("Received {Rating} stars from rider", rating);
    });

    // Rider-facing events — only deliverable to this connection from the point it joins
    // ride_{RideId} onward (see the JoinRideGroup call after AcceptRide).
    _hubConnection.On<Point>("SendToRiderNewDriverLocation", location =>
    {
      _logger.LogInformation("Driver location updated: ({Lat}, {Lon})", location.Y, location.X);
    });

    _hubConnection.On<EtaDto>("SendToRiderNewETA", eta =>
    {
      _logger.LogInformation("New ETA: {Minutes} min, {Distance} km", eta.EstimatedArrivalMinutes, eta.DistanceKm);
    });

    _hubConnection.On<Guid>("SendToRiderDriverArrival", rideId =>
    {
      _logger.LogInformation("Driver has arrived for ride {RideId}", rideId);
    });

    _hubConnection.On<Guid>("SendToRiderRideAccepted", rideId =>
    {
      _logger.LogInformation("Ride {RideId} accepted by driver", rideId);
    });

    _hubConnection.On<Guid>("SendToRiderRideCompleted", rideId =>
    {
      _logger.LogInformation("Ride {RideId} completed", rideId);
    });

    _hubConnection.On<int>("SendToRiderNewRateReceived", rating =>
    {
      _logger.LogInformation("Received {Rating} stars from driver", rating);
    });
  }

  // Sent over the SignalR hub connection (VoyagerHub.UpdateDriverLocation), not REST — this is
  // the driver-originated leg of the real-time path the mandate asks for.
  private static async Task UpdateDriverLocation(Point location)
  {
    await _hubConnection.InvokeAsync("UpdateDriverLocation", location);
  }

  private static async Task UpdateDriverStatus(bool available)
  {
    var response = await _httpClient.PutAsync(
      $"{DRIVER_API}/api/v1/drivers/availability",
      new StringContent(JsonConvert.SerializeObject(new { status = available ? 1 : 0 }),
        Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();
  }

  private static async Task<List<DriverResponse>> SearchNearbyDrivers(Point location)
  {
    var response = await _httpClient.PostAsync(
      $"{DRIVER_API}/api/v1/drivers/search",
      new StringContent(JsonConvert.SerializeObject(new
      {
        // GeoJSON coordinate order is [longitude, latitude] — Point.X is longitude, Point.Y is
        // latitude. Reversing this doesn't fail loudly: both write and read paths agree on the
        // (wrong) order, so it round-trips silently into the wrong hemisphere.
        location = new LocationDto { Coordinates = [location.X, location.Y] },
        distanceThresholdInKm = 5
      }), Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();

    return JsonConvert.DeserializeObject<List<DriverResponse>>(
      await response.Content.ReadAsStringAsync()
    ) ?? [];
  }

  private static async Task<Guid> RequestRide(Guid driverId, Point pickup, Point dropoff)
  {
    var response = await _httpClient.PostAsync(
      $"{RIDE_API}/api/v1/rides",
      new StringContent(JsonConvert.SerializeObject(new
      {
        driverId,
        pickupLocation = new LocationDto { Coordinates = [pickup.X, pickup.Y] },
        dropoffLocation = new LocationDto { Coordinates = [dropoff.X, dropoff.Y] }
      }), Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();

    var ride = JsonConvert.DeserializeObject<RideResponse>(
      await response.Content.ReadAsStringAsync()
    );

    return ride?.Id ?? Guid.Empty;
  }

  private static async Task AcceptRide(Guid rideId)
  {
    var response = await _httpClient.PutAsync(
      $"{RIDE_API}/api/v1/rides/{rideId}/accept",
      null
    );

    response.EnsureSuccessStatusCode();
  }

  private static async Task StartRide(Guid rideId, Point location)
  {
    var response = await _httpClient.PutAsync(
      $"{RIDE_API}/api/v1/rides/{rideId}/start",
      new StringContent(JsonConvert.SerializeObject(new { location = new LocationDto { Coordinates = [location.X, location.Y] } }), Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();
  }

  private static List<Point> SimulateRideProgress(Point start, Point end)
  {
    const int steps = 5;
    var points = new List<Point>();

    for (var i = 1; i <= steps; i++)
    {
      var fraction = i / (double)steps;
      var lat = start.Y + (end.Y - start.Y) * fraction;
      var lon = start.X + (end.X - start.X) * fraction;
      points.Add(new Point(lon, lat) { SRID = 4326 });
    }

    return points;
  }

  private static async Task CompleteRide(Guid rideId, Point location)
  {
    var response = await _httpClient.PutAsync(
      $"{RIDE_API}/api/v1/rides/{rideId}/complete",
      new StringContent(JsonConvert.SerializeObject(new
      {
        location = new LocationDto { Coordinates = [location.X, location.Y] }
      }), Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();
  }

  private static async Task RateDriver(Guid rideId, int rating)
  {
    var response = await _httpClient.PutAsync(
      $"{RIDE_API}/api/v1/rides/{rideId}/rate/driver",
      new StringContent(JsonConvert.SerializeObject(new { rating }),
        Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();
  }

  private static async Task RateRider(Guid rideId, int rating)
  {
    var response = await _httpClient.PutAsync(
      $"{RIDE_API}/api/v1/rides/{rideId}/rate",
      new StringContent(JsonConvert.SerializeObject(new { rating }),
        Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();
  }

  private class TokenResponse
  {
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
  }

  // ReSharper disable once ClassNeverInstantiated.Local
  private class DriverResponse
  {
    public Guid DriverId { get; set; }
    public double Distance { get; set; }
    public double Score { get; set; }
  }

  private class RideResponse
  {
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid DriverId { get; set; }
  }

  private class LocationDto
  {
    [JsonProperty("type")]
    public string Type { get; set; } = "Point";
    [JsonProperty("coordinates")]
    public double[] Coordinates { get; set; }
  }

  // ReSharper disable once ClassNeverInstantiated.Local
  private class EtaDto
  {
    public int? EstimatedArrivalMinutes { get; set; }
    public double? DistanceKm { get; set; }
  }
}
