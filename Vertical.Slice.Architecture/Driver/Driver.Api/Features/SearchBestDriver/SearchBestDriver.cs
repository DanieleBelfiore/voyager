using System;
using System.Collections.Generic;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Driver.Api.Features.SearchBestDriver;

public class SearchBestDriver : IRequest<List<SearchBestDriverResponse>>
{
  public Guid UserId { get; set; }
  public Point Location { get; set; }
  public int DistanceThresholdInMeters { get; set; }
}
