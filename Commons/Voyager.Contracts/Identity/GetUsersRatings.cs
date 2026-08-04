using System;
using System.Collections.Generic;
using Hikyaku;

namespace Voyager.Contracts.Identity;

/// <summary>
/// Cross-service query: average rating per user, owned by the Identity bounded context.
/// Consumed remotely (over the message bus) by any service that needs it for scoring/matching.
/// </summary>
public class GetUsersRatings : IRequest<Dictionary<Guid, double>>
{
  public List<Guid> UserIds { get; set; }
}
