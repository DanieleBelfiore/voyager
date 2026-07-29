using System;
using System.Collections.Generic;
using MediatR;
using NetTopologySuite.Geometries;

namespace Driver.Module.Features.SearchBestDriver;

internal class SearchBestDriver : IRequest<List<SearchBestDriverResponse>>
{
  public Guid UserId { get; set; }
  public Point Location { get; set; }
  public int DistanceThresholdInMeters { get; set; }
}
