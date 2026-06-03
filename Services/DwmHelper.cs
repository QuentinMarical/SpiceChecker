using System.Runtime.InteropServices;
using SpiceChecker.Models;
using Vanara.PInvoke;

namespace SpiceChecker.Services;

/// <summary>
/// Applique les effets de transparence DWM Windows 11 (Mica / Acrylic / Blur)
/// via P/Invoke sur DwmSetWindowAttribute + SetWindowCompositionAttribute.
/// </summary>
internal static class DwmHelper
{
    // ── DWM attributes ──────────────────────────────────────────────────────
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE     = 38;   // Win 11 22H2+
    private const int DWMWA_MICA_EFFECT             = 1029; // Win 11 21H2 (fallback)

    private enum DWM_SYSTEMBACKDROP_TYPE
    {
        Auto    = 0,
        None    = 1,
        Mica    = 2,
        Acrylic = 3,
        Tabbed  = 4   // "Mica Alt"
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmIsCompositionEnabled(out bool enabled);

    // ── SetWindowCompositionAttribute (pour Acrylic Win10/11 21H1) ─────────
    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;   // AABBGGRR
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int    Attribute;    // 19 = WCA_ACCENT_POLICY
        public IntPtr Data;
        public int    SizeOfData;
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowCompositionAttribute(
        IntPtr hwnd, ref WindowCompositionAttributeData data);

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>Détermine si Windows 11 22H2+ est disponible (Mica natif).</summary>
    public static bool IsWin11_22H2OrLater()
    {
        var v = Environment.OSVersion.Version;
        return v.Major >= 10 && v.Build >= 22621;
    }

    public static bool IsWin11OrLater()
    {
        var v = Environment.OSVersion.Version;
        return v.Major >= 10 && v.Build >= 22000;
    }

    /// <summary>Active Mica (DWMWA_SYSTEMBACKDROP_TYPE = 2) sur Win 11 22H2+.</summary>
    public static void ApplyMica(IntPtr hwnd)
    {
        if (!IsWin11_22H2OrLater()) return;
        int val = (int)DWM_SYSTEMBACKDROP_TYPE.Mica;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref val, sizeof(int));
    }

    /// <summary>Active Acrylic (DWMWA_SYSTEMBACKDROP_TYPE = 3) sur Win 11 22H2+.</summary>
    public static void ApplyAcrylic(IntPtr hwnd)
    {
        if (!IsWin11_22H2OrLater()) return;
        int val = (int)DWM_SYSTEMBACKDROP_TYPE.Acrylic;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref val, sizeof(int));
    }

    /// <summary>Fallback Mica pour Win 11 21H2 (build 22000–22620).</summary>
    public static void ApplyMicaLegacy(IntPtr hwnd)
    {
        int val = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref val, sizeof(int));
    }

    /// <summary>
    /// Applique un Acrylic "blur" via SetWindowCompositionAttribute.
    /// Fonctionne sur Win10 1903+ et Win11 avant 22H2.
    /// tintColor = couleur ARGB de teinture (ex: Color.FromArgb(180, 30, 30, 30)).
    /// </summary>
    public static void ApplyAcrylicLegacy(IntPtr hwnd, Color tintColor)
    {
        int gradient = (tintColor.A << 24)
                     | (tintColor.B << 16)
                     | (tintColor.G << 8)
                     |  tintColor.R;

        var policy = new AccentPolicy
        {
            AccentState   = 4,     // ACCENT_ENABLE_ACRYLICBLURBEHIND
            AccentFlags   = 0x20,
            GradientColor = gradient,
            AnimationId   = 0
        };
        int dataSize = Marshal.SizeOf(policy);
        IntPtr dataPtr = Marshal.AllocHGlobal(dataSize);
        try
        {
            Marshal.StructureToPtr(policy, dataPtr, false);
            var attrData = new WindowCompositionAttributeData
            {
                Attribute  = 19,   // WCA_ACCENT_POLICY
                Data       = dataPtr,
                SizeOfData = dataSize
            };
            SetWindowCompositionAttribute(hwnd, ref attrData);
        }
        finally
        {
            Marshal.FreeHGlobal(dataPtr);
        }
    }

    /// <summary>Désactive tout effet DWM (retour à un fond opaque).</summary>
    public static void DisableBackdrop(IntPtr hwnd)
    {
        if (IsWin11_22H2OrLater())
        {
            int val = (int)DWM_SYSTEMBACKDROP_TYPE.None;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref val, sizeof(int));
        }
        else
        {
            var policy = new AccentPolicy { AccentState = 0 }; // ACCENT_DISABLED
            int dataSize = Marshal.SizeOf(policy);
            IntPtr dataPtr = Marshal.AllocHGlobal(dataSize);
            try
            {
                Marshal.StructureToPtr(policy, dataPtr, false);
                var attrData = new WindowCompositionAttributeData
                {
                    Attribute  = 19,
                    Data       = dataPtr,
                    SizeOfData = dataSize
                };
                SetWindowCompositionAttribute(hwnd, ref attrData);
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }
    }

    /// <summary>Active/désactive le dark mode DWM sur la barre de titre native.</summary>
    public static void SetDarkTitleBar(IntPtr hwnd, bool dark)
    {
        int val = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
    }

    /// <summary>
    /// Active/désactive le dark mode DWM via Vanara.PInvoke.DwmApi.
    /// Fallback sur l'attribut 19 (pre-build 22000) en cas d'échec.
    /// Silencieux si le système ne supporte pas l'API.
    /// </summary>
    public static void SetTitleBarDarkMode(IntPtr hwnd, bool dark)
    {
        try
        {
            // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 11 / Win10 20H1+)
            var attr = DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;
            int val = dark ? 1 : 0;
            DwmApi.DwmSetWindowAttribute(hwnd, attr, val);
        }
        catch
        {
            try
            {
                // Fallback attribut 19 (Windows 10 pre-20H1)
                int val = dark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, 19, ref val, sizeof(int));
            }
            catch { }
        }
    }

    /// <summary>
    /// Point d'entrée principal : choisit automatiquement le meilleur effet
    /// disponible selon la version de Windows et le type demandé.
    /// </summary>
    public static void ApplyBestEffect(IntPtr hwnd, BackdropEffect effect, Color fallbackTint)
    {
        DwmIsCompositionEnabled(out bool comp);
        if (!comp) return; // VM ou RDP sans DWM — abandon silencieux

        if (effect == BackdropEffect.None)
        {
            DisableBackdrop(hwnd);
            return;
        }

        if (IsWin11_22H2OrLater())
        {
            int val = effect switch
            {
                BackdropEffect.Mica    => (int)DWM_SYSTEMBACKDROP_TYPE.Mica,
                BackdropEffect.Acrylic => (int)DWM_SYSTEMBACKDROP_TYPE.Acrylic,
                BackdropEffect.Tabbed  => (int)DWM_SYSTEMBACKDROP_TYPE.Tabbed,
                _                      => (int)DWM_SYSTEMBACKDROP_TYPE.None
            };
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref val, sizeof(int));
        }
        else if (IsWin11OrLater() && effect == BackdropEffect.Mica)
        {
            ApplyMicaLegacy(hwnd);
        }
        else
        {
            ApplyAcrylicLegacy(hwnd, fallbackTint);
        }
    }
}
