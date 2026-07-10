namespace SpiceChecker.Services
{
    /// <summary>
    /// Helper partagé pour la validation des commentaires de panne.
    /// Réutilisé par DefectiveStateRule, RevalorisationRule et EditSubStateForm.
    /// </summary>
    public static class ValidationHelpers
    {
        /// <summary>
        /// Applique un contrôle de commentaire de panne obligatoire.
        /// Retourne null si OK, ou un message d'anomalie si le commentaire manque.
        /// </summary>
        public static string? ApplyDefectCommentCheck(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return "Défectueux : commentaire de panne manquant";
            }
            if (description.Length < 10)
            {
                return "Défectueux : commentaire de panne trop court (min. 10 car.)";
            }
            return null;
        }

        /// <summary>
        /// Vérifie si un commentaire est présent et suffisamment descriptif.
        /// </summary>
        public static bool HasValidComment(string? description)
        {
            return !string.IsNullOrWhiteSpace(description) && description.Length >= 10;
        }
    }
}