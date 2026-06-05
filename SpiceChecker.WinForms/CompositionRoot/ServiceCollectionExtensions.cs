using Microsoft.Extensions.DependencyInjection;
using SpiceChecker.Application.Services;
using SpiceChecker.Application.UseCases;
using SpiceChecker.Domain.Rules;
using SpiceChecker.Domain.Validation;
using SpiceChecker.Infrastructure.Excel;
using SpiceChecker.Infrastructure.Export;
using SpiceChecker.Infrastructure.Settings;
using SpiceChecker.WinForms.Services;
using SpiceChecker.WinForms.ViewModels;

namespace SpiceChecker.WinForms.CompositionRoot;

/// <summary>
/// Extensions d'enregistrement des services SpiceChecker.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre les services Application, Domain et Infrastructure nécessaires à l'UI WinForms.
    /// </summary>
    /// <param name="services">Collection de services.</param>
    /// <returns>La collection enrichie.</returns>
    public static IServiceCollection AddSpiceCheckerServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IHardwareImportService, HardwareImportService>();
        services.AddTransient<IFilePickerService, WinFormsFilePickerService>();
        services.AddTransient<IEditAssetDialogService, EditAssetDialogService>();
        services.AddTransient<ISaveFileService, WinFormsSaveFileService>();
        services.AddTransient<IClipboardService, WinFormsClipboardService>();
        services.AddTransient<IClipboardExportFormatter, ClipboardExportFormatter>();
        services.AddSingleton<IThemeService, WinFormsThemeService>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();

        services.AddTransient<IDefectCommentValidator, DefectCommentValidator>();

        services.AddTransient<IRule, HighRamLenovoRule>();
        services.AddTransient<IRule, DefectiveStateRule>();
        services.AddTransient<IRule, RevalorisationRule>();
        services.AddTransient<IRule, RevalorisationSansDefautRule>();
        services.AddTransient<IRule, L13L14RenewalRule>();
        services.AddTransient<IRule, StaleSubstateRule>();

        services.AddTransient<IProcessSpiceExportUseCase, ProcessSpiceExportUseCase>();
        services.AddTransient<IFilterAssetsUseCase, FilterAssetsUseCase>();
        services.AddTransient<IReevaluateAssetUseCase, ReevaluateAssetUseCase>();
        services.AddTransient<CsvExportService>();
        services.AddTransient<XlsxExportService>();
        services.AddTransient<IExportService, ExportService>();
        services.AddTransient<IExportDataUseCase, ExportDataUseCase>();
        services.AddTransient<MainViewModel>();

        return services;
    }
}
