using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using CentroAether.Services;

namespace CentroAether.Pages;

/// <summary>
/// Oficina: blindaje de Office contra los mods de Windhawk (exclusión global), macros sin
/// bloqueo, desbloqueo de archivos que llegaron de otra PC y modo enfocado.
/// </summary>
public sealed partial class OficinaPage : PaginaBase
{
    public OficinaPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Refrescar();
    }

    private void Refrescar()
    {
        FilaOffice.Descripcion = OficinaServicio.OfficeInstalado()
            ? "Microsoft Office detectado en este equipo."
            : "No se detectó Microsoft Office. La protección queda lista para cuando lo instales.";

        // Blindaje contra mods
        if (!OficinaServicio.WindhawkInstalado())
        {
            FilaBlindaje.Descripcion = "Windhawk no está instalado, así que ningún mod puede tocar Office. Si lo instalas, Aether Toolbox protege Office automáticamente.";
            FilaBlindaje.Accion = Ui.Insignia("Protegido", Ui.Hex("#3FBF7F"));
        }
        else if (OficinaServicio.OfficeBlindado())
        {
            FilaBlindaje.Descripcion = "Word, Excel, PowerPoint, Outlook, OneNote, Access, Publisher, Visio y Project están fuera del alcance de Windhawk: ningún mod (transparencias, vidrio, colores…) los puede alterar. Aether Toolbox lo vigila en segundo plano.";
            FilaBlindaje.Accion = Ui.Insignia("Protegido", Ui.Hex("#3FBF7F"));
        }
        else
        {
            FilaBlindaje.Descripcion = "Algunos mods de Windhawk pueden alterar Office (por ejemplo, texto que se ve lavado o invisible en Word). Protégelo para dejarlo fuera de todos los mods.";
            FilaBlindaje.Accion = BtnBlindar;
        }

        // Macros
        if (OficinaServicio.MacrosSinBloqueo())
        {
            FilaMacros.Descripcion = "Las macros VBA funcionan sin aviso y los archivos que vienen de otra PC no se bloquean ni abren en Vista protegida.";
            FilaMacros.Accion = Ui.Insignia("Listo", Ui.Hex("#3FBF7F"));
        }
        else
        {
            FilaMacros.Descripcion = "Office puede bloquear tus macros o abrir en Vista protegida los archivos que vienen de otra PC. Aplica los ajustes de Aether.";
            FilaMacros.Accion = BtnMacros;
        }
    }

    private async void Blindar_Click(object sender, RoutedEventArgs e)
    {
        BtnBlindar.IsEnabled = false;
        Aviso("Protegiendo Office…", InfoBarSeverity.Informational);
        bool ok = await Task.Run(() => OficinaServicio.BlindarOffice());
        BtnBlindar.IsEnabled = true;
        Aviso(ok
            ? "Office quedó protegido. Cierra y vuelve a abrir Word, Excel o PowerPoint si estaban abiertos."
            : "No se pudo proteger Office. Abre Windhawk una vez y vuelve a intentarlo.",
            ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        Refrescar();
    }

    private void Macros_Click(object sender, RoutedEventArgs e)
    {
        bool ok = OficinaServicio.AplicarMacrosSinBloqueo();
        Aviso(ok
            ? "Listo. Cierra y vuelve a abrir Office para que tome los cambios."
            : "No se pudieron aplicar los ajustes de macros.",
            ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        Refrescar();
    }

    private async void Desbloquear_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new Microsoft.Win32.OpenFolderDialog { Title = "Elige la carpeta con los archivos a desbloquear" };
        if (dialogo.ShowDialog(Window.GetWindow(this)) != true || string.IsNullOrEmpty(dialogo.FolderName))
        {
            return;
        }
        string carpeta = dialogo.FolderName;
        BtnDesbloquear.IsEnabled = false;
        Aviso("Desbloqueando archivos…", InfoBarSeverity.Informational);
        int cuenta = await Task.Run(() => OficinaServicio.DesbloquearCarpeta(carpeta));
        BtnDesbloquear.IsEnabled = true;
        Aviso(cuenta == 0
            ? "No había archivos de Office bloqueados en esa carpeta."
            : $"Listo: se desbloquearon {cuenta} archivo(s) de Office.",
            InfoBarSeverity.Success);
    }

    private void Enfocado_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Sistema.Abrir("ms-settings:quiethours");
        }
        catch
        {
            Aviso("No se pudo abrir la configuración de concentración.", InfoBarSeverity.Warning);
        }
    }

    private void Aviso(string texto, InfoBarSeverity tipo)
    {
        Estado.Severity = tipo;
        Estado.Message = texto;
        Estado.IsOpen = true;
    }
}
