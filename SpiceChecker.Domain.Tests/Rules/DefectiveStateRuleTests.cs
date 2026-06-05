using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Tests.Helpers;
using SpiceChecker.Domain.Validation;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class DefectiveStateRuleTests
{
    [Fact]
    public void ReturnsError_WhenDefectueuxAndInjectedValidatorFails()
    {
        var validator = new FakeDefectCommentValidator(ValidationResult.Failure("Commentaire requis"));
        var rule = new DefectiveStateRule(validator);
        var asset = HardwareAssetTestHelper.CreateAsset(sousEtat: SousEtat.Defectueux, commentaire: " ");

        var result = rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.Niveau.Should().Be(NiveauAnomalie.Erreur);
        result.EstBloquant.Should().BeTrue();
        result.RegleDeclenchee.Should().Be("DefectiveStateRule");
        result.Message.Should().Be("Commentaire requis");
        validator.CallCount.Should().Be(1);
    }

    [Fact]
    public void ReturnsWarning_WhenDisponibleAndCommentMentionsPanne()
    {
        var validator = new FakeDefectCommentValidator(ValidationResult.Success());
        var rule = new DefectiveStateRule(validator);
        var asset = HardwareAssetTestHelper.CreateAsset(sousEtat: SousEtat.Disponible, commentaire: "PC en panne intermittente");

        var result = rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.Niveau.Should().Be(NiveauAnomalie.Avertissement);
        result.EstBloquant.Should().BeFalse();
        result.Message.Should().Be("Incohérence : l'équipement est marqué disponible mais le commentaire mentionne un défaut.");
        validator.CallCount.Should().Be(0);
    }

    private sealed class FakeDefectCommentValidator : IDefectCommentValidator
    {
        private readonly ValidationResult _result;

        public FakeDefectCommentValidator(ValidationResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public ValidationResult Validate(string commentaire, SousEtat sousEtat)
        {
            CallCount++;
            return _result;
        }
    }
}
