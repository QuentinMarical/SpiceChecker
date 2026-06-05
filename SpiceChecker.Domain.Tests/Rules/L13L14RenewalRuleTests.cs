using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Tests.Helpers;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class L13L14RenewalRuleTests
{
    private readonly IRule _rule = new L13L14RenewalRule();

    [Fact]
    public void ReturnsWarning_WhenLenovoL14_8Go_AndRenewalIsDue()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            fabricant: "Lenovo",
            modele: "ThinkPad L14 Gen 2",
            ramGo: 8,
            dateRenouvellement: DateTime.Today.AddDays(-1));

        var result = _rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.Niveau.Should().Be(NiveauAnomalie.Avertissement);
        result.EstBloquant.Should().BeFalse();
        result.RegleDeclenchee.Should().Be("L13L14RenewalRule");
        result.Message.Should().Be("Modèle Lenovo L13/L14 8Go dont le renouvellement est échu ou imminent.");
    }

    [Fact]
    public void ReturnsNull_WhenRenewalDateIsInFuture()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            fabricant: "Lenovo",
            modele: "ThinkPad L13 Gen 3",
            ramGo: 8,
            dateRenouvellement: DateTime.Today.AddDays(10));

        var result = _rule.Evaluate(asset);

        result.Should().BeNull();
    }
}
