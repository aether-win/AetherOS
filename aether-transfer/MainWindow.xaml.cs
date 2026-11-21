using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;
using AetherTransfer.Connections;
using AetherTransfer.Models;
using AetherTransfer.Protocols;
using AetherTransfer.TransferEngine;
using AetherTransfer.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace AetherTransfer;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<ElementoRemoto> _local = new();
    private readonly ObservableCollection<ElementoRemoto> _remoto = new();
    private readonly ObservableCollection<string> _log = new();

    private IServidorRemoto? _servidor;
    private GestorTransferencias _gestor = null!;
    private string _rutaLocal = "";
    private string _rutaRemota = "/";

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "AETHER TRANSFER";

        if (this.AppWindow is not null)
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1300, 840));
            try { this.AppWindow.SetIcon("Assets\\aether-transfer.ico"); } catch { /* opcional */ }
        }

        LocalLista.ItemsSource = _local;
        RemotoLista.ItemsSource = _remoto;
        LogLista.ItemsSource = _log;

        _gestor = new GestorTransferencias(() => _servidor, OnTransferenciaCompletada);
        ColaLista.ItemsSource = _gestor.Items;
        _gestor.Items.CollectionChanged += OnColaChanged;
        ActualizarContadores();

        CargarLocal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Log("Aether Transfer listo.");
    }

    private void OnTransferenciaCompletada()
    {
        CargarLocal(_rutaLocal);
        _ = CargarRemoto(_rutaRemota);
    }

    private void OnColaChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (object o in e.NewItems)
            {
                if (o is TransferItem ti)
                {
                    ti.PropertyChanged += (_, _) => ActualizarContadores();
                }
            }
        }
        ActualizarContadores();
    }

    private void ActualizarContadores()
    {
        int enCola = _gestor.Items.Count(i => i.Estado is EstadoTransfer.EnCola or EstadoTransfer.Transfiriendo or EstadoTransfer.Pausado);
        int fallidas = _gestor.Items.Count(i => i.Estado is EstadoTransfer.Fallido or EstadoTransfer.Cancelado);
        int hechas = _gestor.Items.Count(i => i.Estado == EstadoTransfer.Completado);
        TabCola.Text = $"Archivos en cola ({enCola})";
        TabFallidas.Text = $"Transferencias fallidas ({fallidas})";
        TabHechas.Text = $"Transferencias satisfactorias ({hechas})";
        ColaVacia.Visibility = _gestor.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pausar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TransferItem item)
        {
            return;
        }
        if (item.Estado == EstadoTransfer.Transfiriendo)
        {
            _gestor.Pausar(item);
        }
        else
        {
            _gestor.Reanudar(item);
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TransferItem item)
        {
            return;
        }
        if (item.Estado is EstadoTransfer.Completado or EstadoTransfer.Cancelado or EstadoTransfer.Fallido)
        {
            _gestor.Quitar(item);
        }
        else
        {
            _gestor.Cancelar(item);
        }
    }

    // ── Registro ─────────────────────────────────────────────────────────────

    private void Log(string msg)
    {
        _log.Add($"[{DateTime.Now:HH:mm:ss}]  {msg}");
        while (_log.Count > 200)
        {
            _log.RemoveAt(0);
        }
    }

    // ── Sitio local ──────────────────────────────────────────────────────────

    private void CargarLocal(string ruta)
    {
        try
        {
            var dir = new DirectoryInfo(ruta);
            if (!dir.Exists)
            {
                return;
            }

            _local.Clear();

            DirectoryInfo? padre = dir.Parent;
            if (padre is not null)
            {
                _local.Add(new ElementoRemoto { Nombre = "..", EsCarpeta = true, Ruta = padre.FullName });
            }

            try
            {
                foreach (DirectoryInfo d in dir.GetDirectories().OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    _local.Add(new ElementoRemoto { Nombre = d.Name, EsCarpeta = true, Ruta = d.FullName, Modificado = d.LastWriteTime });
                }
                foreach (FileInfo f in dir.GetFiles().OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    _local.Add(new ElementoRemoto { Nombre = f.Name, EsCarpeta = false, Tamano = f.Length, Ruta = f.FullName, Modificado = f.LastWriteTime });
                }
            }
            catch (UnauthorizedAccessException)
            {
                Log($"Sin permiso para leer: {ruta}");
            }

            _rutaLocal = dir.FullName;
            TxtLocalRuta.Text = _rutaLocal;
            int carpetas = _local.Count(e => e.EsCarpeta && e.Nombre != "..");
            int archivos = _local.Count(e => !e.EsCarpeta);
            LocalInfo.Text = $"{archivos} archivo(s) y {carpetas} directorio(s)";
        }
        catch (Exception ex)
        {
            Log($"Error local: {ex.Message}");
        }
    }

    private void LocalSubir_Click(object sender, RoutedEventArgs e)
    {
        DirectoryInfo? padre = Directory.GetParent(_rutaLocal);
        if (padre is not null)
        {
            CargarLocal(padre.FullName);
        }
    }

    private void LocalActualizar_Click(object sender, RoutedEventArgs e) => CargarLocal(_rutaLocal);

    private void LocalLista_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (LocalLista.SelectedItem is not ElementoRemoto item)
        {
            return;
        }
        if (item.EsCarpeta)
        {
            CargarLocal(item.Ruta);
        }
        else
        {
            SubirUno(item);
        }
    }

    // ── Conexión ─────────────────────────────────────────────────────────────

    private async void Conectar_Click(object sender, RoutedEventArgs e)
    {
        int puerto = int.TryParse(TxtPuerto.Text.Trim(), out int p) ? p : 21;
        Protocolo proto = puerto switch
        {
            22 => Protocolo.Sftp,
            990 => Protocolo.FtpsImplicito,
            _ => Protocolo.Ftp,
        };

        await ConectarCon(new ConexionInfo
        {
            Host = TxtHost.Text.Trim(),
            Usuario = TxtUsuario.Text.Trim(),
            Password = TxtPass.Password,
            Puerto = puerto,
            Protocolo = proto,
        });
    }

    private async void AdminSitios_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new DialogoSitios { XamlRoot = Raiz.XamlRoot };
        ContentDialogResult res = await dlg.ShowAsync();
        if (res == ContentDialogResult.Primary && dlg.SitioParaConectar is SitioGuardado s)
        {
            ConexionInfo info = s.AConexion();
            TxtHost.Text = info.Host;
            TxtUsuario.Text = info.Usuario;
            TxtPass.Password = info.Password;
            TxtPuerto.Text = info.Puerto.ToString();
            await ConectarCon(info);
        }
    }

    private async void Sincronizar_Click(object sender, RoutedEventArgs e)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            Log("Conéctate primero para sincronizar.");
            return;
        }
        var dlg = new DialogoSync(_local.ToList(), _remoto.ToList()) { XamlRoot = Raiz.XamlRoot };
        if (await dlg.ShowAsync() == ContentDialogResult.Primary)
        {
            foreach (AccionSync a in dlg.Acciones)
            {
                _gestor.Encolar(new TransferItem
                {
                    Nombre = a.Nombre,
                    RutaLocal = Path.Combine(_rutaLocal, a.Nombre),
                    RutaRemota = UnirPosix(_rutaRemota, a.Nombre),
                    Subida = a.Subida,
                    Tamano = a.Tamano,
                });
            }
            Log($"Sincronización: {dlg.Acciones.Count} archivo(s) en cola.");
        }
    }

    private async Task ConectarCon(ConexionInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.Host))
        {
            Log("Escribe la dirección del servidor.");
            return;
        }

        BtnConectar.IsEnabled = false;
        await DesconectarInterno();

        _servidor = info.Protocolo == Protocolo.Sftp ? new ServidorSftp() : new ServidorFtp();
        Log($"Conectando a {info.Host}:{info.Puerto} ({info.Protocolo})…");

        try
        {
            await _servidor.ConectarAsync(info);
            Log("Sesión iniciada correctamente.");
            EstadoPunto.Fill = new SolidColorBrush(Colors.LightGreen);
            TxtEstado.Text = $"Conectado: {info.Host} ({info.Protocolo})";
            string inicio = string.IsNullOrWhiteSpace(info.DirectorioInicial) ? "/" : info.DirectorioInicial;
            _rutaRemota = inicio;
            await CargarRemoto(inicio);
        }
        catch (Exception ex)
        {
            Log($"Error de conexión: {ex.Message}");
            EstadoPunto.Fill = new SolidColorBrush(Colors.OrangeRed);
            TxtEstado.Text = "Error de conexión";
            await DesconectarInterno();
        }
        finally
        {
            BtnConectar.IsEnabled = true;
        }
    }

    private async void Desconectar_Click(object sender, RoutedEventArgs e)
    {
        await DesconectarInterno();
        Log("Desconectado.");
        EstadoPunto.Fill = new SolidColorBrush(Colors.Gray);
        TxtEstado.Text = "No conectado";
        _remoto.Clear();
        RemotoInfo.Text = "No conectado";
        TxtRemotoRuta.Text = "";
    }

    private async Task DesconectarInterno()
    {
        if (_servidor is not null)
        {
            try { await _servidor.DesconectarAsync(); } catch { /* ignore */ }
            _servidor = null;
        }
    }

    // ── Sitio remoto ─────────────────────────────────────────────────────────

    private async Task CargarRemoto(string ruta)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            return;
        }
        try
        {
            var items = await _servidor.ListarAsync(ruta);
            _remoto.Clear();

            if (ruta != "/")
            {
                _remoto.Add(new ElementoRemoto { Nombre = "..", EsCarpeta = true, Ruta = PadrePosix(ruta) });
            }
            foreach (ElementoRemoto i in items.Where(x => x.EsCarpeta).OrderBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase))
            {
                _remoto.Add(ConRuta(i, ruta));
            }
            foreach (ElementoRemoto i in items.Where(x => !x.EsCarpeta).OrderBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase))
            {
                _remoto.Add(ConRuta(i, ruta));
            }

            _rutaRemota = ruta;
            TxtRemotoRuta.Text = ruta;
            int carpetas = items.Count(x => x.EsCarpeta);
            int archivos = items.Count(x => !x.EsCarpeta);
            RemotoInfo.Text = $"{archivos} archivo(s) y {carpetas} directorio(s)";
        }
        catch (Exception ex)
        {
            Log($"Error al listar {ruta}: {ex.Message}");
        }
    }

    private static ElementoRemoto ConRuta(ElementoRemoto i, string dir) => new()
    {
        Nombre = i.Nombre,
        EsCarpeta = i.EsCarpeta,
        Tamano = i.Tamano,
        Modificado = i.Modificado,
        Ruta = UnirPosix(dir, i.Nombre),
    };

    private void RemotoSubir_Click(object sender, RoutedEventArgs e) => _ = CargarRemoto(PadrePosix(_rutaRemota));

    private void RemotoActualizar_Click(object sender, RoutedEventArgs e) => _ = CargarRemoto(_rutaRemota);

    private async void RemotoLista_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (RemotoLista.SelectedItem is not ElementoRemoto item)
        {
            return;
        }
        if (item.EsCarpeta)
        {
            await CargarRemoto(item.Ruta);
        }
        else
        {
            DescargarUno(item);
        }
    }

    // ── Transferencias ───────────────────────────────────────────────────────

    private void Subir_Click(object sender, RoutedEventArgs e)
    {
        foreach (ElementoRemoto item in LocalLista.SelectedItems.OfType<ElementoRemoto>().Where(x => !x.EsCarpeta).ToList())
        {
            SubirUno(item);
        }
    }

    private void SubirUno(ElementoRemoto archivo)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            Log("Conéctate primero para subir archivos.");
            return;
        }
        _gestor.Encolar(new TransferItem
        {
            Nombre = archivo.Nombre,
            RutaLocal = archivo.Ruta,
            RutaRemota = UnirPosix(_rutaRemota, archivo.Nombre),
            Subida = true,
            Tamano = archivo.Tamano,
        });
        Log($"En cola (subir): {archivo.Nombre}");
    }

    private void Descargar_Click(object sender, RoutedEventArgs e)
    {
        foreach (ElementoRemoto item in RemotoLista.SelectedItems.OfType<ElementoRemoto>().Where(x => !x.EsCarpeta).ToList())
        {
            DescargarUno(item);
        }
    }

    private void DescargarUno(ElementoRemoto archivo)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            return;
        }
        _gestor.Encolar(new TransferItem
        {
            Nombre = archivo.Nombre,
            RutaLocal = Path.Combine(_rutaLocal, archivo.Nombre),
            RutaRemota = archivo.Ruta,
            Subida = false,
            Tamano = archivo.Tamano,
        });
        Log($"En cola (descargar): {archivo.Nombre}");
    }

    // ── Menús contextuales ───────────────────────────────────────────────────

    private void Lista_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is ListView lv && e.OriginalSource is FrameworkElement fe && fe.DataContext is ElementoRemoto it)
        {
            lv.SelectedItem = it;
        }
    }

    private async void LocalNuevaCarpeta_Click(object sender, RoutedEventArgs e)
    {
        string? nombre = await PedirTexto("Nueva carpeta", "Nueva carpeta");
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.Combine(_rutaLocal, nombre));
            CargarLocal(_rutaLocal);
        }
        catch (Exception ex) { Log($"Error al crear carpeta: {ex.Message}"); }
    }

    private async void LocalRenombrar_Click(object sender, RoutedEventArgs e)
    {
        if (LocalLista.SelectedItem is not ElementoRemoto item || item.Nombre == "..")
        {
            return;
        }
        string? nuevo = await PedirTexto("Renombrar", item.Nombre);
        if (string.IsNullOrWhiteSpace(nuevo) || nuevo == item.Nombre)
        {
            return;
        }
        try
        {
            string destino = Path.Combine(_rutaLocal, nuevo);
            if (item.EsCarpeta)
            {
                Directory.Move(item.Ruta, destino);
            }
            else
            {
                File.Move(item.Ruta, destino);
            }
            CargarLocal(_rutaLocal);
        }
        catch (Exception ex) { Log($"Error al renombrar: {ex.Message}"); }
    }

    private async void LocalEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (LocalLista.SelectedItem is not ElementoRemoto item || item.Nombre == "..")
        {
            return;
        }
        if (!await Confirmar($"¿Eliminar «{item.Nombre}» del equipo?"))
        {
            return;
        }
        try
        {
            if (item.EsCarpeta)
            {
                Directory.Delete(item.Ruta, true);
            }
            else
            {
                File.Delete(item.Ruta);
            }
            CargarLocal(_rutaLocal);
        }
        catch (Exception ex) { Log($"Error al eliminar: {ex.Message}"); }
    }

    private async void RemotoNuevaCarpeta_Click(object sender, RoutedEventArgs e)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            return;
        }
        string? nombre = await PedirTexto("Nueva carpeta remota", "nueva_carpeta");
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }
        try
        {
            await _servidor.CrearCarpetaAsync(UnirPosix(_rutaRemota, nombre));
            await CargarRemoto(_rutaRemota);
        }
        catch (Exception ex) { Log($"Error al crear carpeta remota: {ex.Message}"); }
    }

    private async void RemotoRenombrar_Click(object sender, RoutedEventArgs e)
    {
        if (_servidor is null || RemotoLista.SelectedItem is not ElementoRemoto item || item.Nombre == "..")
        {
            return;
        }
        string? nuevo = await PedirTexto("Renombrar", item.Nombre);
        if (string.IsNullOrWhiteSpace(nuevo) || nuevo == item.Nombre)
        {
            return;
        }
        try
        {
            await _servidor.RenombrarAsync(item.Ruta, UnirPosix(_rutaRemota, nuevo));
            await CargarRemoto(_rutaRemota);
        }
        catch (Exception ex) { Log($"Error al renombrar: {ex.Message}"); }
    }

    private async void RemotoEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (_servidor is null || RemotoLista.SelectedItem is not ElementoRemoto item || item.Nombre == "..")
        {
            return;
        }
        if (!await Confirmar($"¿Eliminar «{item.Nombre}» del servidor?"))
        {
            return;
        }
        try
        {
            await _servidor.EliminarAsync(item.Ruta, item.EsCarpeta);
            await CargarRemoto(_rutaRemota);
        }
        catch (Exception ex) { Log($"Error al eliminar: {ex.Message}"); }
    }

    private async Task<string?> PedirTexto(string titulo, string valor)
    {
        var tb = new TextBox { Text = valor };
        var dlg = new ContentDialog
        {
            Title = titulo,
            Content = tb,
            PrimaryButtonText = "Aceptar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            RequestedTheme = ElementTheme.Dark,
            XamlRoot = Raiz.XamlRoot,
        };
        ContentDialogResult r = await dlg.ShowAsync();
        return r == ContentDialogResult.Primary ? tb.Text.Trim() : null;
    }

    private async Task<bool> Confirmar(string mensaje)
    {
        var dlg = new ContentDialog
        {
            Title = "Confirmar",
            Content = mensaje,
            PrimaryButtonText = "Sí",
            CloseButtonText = "No",
            RequestedTheme = ElementTheme.Dark,
            XamlRoot = Raiz.XamlRoot,
        };
        return await dlg.ShowAsync() == ContentDialogResult.Primary;
    }

    // ── Menú ─────────────────────────────────────────────────────────────────

    private void Salir_Click(object sender, RoutedEventArgs e) => this.Close();

    // ── Utilidades de rutas POSIX ────────────────────────────────────────────

    private static string UnirPosix(string dir, string nombre)
    {
        if (string.IsNullOrEmpty(dir) || dir == "/")
        {
            return "/" + nombre;
        }
        return dir.TrimEnd('/') + "/" + nombre;
    }

    private static string PadrePosix(string ruta)
    {
        string p = ruta.TrimEnd('/');
        if (p.Length == 0)
        {
            return "/";
        }
        int i = p.LastIndexOf('/');
        return i <= 0 ? "/" : p[..i];
    }
}
