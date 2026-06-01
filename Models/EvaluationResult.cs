namespace SpiceChecker.Models
{
    public enum NiveauAnomalie
    {
        OK,
        Info,
        Avertissement,
        Erreur
    }

    public class EvaluationResult
    {
        public bool EstAnomalie { get; set; }
        public NiveauAnomalie Niveau { get; set; } = NiveauAnomalie.OK;
        public string Message { get; set; } = "";
        public string SousEtatConseille { get; set; } = "";

        public static EvaluationResult Ok() => new EvaluationResult();
    }
}