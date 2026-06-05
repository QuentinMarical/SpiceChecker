using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Tests.Helpers;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class HighRamLenovoRuleTests
{
    private readonly IRule _rule = new HighRamLenovoRule();

    [Fact]
    public void ReturnsAnomaly_WhenLenovoAnd16Go()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(fabricant: "Lenovo", ramGo: 16);

        var result = _rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.Niveau.Should().Be(NiveauAnomalie.Erreur);
        result.RegleDeclenchee.Should().Be("HighRamLenovoRule");
        result.Message.Should().Be("Lenovo 16/32 Go détecté : à traiter en priorité (Override).");
        result.EstBloquant.Should().BeTrue();
    }

    [Fact]
    public void ReturnsAnomaly_WhenLenovoAnd32Go()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(fabricant: "LENOVO", ramGo: 32);

        var result = _rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.EstBloquant.Should().BeTrue();
    }

    [Fact]
    public void ReturnsNull_WhenLenovoBut8Go()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(fabricant: "Lenovo", ramGo: 8);

        var result = _rule.Evaluate(asset);

        result.Should().BeNull();
    }

    [Fact]
    public void ReturnsNull_When16GoButDell()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(fabricant: "Dell", ramGo: 16);

        var result = _rule.Evaluate(asset);

        result.Should().BeNull();
    }
}
