using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    public interface IRule
    {
        string Nom { get; }
        EvaluationResult Evaluate(HardwareRow row);
    }
}