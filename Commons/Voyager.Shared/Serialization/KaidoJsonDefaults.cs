using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Voyager.Shared.Serialization;

/// <summary>
/// Newtonsoft's process-wide serializer settings, which are what Kaido uses on the wire.
/// </summary>
/// <remarks>
/// Kaido serialises cross-process request and notification payloads over RabbitMQ through
/// <see cref="JsonConvert.DefaultSettings"/> and exposes no per-call hook, so this is the only
/// place the wire format can be configured. Without <see cref="GeometryConverter"/> here, any
/// payload carrying a NetTopologySuite <c>Point</c> is written as the library's internal
/// <c>CoordinateSequence</c>/<c>Factory</c> graph and then fails on the receiving end —
/// "Unable to find a constructor to use for type NetTopologySuite.Geometries.Point".
///
/// That takes out the whole real-time path in the service-per-process variants: Hub asks Ride for
/// the active ride (pickup and dropoff are Points) before admitting a caller to a ride group, and
/// relays driver locations. Configuring the MVC and SignalR serialisers is not enough — those
/// cover HTTP and the hub connection, not the broker.
///
/// Call before the host is built: the settings must be in place before the first payload moves.
/// </remarks>
public static class KaidoJsonDefaults
{
  public static void Apply()
  {
    JsonConvert.DefaultSettings = () => new JsonSerializerSettings
    {
      Converters = { new StringEnumConverter(), new GeometryConverter() }
    };
  }
}
