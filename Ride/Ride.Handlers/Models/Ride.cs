using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Core.Enums;

namespace Ride.Handlers.Models
{
  [Table("Rides")]
  [Index(nameof(Status), nameof(UserId), nameof(DriverId), Name = "IX_Rides_Status_UserId_DriverId")]
  [Index(nameof(UserId), nameof(Status), nameof(RequestedAt), Name = "IX_Rides_UserId_Status_RequestedAt", IsDescending = [false, false, true])]
  [Index(nameof(DriverId), nameof(Status), nameof(RequestedAt), Name = "IX_Rides_DriverId_Status_RequestedAt", IsDescending = [false, false, true])]
  public class Ride
  {
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid DriverId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public double? Price { get; set; }
    public RideStatus Status { get; set; }
    [StringLength(128)]
    public string CancellationReason { get; set; }
    public Point PickupLocation { get; set; }
    [StringLength(64)]
    public string PickupLocationGeoJSON { get; set; }
    public Point DropoffLocation { get; set; }
    [StringLength(64)]
    public string DropoffLocationGeoJSON { get; set; }
    public Point? LastLocation { get; set; }
    [StringLength(64)]
    public string LastLocationGeoJSON { get; set; }
    public DateTime LastUpdateDate { get; set; } = DateTime.UtcNow;
  }
}
