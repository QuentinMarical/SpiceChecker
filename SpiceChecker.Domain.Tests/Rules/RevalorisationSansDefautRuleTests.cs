using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Tests.Helpers;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class RevalorisationSansDefautRuleTests
{
    private readonly IRule _rule = new RevalorisationSansDefautRule();

    [Fact]
    public void ReturnsError_WhenOrdinateurInRevalorisationWithoutDefectComment()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            categorie: CategorieEquipement.Ordinateur,
            sousEtat: SousEtat.Revalorisation,
            commentaire: "Poste à contrôler");

        var result = _rule.Evaluate(asset);

        result.Should().NotBeNull();
        result!.Niveau.Should().Be(NiveauAnomalie.Erreur);
        result.EstBloquant.Should().BeTrue();
        result.RegleDeclenchee.Should().Be("RevalorisationSansDefautRule");
        result.Message.Should().Be("Un ordinateur ne peut être en revalorisation sans justification de défaut.");
    }

    [Fact]
    public void ReturnsNull_WhenOrdinateurInRevalorisationWithDefectComment()
    {
        var asset = HardwareAssetTestHelper.CreateAsset(
            categorie: CategorieEquipement.Ordinateur,
            sousEtat: SousEtat.Revalorisation,
            commentaire: "Écran cassé et panne au démarrage.");

        var result = _rule.Evaluate(asset);

        result.Should().BeNull();
    }
}
