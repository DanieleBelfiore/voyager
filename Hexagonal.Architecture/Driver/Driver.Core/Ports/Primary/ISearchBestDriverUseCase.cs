using System;
using System.Collections.Generic;
using Driver.Core.Dtos;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Driver.Core.Ports.Primary;

public class SearchBestDriver : IRequest<List<SearchBestDriverResponse>>
{
  public Guid UserId { get; set; }
  public Point Location { get; set; }
  public int DistanceThresholdInMeters { get; set; }
}

public interface ISearchBestDriverUseCase : IRequestHandler<SearchBestDriver, List<SearchBestDriverResponse>>;
