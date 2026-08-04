using System;
using Hikyaku;

namespace Voyager.Contracts.Driver;

/// <summary>
/// Cross-service command: register a driver record, owned by the Driver bounded context.
/// Sent remotely by Identity right after user registration when the new user opts in as a driver.
/// </summary>
public class AddDriver : IRequest
{
  public Guid DriverId { get; set; }
}
