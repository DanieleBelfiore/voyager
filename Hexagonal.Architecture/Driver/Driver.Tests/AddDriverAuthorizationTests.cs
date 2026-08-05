using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Driver.Tests;

/// <summary>
/// Driver registration must stay gated on the is_driver claim, not plain authentication. A Driver
/// row is what puts an id into SearchBestDriver's candidate pool, so an [Authorize]-only endpoint
/// let a rider account self-register, publish a location, and be ranked into real ride requests it
/// can never accept — AcceptRide checks the same claim, and there is no offer timeout to recover a
/// ride stuck against such a phantom driver. The claim marks the account type chosen at
/// registration, not a privilege granted by anyone: registering as a driver is self-service, so
/// this separates the rider and driver flows rather than keeping anyone out.
///
/// This is a wiring test — it asserts only that the attribute is present. That the gate actually
/// turns a rider token away is covered end to end in the integration AuthGateTests.
/// </summary>
public class AddDriverAuthorizationTests
{
  [Fact]
  public void AddDriverEndpoint_RequiresTheDriverPolicy()
  {
    // Arrange
    var action = typeof(Driver.Api.Controllers.DriversController).GetMethod("AddDriver", BindingFlags.Public | BindingFlags.Instance);

    // Act
    var policies = action!.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
      .Concat(action.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
      .Select(a => a.Policy)
      .ToList();

    // Assert
    Assert.Contains("RequireDriver", policies);
  }
}
