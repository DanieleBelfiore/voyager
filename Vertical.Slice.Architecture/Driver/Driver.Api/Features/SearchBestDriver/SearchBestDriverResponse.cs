using System;

namespace Driver.Api.Features.SearchBestDriver;

public class SearchBestDriverResponse
{
  public Guid DriverId { get; set; }
  public DateTime LastUpdateDate { get; set; }
  public double Distance { get; set; }
  public double Score { get; set; }
}
