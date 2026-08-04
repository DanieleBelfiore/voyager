using System.Net.Http;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Voyager.TestInfra;

public static class HttpAssertions
{
  /// <summary>
  /// Like <c>EnsureSuccessStatusCode</c>, but reports the response body.
  /// </summary>
  /// <remarks>
  /// These tests drive several requests in sequence, and the built-in version says only
  /// "Response status code does not indicate success: 500" — which request, and why, is left to
  /// guesswork against a host whose logs the runner discards. The body carries the problem
  /// details the API already produces.
  /// </remarks>
  public static async Task<HttpResponseMessage> ShouldSucceed(this Task<HttpResponseMessage> pending, string what = null)
  {
    var response = await pending;

    if (response.IsSuccessStatusCode)
      return response;

    throw new XunitException(
      $"{what ?? $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}"} " +
      $"returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }
}
