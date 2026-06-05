using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Tests.Helpers;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class StaleSubstateRuleTests
{
    private readonly IRule _rule = new StaleSubstateRule();

    [Fact]
    public void ReturnsInfo_WhenSubstateIsOldAndEligible()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            sousEtat: SousEtat.Revalorisation,
            dateDerniereModifSousEtat: DateTime.Today.AddDays(-100));

        var result = _rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.Niveau.Should().Be(NiveauAnomalie.Info);
        result.EstBloquant.Should().BeFalse();
        result.RegleDeclenchee.Should().Be("StaleSubstateRule");
        result.Message.Should().Be("Sous-état inchangé depuis plus de 90 jours, à vérifier.");
    }

    [Fact]
    public void ReturnsNull_WhenSubstateIsRecent()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            sousEtat: SousEtat.RepriseEnAttente,
            dateDerniereModifSousEtat: DateTime.Today.AddDays(-10));

        var result = _rule.Evaluate(asset);

        result.Should().BeNull();
    }
}
