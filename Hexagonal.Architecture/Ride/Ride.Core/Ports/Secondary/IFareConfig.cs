namespace Ride.Core.Ports.Secondary;

public interface IFareConfig
{
  double BaseFare { get; }
  double PerKmRate { get; }
  double PerMinuteRate { get; }
}
