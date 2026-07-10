using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    public interface IRule
    {
        string Nom { get; }
        bool IsOverride { get; }
        EvaluationResult Evaluate(HardwareRow row);
    }
}