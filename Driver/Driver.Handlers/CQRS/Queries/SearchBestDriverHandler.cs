using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Cache;
using Driver.Core.Dtos;
using Driver.Core.Enums;
using Driver.Handlers.Interfaces;
using Identity.Core.CQRS.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Driver.Core.CQRS.Queries
{
  /// <summary>
  /// Implements the driver matching algorithm.
  /// Ranks available drivers based on:
  /// - Geographic proximity to rider
  /// - Driver rating
  /// - Historical performance
  /// 
  /// Uses a weighted scoring system where:
  /// - Distance weight: Higher priority for closer drivers
  /// - Rating weight: Factors in driver quality
  /// - Cache implementation for performance optimization
  /// </summary>
  public class SearchBestDriverHandler(IDriverContext db, IMediator mediator, IConfiguration configuration, ICacheService cache) : IRequestHandler<SearchBestDriver, List<SearchBestDriverResponse>>
  {
    private const string CacheKeyPrefix = "drivers:available";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30); // Short to keep fresh the data
    public async Task<List<SearchBestDriverResponse>> Handle(SearchBestDriver request, CancellationToken cancellationToken)
    {
      var availableDrivers = await cache.GetOrCreateAsync(CacheKeyPrefix, async () =>
          await db.Drivers.AsNoTracking().Where(f => f.Status == DriverStatus.Available && f.LastLocation != null).ToListAsync(cancellationToken), CacheExpiration);

      var drivers = availableDrivers.Where(d => d.LastLocation!.Distance(request.Location) <= request.DistanceThresholdInMeters).ToList();
      if (drivers.Count == 0)
        return [];

      var driverIds = drivers.Select(d => d.Id).ToList();
      var userRatings = await GetDriverRatings(driverIds, cancellationToken);

      var result = (
        from driver in drivers
        let distance = driver.LastLocation!.Distance(request.Location)
        let driverRating = userRatings.GetValueOrDefault(driver.Id, 0.0)
        let normalizedDistance = distance / request.DistanceThresholdInMeters
        let normalizedRating = (driverRating - configuration.GetValue<double>("UserMinRating")) / (configuration.GetValue<double>("UserMaxRating") - configuration.GetValue<double>("UserMinRating"))
        let score = configuration.GetValue<double>("DistanceWeight") * normalizedDistance + configuration.GetValue<double>("RatingWeight") * (1 - normalizedRating)
        select new SearchBestDriverResponse
        {
          DriverId = driver.Id,
          Distance = distance,
          Score = score
        }).ToList();

      return [.. result.OrderBy(f => f.Score)];
    }

    private async Task<Dictionary<Guid, double>> GetDriverRatings(List<Guid> driverIds, CancellationToken cancellationToken)
    {
      var tasks = driverIds.Select(async id =>
      {
        var rating = await cache.GetOrCreateAsync($"driver:rating:{id}",
            async () => await mediator.Send(new GetUsersRatings { UserIds = [id] }, cancellationToken),
            TimeSpan.FromMinutes(15));
        return (id, rating?.FirstOrDefault().Value ?? 0.0);
      });

      var results = await Task.WhenAll(tasks);

      return results.ToDictionary(x => x.id, x => x.Item2);
    }
  }
}
