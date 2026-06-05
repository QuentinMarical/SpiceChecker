using System.Runtime.InteropServices;

namespace SpiceChecker.WinForms.Services;

internal static partial class DwmHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2;

    [LibraryImport("dwmapi.dll", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

    [LibraryImport("dwmapi.dll", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool enabled);

    public static bool SupportsMica()
    {
        var version = Environment.OSVersion.Version;
        return version.Major >= 10 && version.Build >= 22000;
    }

    public static void ApplyFluentOrMica(IntPtr hwnd, bool dark)
    {
        if (hwnd == IntPtr.Zero || !SupportsMica())
        {
            return;
        }

        try
        {
            var compositionHr = DwmIsCompositionEnabled(out var compositionEnabled);
            if (compositionHr != 0)
            {
                System.Diagnostics.Debug.WriteLine($"DWM Error (DwmIsCompositionEnabled): {compositionHr}");
                return;
            }

            if (!compositionEnabled)
            {
                return;
            }

            var darkMode = dark ? 1 : 0;
            var darkModeHr = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            if (darkModeHr != 0)
            {
                System.Diagnostics.Debug.WriteLine($"DWM Error (DWMWA_USE_IMMERSIVE_DARK_MODE): {darkModeHr}");
            }

            var backdrop = DWMSBT_MAINWINDOW;
            var backdropHr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            if (backdropHr != 0)
            {
                System.Diagnostics.Debug.WriteLine($"DWM Error (DWMWA_SYSTEMBACKDROP_TYPE): {backdropHr}");
            }
        }
        catch
        {
        }
    }

    public static void DisableBackdrop(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !SupportsMica())
        {
            return;
        }

        try
        {
            var backdrop = DWMSBT_NONE;
            var backdropHr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            if (backdropHr != 0)
            {
                System.Diagnostics.Debug.WriteLine($"DWM Error (DisableBackdrop): {backdropHr}");
            }

            var darkMode = 0;
            var darkModeHr = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            if (darkModeHr != 0)
            {
                System.Diagnostics.Debug.WriteLine($"DWM Error (ResetDarkMode): {darkModeHr}");
            }
        }
        catch
        {
        }
    }
}
