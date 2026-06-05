using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;
using FluentAssertions;

namespace SpiceChecker.Domain.Tests.Rules;

public sealed class RuleEngineTests
{
    [Fact]
    public void EvaluateAll_ReturnsNull_WhenNoRulesTrigger()
    {
        var rules = new IRule[]
        {
            new FakeRule("R1", _ => null),
            new FakeRule("R2", _ => null)
        };

        var engine = new RuleEngine(rules);
        var asset = CreateAsset();

        var result = engine.EvaluateAll(asset);

        result.Should().BeNull();
    }

    [Fact]
    public void EvaluateAll_StopsAtFirstAnomaly()
    {
        var anomaly = new EvaluationResult
        {
            Niveau = NiveauAnomalie.Erreur,
            RegleDeclenchee = "R1",
            Message = "Anomalie test",
            EstBloquant = true
        };

        var first = new CountingRule("R1", _ => anomaly);
        var second = new CountingRule("R2", _ => null);
        var third = new CountingRule("R3", _ => null);

        var engine = new RuleEngine(new IRule[] { first, second, third });
        var asset = CreateAsset();

        var result = engine.EvaluateAll(asset);

        result.Should().BeEquivalentTo(anomaly);
        first.EvaluateCallCount.Should().Be(1);
        second.EvaluateCallCount.Should().Be(0);
        third.EvaluateCallCount.Should().Be(0);
    }

    [Fact]
    public void EvaluateAll_ReturnsNull_WhenAllRulesReturnNull()
    {
        var first = new CountingRule("R1", _ => null);
        var second = new CountingRule("R2", _ => null);
        var third = new CountingRule("R3", _ => null);

        var engine = new RuleEngine(new IRule[] { first, second, third });
        var asset = CreateAsset();

        var result = engine.EvaluateAll(asset);

        result.Should().BeNull();
        first.EvaluateCallCount.Should().Be(1);
        second.EvaluateCallCount.Should().Be(1);
        third.EvaluateCallCount.Should().Be(1);
    }

    private static HardwareAsset CreateAsset() => new()
    {
        AssetTag = "ASSET-001",
        Categorie = CategorieEquipement.Ordinateur,
        Fabricant = "LENOVO",
        Modele = "ThinkPad T14",
        RamGo = 16,
        SousEtat = SousEtat.Disponible,
        Commentaire = string.Empty
    };

    private sealed class FakeRule : IRule
    {
        private readonly Func<HardwareAsset, EvaluationResult?> _evaluator;

        public FakeRule(string name, Func<HardwareAsset, EvaluationResult?> evaluator)
        {
            Name = name;
            _evaluator = evaluator;
        }

        public string Name { get; }

        public EvaluationResult? Evaluate(HardwareAsset asset) => _evaluator(asset);
    }

    private sealed class CountingRule : IRule
    {
        private readonly Func<HardwareAsset, EvaluationResult?> _evaluator;

        public CountingRule(string name, Func<HardwareAsset, EvaluationResult?> evaluator)
        {
            Name = name;
            _evaluator = evaluator;
        }

        public string Name { get; }

        public int EvaluateCallCount { get; private set; }

        public EvaluationResult? Evaluate(HardwareAsset asset)
        {
            EvaluateCallCount++;
            return _evaluator(asset);
        }
    }
}
