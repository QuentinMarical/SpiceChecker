namespace SpiceChecker.Domain.Validation;

/// <summary>
/// Représente le résultat d'une validation métier.
/// </summary>
/// <param name="IsValid">Indique si la validation est réussie.</param>
/// <param name="ErrorMessage">Message d'erreur lorsque la validation échoue.</param>
public sealed record ValidationResult(bool IsValid, string ErrorMessage)
{
    /// <summary>
    /// Crée un résultat de validation réussi.
    /// </summary>
    /// <returns>Un résultat valide sans message d'erreur.</returns>
    public static ValidationResult Success() => new(true, string.Empty);

    /// <summary>
    /// Crée un résultat de validation en échec.
    /// </summary>
    /// <param name="message">Message décrivant la raison de l'échec.</param>
    /// <returns>Un résultat invalide avec message d'erreur.</returns>
    public static ValidationResult Failure(string message) => new(false, message);
}
