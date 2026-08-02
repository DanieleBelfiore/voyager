using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Driver.Tests;

/// <summary>
/// Driver registration must stay gated on the is_driver claim, not plain authentication. A Driver
/// row is what puts an id into SearchBestDriver's candidate pool, so an [Authorize]-only endpoint
/// let any authenticated rider self-register, publish a location, and be ranked into real ride
/// requests they can never accept — AcceptRide checks the same claim, and there is no offer
/// timeout to recover a ride stuck against such a phantom driver.
/// </summary>
public class AddDriverAuthorizationTests
{
  [Fact]
  public void AddDriverEndpoint_RequiresTheDriverPolicy()
  {
    // Arrange
    var action = typeof(Driver.Module.Features.AddDriver.AddDriverController).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

    // Act
    var policies = action!.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
      .Concat(action.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
      .Select(a => a.Policy)
      .ToList();

    // Assert
    Assert.Contains("RequireDriver", policies);
  }
}
