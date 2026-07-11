using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Validation;

namespace SpiceChecker.Domain.Tests;

/// <summary>
/// Tests des règles métier SPICE (consignes Enedis consolidées, mai 2026).
/// </summary>
public class ReglesMetierTests
{
    private static HardwareAsset Asset(
        string modele,
        string fabricant = "Lenovo",
        int? ramGo = 8,
        SousEtat sousEtat = SousEtat.Disponible,
        CategorieEquipement categorie = CategorieEquipement.Ordinateur,
        string commentaire = "",
        DateTime? dateRenouvellement = null,
        DateTime? dateDerniereModifSousEtat = null) => new()
    {
        AssetTag = "SCR0000001",
        Modele = modele,
        Fabricant = fabricant,
        RamGo = ramGo,
        SousEtat = sousEtat,
        Categorie = categorie,
        Commentaire = commentaire,
        DateRenouvellement = dateRenouvellement,
        DateDerniereModifSousEtat = dateDerniereModifSousEtat
    };

    private static RuleEngine CreateEngine()
    {
        var validator = new DefectCommentValidator();
        return new RuleEngine(new IRule[]
        {
            new HighRamLenovoRule(),
            new L13L14RenewalRule(),
            new RevalorisationAutomatiqueRule(),
            new RevalorisationRule(validator),
            new RevalorisationSansDefautRule(),
            new DefectiveStateRule(validator),
            new BlanchimentRule(),
            new StaleSubstateRule()
        });
    }

    // ─── Lenovo 16/32 Go (règle prioritaire) ───────────────────────────────

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    public void Lenovo_16_32Go_EnRevalorisation_EstUneErreur(int ram)
    {
        var rule = new HighRamLenovoRule();
        var result = rule.Evaluate(Asset("THINKPAD_L14 G3 I5 16", ramGo: ram, sousEtat: SousEtat.Revalorisation));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Erreur, result.Niveau);
    }

    [Fact]
    public void Lenovo_16Go_Defectueux_DoitPartirEnReparation()
    {
        var rule = new HighRamLenovoRule();
        var result = rule.Evaluate(Asset("THINKPAD_L14 G3 I5 16", ramGo: 16, sousEtat: SousEtat.Defectueux));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Avertissement, result.Niveau);
        Assert.Contains("Réparation", result.Message);
    }

    [Fact]
    public void Lenovo_L14G2_I7_32Go_Defectueux_MentionneTmi()
    {
        var rule = new HighRamLenovoRule();
        var result = rule.Evaluate(Asset("THINKPAD_L14 G2 I7 32", ramGo: 32, sousEtat: SousEtat.Defectueux));

        Assert.NotNull(result);
        Assert.Contains("TMI", result.Message);
    }

    [Fact]
    public void Lenovo_32Go_EnAttenteDeDon_EstUneErreur()
    {
        var rule = new HighRamLenovoRule();
        var result = rule.Evaluate(Asset("THINKPAD_L14 G2 I7 32", ramGo: 32, sousEtat: SousEtat.EnAttenteDeDon));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Erreur, result.Niveau);
    }

    [Fact]
    public void Lenovo_16Go_Fonctionnel_EstConformeEtCourtCircuiteLesAutresRegles()
    {
        var engine = CreateEngine();
        var result = engine.EvaluateAll(Asset("THINKPAD_L390 I7 16", ramGo: 16, sousEtat: SousEtat.Disponible));

        // Même un L390 (modèle ancien) est conservé s'il a 16 Go : la règle prioritaire l'emporte.
        Assert.Null(result);
    }

    [Fact]
    public void Hp_32Go_NestPasConcerneParLaReglePrioritaire()
    {
        var rule = new HighRamLenovoRule();
        var result = rule.Evaluate(Asset("ProBook 4G1a 14 R5 32Go 512Go", fabricant: "HP", ramGo: 32, sousEtat: SousEtat.Revalorisation));

        Assert.Null(result);
    }

    // ─── L13 / L14 8 Go ────────────────────────────────────────────────────

    [Fact]
    public void L13G1_8Go_Defectueux_DoitPartirEnRevalorisation()
    {
        var rule = new L13L14RenewalRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.Defectueux));

        Assert.NotNull(result);
        Assert.Contains("Revalorisation", result.Message);
    }

    [Fact]
    public void L13G1_8Go_EnRevalorisation_EstConformePourCetteRegle()
    {
        var rule = new L13L14RenewalRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.Revalorisation));

        Assert.Null(result);
    }

    [Fact]
    public void L13G2_8Go_Defectueux_Renouvellement2027_DoitPartirEnRevalorisation()
    {
        var rule = new L13L14RenewalRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G2 I5 8", sousEtat: SousEtat.Defectueux, dateRenouvellement: new DateTime(2027, 6, 1)));

        Assert.NotNull(result);
        Assert.Contains("Revalorisation", result.Message);
    }

    [Fact]
    public void L13G2_8Go_Defectueux_Renouvellement2028_DoitPartirEnReparation()
    {
        var rule = new L13L14RenewalRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G2 I5 8", sousEtat: SousEtat.Defectueux, dateRenouvellement: new DateTime(2028, 3, 1)));

        Assert.NotNull(result);
        Assert.Contains("Réparation", result.Message);
    }

    [Fact]
    public void L14_8Go_Defectueux_SansDateRenouvellement_DemandeVerification()
    {
        var rule = new L13L14RenewalRule();
        var result = rule.Evaluate(Asset("THINKPAD_L14 G1 I5 8", sousEtat: SousEtat.Defectueux));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Avertissement, result.Niveau);
    }

    [Fact]
    public void L13G2_8Go_EnRevalorisation_AvecRenouvellement2028_EstUneErreur()
    {
        var rule = new L13L14RenewalRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G2 I5 8", sousEtat: SousEtat.Revalorisation, dateRenouvellement: new DateTime(2028, 3, 1)));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Erreur, result.Niveau);
    }

    [Fact]
    public void L13G1_8Go_DisponibleNeuf_DoitPasserEnReUse()
    {
        var rule = new L13L14RenewalRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.DisponibleNeuf));

        Assert.NotNull(result);
        Assert.Contains("Disponible Re-Use", result.Message);
    }

    // ─── Revalorisation automatique (modèles anciens, Surface, non Lenovo/HP) ─

    [Fact]
    public void L390_8Go_EnStock_DoitPartirEnRevalorisation()
    {
        var rule = new RevalorisationAutomatiqueRule();
        var result = rule.Evaluate(Asset("THINKPAD_L390 I5 8", sousEtat: SousEtat.Disponible));

        Assert.NotNull(result);
        Assert.Contains("Revalorisation", result.Message);
    }

    [Fact]
    public void L390_DejaEnRevalorisation_EstConforme()
    {
        var rule = new RevalorisationAutomatiqueRule();
        var result = rule.Evaluate(Asset("THINKPAD_L390 I5 8", sousEtat: SousEtat.Revalorisation));

        Assert.Null(result);
    }

    [Fact]
    public void SurfacePro_Defectueux_DoitPartirEnRevalorisation()
    {
        var rule = new RevalorisationAutomatiqueRule();
        var result = rule.Evaluate(Asset("SURFACE_PRO_7+ 8Go 256Go", fabricant: "Microsoft", sousEtat: SousEtat.Defectueux));

        Assert.NotNull(result);
        Assert.Contains("Revalorisation", result.Message);
    }

    [Fact]
    public void OrdinateurNonLenovoHp_Defectueux_DoitPartirEnRevalorisation()
    {
        var rule = new RevalorisationAutomatiqueRule();
        var result = rule.Evaluate(Asset("TOUGHBOOK_CF20", fabricant: "Panasonic", ramGo: null, sousEtat: SousEtat.Defectueux));

        Assert.NotNull(result);
        Assert.Contains("Revalorisation", result.Message);
    }

    [Fact]
    public void Serveur_EnRevalorisation_NestPasConcerne()
    {
        var rule = new RevalorisationAutomatiqueRule();
        var result = rule.Evaluate(Asset("PROLIANT_ML350G10", fabricant: "HP", ramGo: null, sousEtat: SousEtat.Revalorisation, categorie: CategorieEquipement.Serveur));

        Assert.Null(result);
    }

    // ─── Revalorisation : justification de panne ───────────────────────────

    [Fact]
    public void Revalorisation_SansCommentaire_EstUnAvertissement()
    {
        var rule = new RevalorisationSansDefautRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.Revalorisation));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Avertissement, result.Niveau);
    }

    [Fact]
    public void Revalorisation_CommentaireSansPanne_EstUneErreur()
    {
        var rule = new RevalorisationSansDefautRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.Revalorisation, commentaire: "poste récupéré au bureau du 2e étage"));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Erreur, result.Niveau);
    }

    [Theory]
    [InlineData("écran cassé en bas à gauche")]
    [InlineData("en panne clavier, touches E et R mortes")]
    [InlineData("batterie gonflée")]
    [InlineData("ne démarre plus après chute")]
    public void Revalorisation_CommentaireDePanneValide_EstConforme(string commentaire)
    {
        var rule = new RevalorisationSansDefautRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.Revalorisation, commentaire: commentaire));

        Assert.Null(result);
    }

    // ─── Validation du commentaire de panne ────────────────────────────────

    [Fact]
    public void Defectueux_SansCommentaire_EstInvalide()
    {
        var validator = new DefectCommentValidator();
        Assert.False(validator.Validate("", SousEtat.Defectueux).IsValid);
    }

    [Fact]
    public void Defectueux_CommentaireTropCourt_EstInvalide()
    {
        var validator = new DefectCommentValidator();
        Assert.False(validator.Validate("HS", SousEtat.Defectueux).IsValid);
    }

    [Fact]
    public void Defectueux_CommentaireDePanneComplet_EstValide()
    {
        var validator = new DefectCommentValidator();
        Assert.True(validator.Validate("écran cassé au coin supérieur droit", SousEtat.Defectueux).IsValid);
    }

    [Fact]
    public void Revalorisation_CommentaireDeReparation_EstInvalide()
    {
        var validator = new DefectCommentValidator();
        Assert.False(validator.Validate("clavier HS, à réparer par le SAV", SousEtat.Revalorisation).IsValid);
    }

    // ─── Blanchiment et sous-états figés ───────────────────────────────────

    [Fact]
    public void ABlanchir_ProduitUneInfoDeTransitionVersReUse()
    {
        var rule = new BlanchimentRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.ABlanchir));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Info, result.Niveau);
        Assert.Contains("Disponible Re-Use", result.Message);
    }

    [Fact]
    public void SousEtatTransitoire_FigeDepuisPlusDe6Mois_EstSignale()
    {
        var rule = new StaleSubstateRule();
        var result = rule.Evaluate(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.ABlanchir, dateDerniereModifSousEtat: DateTime.Today.AddDays(-200)));

        Assert.NotNull(result);
        Assert.Equal(NiveauAnomalie.Info, result.Niveau);
    }

    [Fact]
    public void SousEtatStable_AncienMaisLegitime_NestPasSignale()
    {
        var rule = new StaleSubstateRule();
        var result = rule.Evaluate(Asset("THINKPAD_L14 G3 I5 16", ramGo: 16, sousEtat: SousEtat.Disponible, dateDerniereModifSousEtat: DateTime.Today.AddDays(-400)));

        Assert.Null(result);
    }

    // ─── Priorités du moteur de règles ─────────────────────────────────────

    [Fact]
    public void Lenovo16Go_EnRevalorisation_CestLaReglePrioritaireQuiRepond()
    {
        var engine = CreateEngine();
        var result = engine.EvaluateAll(Asset("THINKPAD_L14 G3 I5 16", ramGo: 16, sousEtat: SousEtat.Revalorisation));

        Assert.NotNull(result);
        Assert.Equal("HighRamLenovoRule", result.RegleDeclenchee);
    }

    [Fact]
    public void ReserveMasterise_NestJamaisUneAnomalie()
    {
        var engine = CreateEngine();
        var result = engine.EvaluateAll(Asset("THINKPAD_L13 G1 I5 8", sousEtat: SousEtat.ReserveMasterise));

        Assert.Null(result);
    }
}
