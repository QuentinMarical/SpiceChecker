using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Contrat de réévaluation d'un équipement avec les règles métier configurées.
/// </summary>
public interface IReevaluateAssetUseCase
{
    /// <summary>
    /// Retourne une nouvelle instance d'équipement avec sa nouvelle évaluation.
    /// </summary>
    HardwareAsset Execute(HardwareAsset asset);
}
