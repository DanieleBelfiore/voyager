using MediatR;
using Voyager.Contracts.Driver;

namespace Driver.Core.Ports.Primary;

public interface IGetDriverAvailabilityUseCase : IRequestHandler<GetDriverAvailability, DriverAvailabilityInfo>;
