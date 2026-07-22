using System;
using System.Collections.Generic;
using Driver.Application.Dtos;
using MediatR;
using NetTopologySuite.Geometries;

namespace Driver.Application.CQRS.Queries;

public class SearchBestDriver : IRequest<List<SearchBestDriverResponse>>
{
  public Guid UserId { get; set; }
  public Point Location { get; set; }
  public int DistanceThresholdInMeters { get; set; }
}
