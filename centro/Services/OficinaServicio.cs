using Microsoft.Win32;

namespace CentroAether.Services;

/// <summary>
/// Protección de Office: detecta Office y gestiona la seguridad de macros
/// (VBAWarnings) en HKCU. No desactiva nada de Office; solo refuerza la seguridad.
/// </summary>
public static class OficinaServicio
{
    private static readonly string[] Apps =
        { "Word", "Excel", "PowerPoint", "Outlook", "Access", "Publisher" };

    private static RegistryKey Hkcu =>
        RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

    public static bool OfficeInstalado()
    {
        try
        {
            using RegistryKey? k = RegistryKey
                .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Microsoft\Office\16.0");
            if (k is not null)
            {
                return true;
            }
        }
        catch { /* sigue */ }

        try
        {
            using RegistryKey? k = RegistryKey
                .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\winword.exe");
            return k is not null;
        }
        catch { return false; }
    }

    /// <summary>Macros protegidas = Word exige aviso o firma (VBAWarnings >= 2).</summary>
    public static bool MacrosProtegidas()
    {
        try
        {
            using RegistryKey? k = Hkcu.OpenSubKey(@"Software\Microsoft\Office\16.0\Word\Security");
            if (k?.GetValue("VBAWarnings") is int v)
            {
                return v >= 2;
            }
        }
        catch { /* no protegidas */ }
        return false;
    }

    /// <summary>Aplica "deshabilitar macros con notificación" (VBAWarnings = 2) a todo Office.</summary>
    public static bool ProtegerMacros()
    {
        bool algo = false;
        foreach (string app in Apps)
        {
            try
            {
                using RegistryKey k = Hkcu.CreateSubKey($@"Software\Microsoft\Office\16.0\{app}\Security");
                k.SetValue("VBAWarnings", 2, RegistryValueKind.DWord);
                algo = true;
            }
            catch { /* esa app no está; siguiente */ }
        }
        return algo;
    }
}
