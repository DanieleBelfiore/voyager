using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voyager.TestInfra;

/// <summary>
/// Drives the real registration and password-grant endpoints. Deliberately not a shortcut that
/// mints a principal in-process: the token these tests carry has to be one the shipped Identity
/// service actually issued, or the auth pipeline is not what is under test.
/// </summary>
public static class VoyagerAuth
{
  public const string ClientId = "voyager_app";
  public const string DefaultPassword = "Aa123456!";

  /// <summary>
  /// Registers a fresh user and returns their access token. The name is unique per call because
  /// the identity catalogue is never truncated between tests — see <see cref="DatabaseResetter"/>.
  /// </summary>
  public static async Task<VoyagerTestUser> RegisterAsync(HttpClient identityClient, bool isDriver)
  {
    var email = $"{(isDriver ? "driver" : "rider")}-{Guid.NewGuid():N}@voyager.test";

    var registration = await identityClient.PostAsJsonAsync("connect/register", new
    {
      email,
      password = DefaultPassword,
      confirmPassword = DefaultPassword,
      firstName = isDriver ? "Test" : "Test",
      lastName = isDriver ? "Driver" : "Rider",
      isDriver
    });

    await EnsureSuccess(registration, "registration");

    var accessToken = await GetAccessTokenAsync(identityClient, email, DefaultPassword);

    return new VoyagerTestUser(ReadSubject(accessToken), email, accessToken);
  }

  public static async Task<string> GetAccessTokenAsync(HttpClient identityClient, string userName, string password)
  {
    var response = await identityClient.PostAsync("connect/token", new FormUrlEncodedContent(
      new Dictionary<string, string>
      {
        ["grant_type"] = "password",
        ["client_id"] = ClientId,
        ["username"] = userName,
        ["password"] = password
      }));

    await EnsureSuccess(response, "token request");

    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    return payload.RootElement.GetProperty("access_token").GetString()
           ?? throw new InvalidOperationException("Token response carried no access_token.");
  }

  public static void Authenticate(this HttpClient client, VoyagerTestUser user)
  {
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
  }

  /// <summary>
  /// Reads <c>sub</c> straight out of the JWT payload. Access-token encryption is disabled in
  /// every variant (see each Identity setup), so the token is a plain JWS and no key material is
  /// needed to read a claim the test already trusts — the server validated it.
  /// </summary>
  private static Guid ReadSubject(string accessToken)
  {
    var segments = accessToken.Split('.');
    if (segments.Length < 2)
      throw new InvalidOperationException("Access token is not a JWT; cannot read the subject claim.");

    var payload = segments[1].Replace('-', '+').Replace('_', '/');
    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

    using var document = JsonDocument.Parse(Convert.FromBase64String(payload));

    return Guid.Parse(document.RootElement.GetProperty("sub").GetString()!);
  }

  private static async Task EnsureSuccess(HttpResponseMessage response, string what)
  {
    if (response.IsSuccessStatusCode)
      return;

    throw new InvalidOperationException(
      $"Voyager {what} failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }
}

public record VoyagerTestUser(Guid Id, string Email, string AccessToken);
