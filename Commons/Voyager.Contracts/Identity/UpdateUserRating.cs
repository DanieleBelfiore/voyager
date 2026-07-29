using System;
using MediatR;

namespace Voyager.Contracts.Identity;

/// <summary>
/// Cross-service command: recompute a user's average rating, owned by the Identity bounded
/// context. Sent remotely by Ride after a driver or rider rates the other side of a completed ride.
/// </summary>
public class UpdateUserRating : IRequest<double>
{
  public Guid UserId { get; set; }
  public int Rating { get; set; }
  public int Rides { get; set; }
}
