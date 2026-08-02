using FluentValidation;
using Identity.Module.Features.RegisterUser;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Identity.Tests;

/// <summary>
/// Guards the Host's validator registration. Every validator in this variant is internal by
/// design, and AddValidatorsFromAssemblies defaults to includeInternalTypes: false — which scans
/// GetExportedTypes() and finds none of them. The failure is silent: ValidationBehavior resolves
/// an empty validator list and lets unvalidated input straight through to the handler.
/// </summary>
public class ValidatorRegistrationTests
{
  [Fact]
  public void AddValidatorsFromAssemblies_ResolvesInternalValidators_WhenIncludeInternalTypesIsSet()
  {
    // Arrange — mirrors Host/Program.cs
    var services = new ServiceCollection();
    services.AddValidatorsFromAssemblies([typeof(RegisterUserValidator).Assembly], includeInternalTypes: true);

    // Act
    var validator = services.BuildServiceProvider().GetService<IValidator<RegisterUser>>();

    // Assert
    Assert.NotNull(validator);
  }

  [Fact]
  public void AddValidatorsFromAssemblies_FindsNothing_WithoutIncludeInternalTypes()
  {
    // Arrange — the default overload, i.e. the bug this guards against
    var services = new ServiceCollection();
    services.AddValidatorsFromAssemblies([typeof(RegisterUserValidator).Assembly]);

    // Act
    var validator = services.BuildServiceProvider().GetService<IValidator<RegisterUser>>();

    // Assert — documents *why* the explicit flag is load-bearing
    Assert.Null(validator);
  }

  [Theory]
  [InlineData("", "Ada", "Lovelace", "ada@example.com", "Str0ng!Pass", "Str0ng!Pass")]
  [InlineData("Ada", "", "Lovelace", "ada@example.com", "Str0ng!Pass", "Str0ng!Pass")]
  [InlineData("Ada", "Lovelace", "", "not-an-email", "Str0ng!Pass", "Str0ng!Pass")]
  [InlineData("Ada", "Lovelace", "Lovelace", "ada@example.com", "short", "short")]
  [InlineData("Ada", "Lovelace", "Lovelace", "ada@example.com", "Str0ng!Pass", "different")]
  public void RegisterUserValidator_Rejects_InvalidRegistrations(
    string firstName, string lastName, string _, string email, string password, string confirmPassword)
  {
    // Arrange
    var validator = new RegisterUserValidator();
    var command = new RegisterUser
    {
      FirstName = firstName,
      LastName = lastName,
      Email = email,
      Password = password,
      ConfirmPassword = confirmPassword
    };

    // Act
    var result = validator.Validate(command);

    // Assert
    Assert.False(result.IsValid);
  }

  [Fact]
  public void RegisterUserValidator_Accepts_ValidRegistration()
  {
    // Arrange
    var validator = new RegisterUserValidator();
    var command = new RegisterUser
    {
      FirstName = "Ada",
      LastName = "Lovelace",
      Email = "ada@example.com",
      Password = "Str0ng!Pass",
      ConfirmPassword = "Str0ng!Pass"
    };

    // Act
    var result = validator.Validate(command);

    // Assert
    Assert.True(result.IsValid);
  }
}
