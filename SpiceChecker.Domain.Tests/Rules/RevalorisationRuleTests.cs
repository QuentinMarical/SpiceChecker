using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Tests.Helpers;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class RevalorisationRuleTests
{
    private readonly IRule _rule = new RevalorisationRule();

    [Fact]
    public void ReturnsNull_WhenCategorieIsEquipementReseau()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            categorie: CategorieEquipement.EquipementReseau,
            sousEtat: SousEtat.Revalorisation);

        var result = _rule.Evaluate(asset);

        result.Should().BeNull();
    }

    [Fact]
    public void ReturnsInfo_WhenOrdinateurInRevalorisation()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            categorie: CategorieEquipement.Ordinateur,
            sousEtat: SousEtat.Revalorisation);

        var result = _rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.Niveau.Should().Be(NiveauAnomalie.Info);
        result.EstBloquant.Should().BeFalse();
        result.RegleDeclenchee.Should().Be("RevalorisationRule");
        result.Message.Should().Be("Revalorisation à envisager pour cet équipement.");
    }
}
