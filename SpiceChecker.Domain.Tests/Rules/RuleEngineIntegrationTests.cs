using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Tests.Helpers;
using SpiceChecker.Domain.Validation;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class RuleEngineIntegrationTests
{
    [Fact]
    public void EvaluateAll_RealRules_StopsAtHighRamLenovoOverride()
    {
        var engine = new RuleEngine(new IRule[]
        {
            new HighRamLenovoRule(),
            new DefectiveStateRule(new DefectCommentValidator()),
            new RevalorisationRule(),
            new RevalorisationSansDefautRule(),
            new L13L14RenewalRule(),
            new StaleSubstateRule()
        });

        var asset = HardwareAssetTestHelper.CreateAsset(
            fabricant: "Lenovo",
            modele: "ThinkPad L14 Gen 3",
            ramGo: 16,
            sousEtat: SousEtat.Defectueux,
            commentaire: " ",
            dateDerniereModifSousEtat: DateTime.Today.AddDays(-100));

        var result = engine.EvaluateAll(asset);

        result.Should().NotBeNull();
        result!.RegleDeclenchee.Should().Be("HighRamLenovoRule");
        result.Niveau.Should().Be(NiveauAnomalie.Erreur);
        result.EstBloquant.Should().BeTrue();
    }
}
