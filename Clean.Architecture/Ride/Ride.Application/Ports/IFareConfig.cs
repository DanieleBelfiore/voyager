namespace Ride.Application.Ports;

public interface IFareConfig
{
  double BaseFare { get; }
  double PerKmRate { get; }
  double PerMinuteRate { get; }
}
