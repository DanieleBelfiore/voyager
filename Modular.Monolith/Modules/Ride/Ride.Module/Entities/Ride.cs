using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Voyager.Errors;

namespace Ride.Module.Entities;

internal class Ride
{
  private static readonly List<RideStatus> CancellableStatuses = [RideStatus.Requested, RideStatus.DriverAssigned];
  private static readonly List<RideStatus> ActiveStatuses = [RideStatus.DriverAssigned, RideStatus.InProgress];

  public const int MinRating = 1;
  public const int MaxRating = 5;

  public Guid Id { get; private set; } = Guid.NewGuid();
  public Guid UserId { get; private set; }
  public Guid DriverId { get; private set; }
  public DateTime RequestedAt { get; private set; } = DateTime.UtcNow;
  public DateTime? StartAt { get; private set; }
  public DateTime? EndAt { get; private set; }
  public double? Price { get; private set; }
  public RideStatus Status { get; private set; }
  public string CancellationReason { get; private set; }
  public Point PickupLocation { get; private set; }
  public Point DropoffLocation { get; private set; }
  public Point LastLocation { get; private set; }
  public DateTime LastUpdateDate { get; private set; } = DateTime.UtcNow;
  public byte[] RowVersion { get; private set; }
  public int? DriverRating { get; private set; }
  public int? RiderRating { get; private set; }

  private Ride()
  {
    // EF Core
  }

  public Ride(Guid userId, Guid driverId, Point pickupLocation, Point dropoffLocation)
  {
    UserId = userId;
    DriverId = driverId;
    PickupLocation = pickupLocation;
    DropoffLocation = dropoffLocation;
    Status = RideStatus.Requested;
  }

  public void Accept(Guid driverId)
  {
    // The rider already picked a driver at RequestRide time (DriverId is set in the
    // constructor) — Accept only confirms it's that same driver calling, never reassigns it.
    if (DriverId != driverId)
      throw new UnauthorizedAccessException("not_ride_participant");

    if (Status != RideStatus.Requested)
      throw new ConflictException("operation_not_permitted");

    Status = RideStatus.DriverAssigned;
    LastUpdateDate = DateTime.UtcNow;
  }

  /// <summary>
  /// Cancels the ride and reports whether it was the one holding the driver, i.e. whether the
  /// caller must release them back to Available.
  ///
  /// Only a DriverAssigned ride ever moved the driver to OnRide. A ride still at Requested never
  /// touched their status, and nothing constrains how many riders hold a Requested ride against
  /// the same available driver — the unique index is per-rider (IX_Rides_UserId_ActiveOnly), not
  /// per-driver. So releasing unconditionally let a stale request being cancelled flip a driver
  /// who is already mid-trip on someone else's ride back into the matching pool.
  /// </summary>
  public bool Cancel(string cancellationReason)
  {
    if (!CancellableStatuses.Contains(Status))
      throw new ConflictException("operation_not_permitted");

    var heldTheDriver = Status == RideStatus.DriverAssigned;

    Status = RideStatus.Cancelled;
    CancellationReason = cancellationReason;
    LastUpdateDate = DateTime.UtcNow;
    EndAt = LastUpdateDate;

    return heldTheDriver;
  }

  public void Start(Point location)
  {
    if (Status != RideStatus.DriverAssigned)
      throw new ConflictException("operation_not_permitted");

    Status = RideStatus.InProgress;
    LastLocation = location;
    LastUpdateDate = DateTime.UtcNow;
    StartAt = LastUpdateDate;
  }

  /// <summary>
  /// Records where the driver currently is. Called for every position report between accept and
  /// completion, which is what keeps GET /rides/{id}/location honest — writing LastLocation only
  /// at Start and Complete left it pinned to the pickup point for the entire trip.
  ///
  /// A report for a ride that is no longer active is ignored rather than rejected: position
  /// updates are fire-and-forget and can legitimately land just after a Complete or Cancel.
  /// </summary>
  /// <summary>
  /// True once the trip itself is under way, which is when the driver stops heading for the
  /// pickup and starts heading for the dropoff — anything measuring "distance to destination"
  /// has to switch endpoints here.
  ///
  /// A method rather than a property so EF Core never tries to map it.
  /// </summary>
  public bool HasStarted() => Status == RideStatus.InProgress;

  public void TrackLocation(Point location)
  {
    if (!ActiveStatuses.Contains(Status))
      return;

    LastLocation = location;
    LastUpdateDate = DateTime.UtcNow;
  }

  public void Complete(Point location, double price)
  {
    if (Status != RideStatus.InProgress)
      throw new ConflictException("operation_not_permitted");

    Status = RideStatus.Completed;
    LastLocation = location;
    LastUpdateDate = DateTime.UtcNow;
    EndAt = LastUpdateDate;
    Price = price;
  }

  /// <summary>
  /// The rider rates the driver. Recorded on the ride itself, not just forwarded to Identity,
  /// because the value feeds a running average there: without a persisted marker the same ride
  /// could be rated over and over, and each replay permanently moves the driver's score and the
  /// matching rank built on top of it.
  /// </summary>
  public void RateDriver(int rating)
  {
    GuardRatable(rating);

    if (DriverRating.HasValue)
      throw new ConflictException("ride_already_rated");

    DriverRating = rating;
    LastUpdateDate = DateTime.UtcNow;
  }

  /// <summary>The driver rates the rider. Same one-shot rule, mirrored.</summary>
  public void RateRider(int rating)
  {
    GuardRatable(rating);

    if (RiderRating.HasValue)
      throw new ConflictException("ride_already_rated");

    RiderRating = rating;
    LastUpdateDate = DateTime.UtcNow;
  }

  private void GuardRatable(int rating)
  {
    // Only a trip that actually happened can be rated — otherwise a rider could request a ride
    // and immediately rate the driver down without ever taking it.
    if (Status != RideStatus.Completed)
      throw new ConflictException("operation_not_permitted");

    if (rating is < MinRating or > MaxRating)
      throw new InvalidInputException("rating_out_of_range");
  }
}
