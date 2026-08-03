using System;
using NetTopologySuite.Geometries;
using Voyager.Errors;

namespace Driver.Core.Validation;

/// <summary>
/// Rejects a caller-supplied coordinate before it reaches an entity or the persistence adapter.
///
/// Nothing else was checking. Controllers carry no [ApiController] attribute and nullable
/// reference types are disabled project-wide, so ASP.NET Core adds no implicit [Required] and
/// performs no automatic model-state check: a body that simply omitted a location bound to null
/// and reached the handler, where writing it out threw and came back as a 500. An out-of-range
/// latitude got further still — SQL Server's geography type rejects it at INSERT time, which is
/// also a 500. Both are bad input and belong at 400.
///
/// It is duplicated per service rather than shared: each hexagon stands on its own and a
/// cross-service helper would couple two cores that are meant to be independent.
/// </summary>
public static class GeoGuard
{
  private const double MaxLatitude = 90;
  private const double MaxLongitude = 180;

  /// <param name="point">The caller-supplied point.</param>
  /// <param name="field">Field name used to build the error code, e.g. "pickup_location".</param>
  public static Point Required(Point point, string field)
  {
    if (point == null)
      throw new InvalidInputException($"{field}_required");

    // X is longitude and Y is latitude — GeoJSON orders coordinates [lon, lat], which is the
    // opposite of how they are usually spoken, so the two are easy to swap. A swapped pair
    // outside ±90 latitude is exactly what this catches.
    if (double.IsNaN(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.X) || double.IsInfinity(point.Y))
      throw new InvalidInputException($"{field}_out_of_range");

    if (Math.Abs(point.Y) > MaxLatitude || Math.Abs(point.X) > MaxLongitude)
      throw new InvalidInputException($"{field}_out_of_range");

    return point;
  }
}
