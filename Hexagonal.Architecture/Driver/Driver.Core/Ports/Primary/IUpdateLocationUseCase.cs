using Hikyaku;
using Voyager.Contracts.Driver;

namespace Driver.Core.Ports.Primary;

/// <summary>Primary port — see IAddDriverUseCase for why this extends IRequestHandler&lt;T&gt;.</summary>
public interface IUpdateLocationUseCase : IRequestHandler<UpdateLocation>;
