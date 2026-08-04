using System;
using Hikyaku;

namespace Voyager.Contracts.Identity;

/// <summary>
/// Cross-service command: recompute a user's average rating, owned by the Identity bounded
/// context. Sent remotely by Ride after a driver or rider rates the other side of a completed
/// ride. The ratings-received count is tracked by Identity itself (User.RatingsCount) rather
/// than trusted from the caller — a caller-supplied count previously conflated "rides completed"
/// with "ratings actually received", which aren't the same number.
/// </summary>
public class UpdateUserRating : IRequest<double>
{
  public Guid UserId { get; set; }
  public int Rating { get; set; }
}
