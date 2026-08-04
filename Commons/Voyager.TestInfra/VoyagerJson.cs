using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NetTopologySuite.IO.Converters;

namespace Voyager.TestInfra;

/// <summary>
/// The wire format the services actually speak: Newtonsoft with a string enum converter and
/// NetTopologySuite's own <see cref="GeometryConverter"/>.
/// </summary>
/// <remarks>
/// Not a convenience. A <c>Point</c> serialised by anything else does not round-trip — plain
/// serialisers walk NTS's internal CoordinateSequence/Factory graph rather than writing GeoJSON —
/// so a test that posts a location with the wrong settings fails on deserialisation and says
/// nothing about the endpoint it meant to exercise.
/// </remarks>
public static class VoyagerJson
{
  public static readonly JsonSerializerSettings Settings = new()
  {
    Converters = { new StringEnumConverter(), new GeometryConverter() },
    NullValueHandling = NullValueHandling.Ignore
  };

  public static StringContent Content(object payload)
  {
    return new StringContent(JsonConvert.SerializeObject(payload, Settings), Encoding.UTF8, "application/json");
  }

  public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
  {
    return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), Settings);
  }
}
