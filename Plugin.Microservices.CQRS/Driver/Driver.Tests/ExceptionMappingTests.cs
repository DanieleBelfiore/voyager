using Common.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Driver.Tests;

/// <summary>
/// Covers Common.Core's exception-to-status table. It lives in Driver.Tests for the same reason
/// RedisCacheServiceTests does: Common.Core has no test project of its own, and Driver.Handlers
/// is the reference that makes it reachable. This variant is self-contained by design, so it
/// maps its own exception types rather than the shared Voyager.Errors ones.
/// </summary>
public class ExceptionMappingTests
{
  [Fact]
  public void Map_ReturnsConcurrentModification_ForConcurrencyFailure()
  {
    // Ride carries a RowVersion token, so two writers racing the same ride raise this. It was
    // unmapped, so losing the race came back as a blanket 500 with no way for the client to tell
    // a retryable race apart from a genuine server fault.
    var (statusCode, error) = ExceptionHandlerExtensions.Map(new DbUpdateConcurrencyException());

    Assert.Equal(StatusCodes.Status409Conflict, statusCode);
    Assert.Equal("concurrent_modification", error);
  }

  [Fact]
  public void Map_KeepsConcurrencyDistinctFromDomainConflict()
  {
    // Both are 409, but a lost race is safe to retry and an illegal status transition is not —
    // the codes have to stay distinguishable for a client to act on them differently.
    var concurrency = ExceptionHandlerExtensions.Map(new DbUpdateConcurrencyException());
    var domain = ExceptionHandlerExtensions.Map(new ConflictException("operation_not_permitted"));

    Assert.Equal(concurrency.StatusCode, domain.StatusCode);
    Assert.NotEqual(concurrency.Error, domain.Error);
  }

  [Theory]
  [MemberData(nameof(MappedExceptions))]
  public void Map_ReturnsExpectedStatus_ForMappedExceptions(Exception exception, int expectedStatusCode, string expectedError)
  {
    var (statusCode, error) = ExceptionHandlerExtensions.Map(exception);

    Assert.Equal(expectedStatusCode, statusCode);
    Assert.Equal(expectedError, error);
  }

  public static TheoryData<Exception, int, string> MappedExceptions() => new()
  {
    { new InvalidInputException("rating_out_of_range"), StatusCodes.Status400BadRequest, "invalid_input" },
    { new UnauthorizedAccessException("not_ride_participant"), StatusCodes.Status403Forbidden, "forbidden" },
    { new NotFoundException("ride_not_found"), StatusCodes.Status404NotFound, "not_found" },
    { new ConflictException("operation_not_permitted"), StatusCodes.Status409Conflict, "conflict" }
  };

  [Fact]
  public void Map_FallsBackTo500_ForUnmappedException()
  {
    // An unmapped exception is an unhandled fault. Reporting it as a client error would hide a
    // real outage behind a 4xx nobody pages on.
    var (statusCode, error) = ExceptionHandlerExtensions.Map(new InvalidOperationException("boom"));

    Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
    Assert.Equal("internal_server_error", error);
  }

  [Fact]
  public void Map_FallsBackTo500_WhenNoExceptionIsAvailable()
  {
    // IExceptionHandlerFeature?.Error is null-propagated at the call site, so null reaches Map.
    var (statusCode, error) = ExceptionHandlerExtensions.Map(null);

    Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
    Assert.Equal("internal_server_error", error);
  }
}
