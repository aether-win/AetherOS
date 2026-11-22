using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace CentroAether.Controles;

/// <summary>Datos de la navegación hacia una página (equivalente al de WinUI).</summary>
public sealed class NavigationEventArgs
{
    public NavigationEventArgs(object? parameter) => Parameter = parameter;

    public object? Parameter { get; }
}

/// <summary>
/// Base de todas las páginas del Centro. La ventana principal crea una página nueva en
/// cada navegación (como el Frame de WinUI) y llama a OnNavigatedTo al mostrarla y a
/// OnNavigatedFrom al dejarla (para detener temporizadores, por ejemplo).
/// </summary>
public class PaginaBase : UserControl
{
    protected virtual void OnNavigatedTo(NavigationEventArgs e)
    {
    }

    protected virtual void OnNavigatedFrom(NavigationEventArgs e)
    {
    }

    /// <summary>Abre otra sección del Centro (accesos rápidos, enlaces entre secciones).</summary>
    protected void IrA(string id) => (Window.GetWindow(this) as MainWindow)?.IrA(id);

    /// <summary>Refresca el modo que muestra el menú lateral después de cambiarlo.</summary>
    protected void RefrescarModoMenu() => _ = (Window.GetWindow(this) as MainWindow)?.ActualizarModoMenu();

    internal void Entrar(object? parametro) => OnNavigatedTo(new NavigationEventArgs(parametro));

    internal void Salir() => OnNavigatedFrom(new NavigationEventArgs(null));
}

/// <summary>Utilidades del sistema que antes daba WinRT (Windows.System.Launcher).</summary>
public static class Sistema
{
    /// <summary>Abre una dirección (https://, steam://, ms-settings:…) o una ruta con el programa asociado.</summary>
    public static void Abrir(string destino)
    {
        Process.Start(new ProcessStartInfo { FileName = destino, UseShellExecute = true })?.Dispose();
    }
}
