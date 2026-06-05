using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Validation;

namespace SpiceChecker.Domain.Tests.Validation;

public sealed class DefectCommentValidatorTests
{
    private readonly IDefectCommentValidator _validator = new DefectCommentValidator();

    [Fact]
    public void Validate_ReturnsFailure_WhenDefectueuxAndCommentIsEmpty()
    {
        var result = _validator.Validate("   ", SousEtat.Defectueux);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("obligatoire");
        result.ErrorMessage.Should().Contain("Défectueux");
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenCommentContainsForbiddenTerm()
    {
        var result = _validator.Validate("Machine à réparer rapidement", SousEtat.Disponible);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("réparation");
        result.ErrorMessage.Should().Contain("Défectueux");
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenDefectueuxWithNormalComment()
    {
        var result = _validator.Validate("Clavier bloqué sur plusieurs touches.", SousEtat.Defectueux);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenCommentIsNormal()
    {
        var result = _validator.Validate("RAS sur le matériel.", SousEtat.Disponible);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }
}
