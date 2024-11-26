using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Driver.Core.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Driver.Handlers.Models
{
  [Table("Drivers")]
  [Index(nameof(Status), nameof(LastUpdateDate), Name = "IX_Drivers_Status_LastUpdateDate", IsDescending = [false, true])]
  public class Driver
  {
    [Key]
    public Guid Id { get; set; }
    public DriverStatus Status { get; set; } = DriverStatus.Available;
    public Point? LastLocation { get; set; }
    [StringLength(64)]
    public string LastLocationGeoJSON { get; set; }
    public DateTime LastUpdateDate { get; set; } = DateTime.UtcNow;
  }
}
