using Microsoft.Extensions.DependencyInjection;
using SpiceChecker.WinForms.CompositionRoot;
using SpiceChecker.WinForms.ViewModels;
using SpiceChecker.WinForms.Views;

namespace SpiceChecker.WinForms;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        // Fichier .xlsx passé en argument (« Ouvrir avec » depuis l'Explorateur).
        var startupFile = args.FirstOrDefault(a => a.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) && File.Exists(a));

        var services = new ServiceCollection();
        services.AddSpiceCheckerServices();
        services.AddTransient(provider => new MainForm(provider.GetRequiredService<MainViewModel>(), startupFile));

        using var provider = services.BuildServiceProvider();
        System.Windows.Forms.Application.Run(provider.GetRequiredService<MainForm>());
    }
}
