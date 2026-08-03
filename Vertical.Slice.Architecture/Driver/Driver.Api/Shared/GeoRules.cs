using System;
using FluentValidation;
using NetTopologySuite.Geometries;

namespace Driver.Api.Shared;

/// <summary>
/// Shared coordinate rule for every feature that accepts a caller-supplied location.
///
/// Nothing was checking these. Controllers carry no [ApiController] attribute and nullable
/// reference types are disabled project-wide, so ASP.NET Core adds no implicit [Required] and
/// performs no automatic model-state check: a body that simply omitted a location bound to null
/// and reached the handler, where writing it out threw and came back as a 500. An out-of-range
/// latitude got further still — SQL Server's geography type rejects it at INSERT time, which is
/// also a 500. Both are bad input and belong at 400.
///
/// Duplicated in Ride.Api rather than shared through Commons: each service stands on its own
/// and this is a dozen lines — see this variant's CLAUDE.md on preferring duplication over a
/// cross-service dependency.
/// </summary>
public static class GeoRules
{
  private const double MaxLatitude = 90;
  private const double MaxLongitude = 180;

  public static IRuleBuilderOptions<T, Point> ValidCoordinate<T>(this IRuleBuilder<T, Point> rule)
  {
    return rule
      .NotNull().WithMessage("coordinate_required")
      .Must(IsOnEarth).WithMessage("coordinate_out_of_range");
  }

  // X is longitude and Y is latitude — GeoJSON orders coordinates [lon, lat], which is the
  // opposite of how they are usually spoken, so the two are easy to swap. A swapped pair outside
  // ±90 latitude is exactly what this catches.
  private static bool IsOnEarth(Point point)
  {
    if (point == null)
      return true; // NotNull already reported it; don't stack a second message on the same field.

    if (double.IsNaN(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.X) || double.IsInfinity(point.Y))
      return false;

    return Math.Abs(point.Y) <= MaxLatitude && Math.Abs(point.X) <= MaxLongitude;
  }
}
