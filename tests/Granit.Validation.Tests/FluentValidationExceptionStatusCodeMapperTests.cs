// =============================================================================
// Tests - FluentValidationExceptionStatusCodeMapper
// =============================================================================
// Verifies that FluentValidation.ValidationException maps to 422 and that all
// other exception types return null (pass-through in chain of responsibility).
// =============================================================================

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Granit.Validation.Internal;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Granit.Validation.Tests;

public sealed class FluentValidationExceptionStatusCodeMapperTests
{
    private static FluentValidationExceptionStatusCodeMapper Create() => new();

    [Fact]
    public void ValidationException_Returns422()
    {
        FluentValidationExceptionStatusCodeMapper mapper = Create();
        ValidationFailure failure = new("Name", "Granit:Validation:NotNullValidator");

        int? result = mapper.TryGetStatusCode(new ValidationException([failure]));

        result.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public void InvalidOperationException_ReturnsNull()
    {
        FluentValidationExceptionStatusCodeMapper mapper = Create();

        int? result = mapper.TryGetStatusCode(new InvalidOperationException("Something failed"));

        result.Should().BeNull();
    }

    [Fact]
    public void ArgumentException_ReturnsNull()
    {
        FluentValidationExceptionStatusCodeMapper mapper = Create();

        int? result = mapper.TryGetStatusCode(new ArgumentException("Bad argument"));

        result.Should().BeNull();
    }

    [Fact]
    public void NotSupportedException_ReturnsNull()
    {
        FluentValidationExceptionStatusCodeMapper mapper = Create();

        int? result = mapper.TryGetStatusCode(new NotSupportedException("Not supported"));

        result.Should().BeNull();
    }
}
