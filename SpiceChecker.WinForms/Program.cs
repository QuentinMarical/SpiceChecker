using Microsoft.Extensions.DependencyInjection;
using SpiceChecker.WinForms.CompositionRoot;
using SpiceChecker.WinForms.Views;

namespace SpiceChecker.WinForms;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        services.AddSpiceCheckerServices();
        services.AddTransient<MainForm>();

        using var provider = services.BuildServiceProvider();
        System.Windows.Forms.Application.Run(provider.GetRequiredService<MainForm>());
    }
}
