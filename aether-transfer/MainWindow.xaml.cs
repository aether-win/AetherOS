using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AetherTransfer.Connections;
using AetherTransfer.Models;
using AetherTransfer.Protocols;
using AetherTransfer.Settings;
using AetherTransfer.Views;
using Microsoft.Win32;

namespace AetherTransfer;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ElementoRemoto> _remoto = new();
    private readonly ObservableCollection<string> _log = new();
    private readonly ObservableCollection<SitioGuardado> _sitios = new();

    private IServidorRemoto? _servidor;
    private GestorTransferencias _gestor = null!;
    private string _rutaRemota = "/";

    // Historial de navegación (para ‹ ›)
    private readonly List<string> _hist = new();
    private int _histPos = -1;

    private System.Windows.Point _inicioArrastre;
    private bool _cargandoSitio;

    public MainWindow()
    {
        InitializeComponent();
        Tema.BarraOscura(this);

        LogLista.ItemsSource = _log;
        RemotoLista.ItemsSource = _remoto;

        _gestor = new GestorTransferencias(() => _servidor, OnTransferenciaCompletada);
        ColaLista.ItemsSource = _gestor.Items;
        _gestor.Items.CollectionChanged += OnColaChanged;
        ActualizarContadores();

        CargarSitios();
        Log("Aether Transfer listo. Escribe un servidor o elige uno guardado.");
    }

    // ── Registro (con auto-scroll) ───────────────────────────────────────────

    private void Log(string msg)
    {
        _log.Add($"[{DateTime.Now:HH:mm:ss}]  {msg}");
        while (_log.Count > 300)
        {
            _log.RemoveAt(0);
        }
        if (_log.Count > 0)
        {
            LogLista.ScrollIntoView(_log[^1]);
        }
    }

    private void OnTransferenciaCompletada() => _ = CargarRemoto(_rutaRemota);

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
        TabFallidas.Text = $"Fallidas ({fallidas})";
        TabHechas.Text = $"Satisfactorias ({hechas})";
        ColaVacia.Visibility = _gestor.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pausar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TransferItem item)
        {
            return;
        }
        if (item.Estado == EstadoTransfer.Transfiriendo) { _gestor.Pausar(item); }
        else { _gestor.Reanudar(item); }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TransferItem item)
        {
            return;
        }
        if (item.Estado is EstadoTransfer.Completado or EstadoTransfer.Cancelado or EstadoTransfer.Fallido) { _gestor.Quitar(item); }
        else { _gestor.Cancelar(item); }
    }

    // ── Sitios guardados ─────────────────────────────────────────────────────

    private void CargarSitios()
    {
        _cargandoSitio = true;
        _sitios.Clear();
        foreach (SitioGuardado s in AlmacenSitios.Cargar())
        {
            _sitios.Add(s);
        }
        CmbSitios.ItemsSource = null;
        CmbSitios.ItemsSource = _sitios;
        CmbSitios.SelectedIndex = -1;
        _cargandoSitio = false;
    }

    private void CmbSitios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargandoSitio || CmbSitios.SelectedItem is not SitioGuardado s)
        {
            return;
        }
        ConexionInfo info = s.AConexion();
        TxtHost.Text = info.Host;
        TxtUsuario.Text = info.Usuario;
        TxtPass.Password = info.Password;
        TxtPuerto.Text = info.Puerto.ToString();
        _ = ConectarCon(info);
        // permitir volver a elegir el mismo
        _cargandoSitio = true;
        CmbSitios.SelectedIndex = -1;
        _cargandoSitio = false;
    }

    private void AdminSitios_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new DialogoSitios { Owner = this };
        bool? r = dlg.ShowDialog();
        CargarSitios();
        if (r == true && dlg.SitioParaConectar is SitioGuardado s)
        {
            ConexionInfo info = s.AConexion();
            TxtHost.Text = info.Host;
            TxtUsuario.Text = info.Usuario;
            TxtPass.Password = info.Password;
            TxtPuerto.Text = info.Puerto.ToString();
            _ = ConectarCon(info);
        }
    }

    // Guarda la conexión actual como "reciente" si no existe ya (host+puerto+usuario).
    private void GuardarReciente(ConexionInfo info)
    {
        bool existe = _sitios.Any(x =>
            string.Equals(x.Host, info.Host, StringComparison.OrdinalIgnoreCase) &&
            x.Puerto == info.Puerto &&
            string.Equals(x.Usuario, info.Usuario, StringComparison.OrdinalIgnoreCase));
        if (existe)
        {
            return;
        }
        var nuevo = AlmacenSitios.Crear(
            string.IsNullOrWhiteSpace(info.Nombre) ? $"{info.Host}:{info.Puerto}" : info.Nombre,
            info.Host, info.Puerto, info.Protocolo, info.Usuario, info.Password,
            string.IsNullOrWhiteSpace(info.DirectorioInicial) ? "/" : info.DirectorioInicial);
        var lista = _sitios.ToList();
        lista.Add(nuevo);
        AlmacenSitios.Guardar(lista);
        CargarSitios();
    }

    // ── Conexión ─────────────────────────────────────────────────────────────

    private async void Conectar_Click(object sender, RoutedEventArgs e)
    {
        int puerto = int.TryParse(TxtPuerto.Text.Trim(), out int p) && p > 0 ? p : 21;
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

    private async Task ConectarCon(ConexionInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.Host))
        {
            Log("Escribe la dirección del servidor.");
            return;
        }
        if (info.Puerto <= 0)
        {
            info.Puerto = ConexionInfo.PuertoPorDefecto(info.Protocolo);
        }
        if (string.IsNullOrWhiteSpace(info.Usuario))
        {
            info.Usuario = "anonymous";
            if (string.IsNullOrEmpty(info.Password))
            {
                info.Password = "anonymous@aether";
            }
        }

        BtnConectar.IsEnabled = false;
        await DesconectarInterno();

        _servidor = info.Protocolo == Protocolo.Sftp ? new ServidorSftp() : new ServidorFtp();
        Log($"Conectando a {info.Host}:{info.Puerto} ({info.Protocolo})…");

        try
        {
            await _servidor.ConectarAsync(info);
            Log("Sesión iniciada correctamente.");
            EstadoPunto.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xD1, 0x7A));
            TxtEstado.Text = $"Conectado: {info.Host} ({info.Protocolo})";
            string inicio = string.IsNullOrWhiteSpace(info.DirectorioInicial) ? "/" : info.DirectorioInicial;
            _hist.Clear();
            _histPos = -1;
            await Navegar(inicio, true);
            GuardarReciente(info);
        }
        catch (Exception ex)
        {
            Log($"Error de conexión: {ex.Message}");
            EstadoPunto.Fill = new SolidColorBrush(Color.FromRgb(0xE5, 0x5A, 0x3A));
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
        EstadoPunto.Fill = new SolidColorBrush(Color.FromRgb(0x6B, 0x7B, 0x90));
        TxtEstado.Text = "No conectado";
        _remoto.Clear();
        RemotoInfo.Text = "No conectado";
        TxtRuta.Text = "";
        _hist.Clear();
        _histPos = -1;
    }

    private async Task DesconectarInterno()
    {
        if (_servidor is not null)
        {
            try { await _servidor.DesconectarAsync(); } catch { /* ignore */ }
            _servidor = null;
        }
    }

    private void Config_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new DialogoConfig { Owner = this };
        dlg.ShowDialog();
    }

    // ── Navegación remota ────────────────────────────────────────────────────

    private async Task Navegar(string ruta, bool nuevoHistorial)
    {
        bool ok = await CargarRemoto(ruta);
        if (ok && nuevoHistorial)
        {
            if (_histPos < _hist.Count - 1)
            {
                _hist.RemoveRange(_histPos + 1, _hist.Count - _histPos - 1);
            }
            if (_hist.Count == 0 || _hist[^1] != ruta)
            {
                _hist.Add(ruta);
            }
            _histPos = _hist.Count - 1;
        }
    }

    private void Atras_Click(object sender, RoutedEventArgs e)
    {
        if (_histPos > 0)
        {
            _histPos--;
            _ = CargarRemoto(_hist[_histPos]);
        }
    }

    private void Adelante_Click(object sender, RoutedEventArgs e)
    {
        if (_histPos < _hist.Count - 1)
        {
            _histPos++;
            _ = CargarRemoto(_hist[_histPos]);
        }
    }

    private void Arriba_Click(object sender, RoutedEventArgs e) => _ = Navegar(PadrePosix(_rutaRemota), true);

    private void RemotoActualizar_Click(object sender, RoutedEventArgs e) => _ = CargarRemoto(_rutaRemota);

    private async Task<bool> CargarRemoto(string ruta)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            return false;
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
            TxtRuta.Text = ruta;
            int carpetas = items.Count(x => x.EsCarpeta);
            int archivos = items.Count(x => !x.EsCarpeta);
            RemotoInfo.Text = $"{archivos} archivo(s) y {carpetas} directorio(s)";
            return true;
        }
        catch (Exception ex)
        {
            Log($"Error al listar {ruta}: {ex.Message}");
            return false;
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

    private async void RemotoLista_DoubleTapped(object sender, MouseButtonEventArgs e)
    {
        if (RemotoLista.SelectedItem is not ElementoRemoto item)
        {
            return;
        }
        if (item.EsCarpeta)
        {
            await Navegar(item.Ruta, true);
        }
        else
        {
            DescargarUno(item);
        }
    }

    // ── Subir / Descargar ────────────────────────────────────────────────────

    private void Subir_Click(object sender, RoutedEventArgs e)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            Log("Conéctate primero para subir archivos.");
            return;
        }
        var dlg = new OpenFileDialog { Multiselect = true, Title = "Elige archivos para subir" };
        if (dlg.ShowDialog(this) == true)
        {
            foreach (string ruta in dlg.FileNames)
            {
                EncolarSubida(ruta);
            }
        }
    }

    private void EncolarSubida(string rutaLocal)
    {
        var fi = new FileInfo(rutaLocal);
        _gestor.Encolar(new TransferItem
        {
            Nombre = fi.Name,
            RutaLocal = rutaLocal,
            RutaRemota = UnirPosix(_rutaRemota, fi.Name),
            Subida = true,
            Tamano = fi.Exists ? fi.Length : 0,
        });
        Log($"En cola (subir): {fi.Name}");
    }

    private void Descargar_Click(object sender, RoutedEventArgs e)
    {
        var sel = RemotoLista.SelectedItems.OfType<ElementoRemoto>().Where(x => !x.EsCarpeta && x.Nombre != "..").ToList();
        if (sel.Count == 0)
        {
            Log("Selecciona uno o más archivos del servidor para descargar.");
            return;
        }
        foreach (ElementoRemoto item in sel)
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
        string destino = Path.Combine(CarpetaDescargas(), archivo.Nombre);
        _gestor.Encolar(new TransferItem
        {
            Nombre = archivo.Nombre,
            RutaLocal = destino,
            RutaRemota = archivo.Ruta,
            Subida = false,
            Tamano = archivo.Tamano,
        });
        Log($"En cola (descargar → Descargas): {archivo.Nombre}");
    }

    private static string CarpetaDescargas()
    {
        string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        try { Directory.CreateDirectory(d); } catch { /* usa la que haya */ }
        return d;
    }

    // ── Menú contextual remoto ───────────────────────────────────────────────

    private void Lista_RightTapped(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView lv && (e.OriginalSource as FrameworkElement)?.DataContext is ElementoRemoto it)
        {
            lv.SelectedItem = it;
        }
    }

    private async void RemotoNuevaCarpeta_Click(object sender, RoutedEventArgs e)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            return;
        }
        string? nombre = InputBox.Pedir(this, "Nueva carpeta remota", "nueva_carpeta");
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }
        try
        {
            await _servidor.CrearCarpetaAsync(UnirPosix(_rutaRemota, nombre));
            Log($"Carpeta creada en el servidor: {nombre}");
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
        string? nuevo = InputBox.Pedir(this, "Renombrar", item.Nombre);
        if (string.IsNullOrWhiteSpace(nuevo) || nuevo == item.Nombre)
        {
            return;
        }
        try
        {
            await _servidor.RenombrarAsync(item.Ruta, UnirPosix(_rutaRemota, nuevo));
            Log($"Renombrado en el servidor: {item.Nombre} → {nuevo}");
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
        if (ConfigApp.Actual.ConfirmarEliminar && !Confirmacion.Pedir(this, $"¿Eliminar «{item.Nombre}» del servidor?"))
        {
            return;
        }
        try
        {
            await _servidor.EliminarAsync(item.Ruta, item.EsCarpeta);
            Log($"Eliminado del servidor: {item.Ruta}");
            await CargarRemoto(_rutaRemota);
        }
        catch (Exception ex) { Log($"Error al eliminar: {ex.Message}"); }
    }

    // ── Arrastrar y soltar ───────────────────────────────────────────────────

    private void DragSobreArchivos(object sender, DragEventArgs e)
    {
        e.Effects = (_servidor is { Conectado: true } && e.Data.GetDataPresent(DataFormats.FileDrop))
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    // Soltar en el panel remoto = subir.
    private void RemotoDrop(object sender, DragEventArgs e)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            Log("Conéctate primero para subir archivos.");
            return;
        }
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] rutas)
        {
            return;
        }
        int n = 0;
        foreach (string ruta in rutas)
        {
            if (File.Exists(ruta)) { EncolarSubida(ruta); n++; }
            else if (Directory.Exists(ruta)) { Log($"Las carpetas aún no se suben por arrastre: {Path.GetFileName(ruta)}"); }
        }
        if (n > 0)
        {
            Log($"Arrastre: {n} archivo(s) en cola para subir.");
        }
    }

    // Arrastrar DESDE el servidor hacia afuera (Explorador): descarga a temporal y entrega el archivo real.
    private void Remoto_MouseDown(object sender, MouseButtonEventArgs e) => _inicioArrastre = e.GetPosition(null);

    private void Remoto_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || !SuperaUmbral(e) || _servidor is null || !_servidor.Conectado)
        {
            return;
        }
        var archivos = RemotoLista.SelectedItems.OfType<ElementoRemoto>().Where(x => !x.EsCarpeta && x.Nombre != "..").ToList();
        if (archivos.Count == 0)
        {
            return;
        }

        string temp = Path.Combine(Path.GetTempPath(), "AetherTransfer");
        var rutas = new List<string>();
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            Directory.CreateDirectory(temp);
            IServidorRemoto srv = _servidor;
            foreach (ElementoRemoto f in archivos)
            {
                string destino = Path.Combine(temp, f.Nombre);
                Log($"Preparando para arrastrar: {f.Nombre}…");
                Task.Run(() => srv.DescargarAsync(f.Ruta, destino)).GetAwaiter().GetResult();
                rutas.Add(destino);
            }
        }
        catch (Exception ex)
        {
            Log($"Error al preparar el arrastre: {ex.Message}");
            Mouse.OverrideCursor = null;
            return;
        }
        Mouse.OverrideCursor = null;

        try { DragDrop.DoDragDrop(RemotoLista, new DataObject(DataFormats.FileDrop, rutas.ToArray()), DragDropEffects.Copy); }
        catch { /* arrastre cancelado */ }
    }

    private bool SuperaUmbral(MouseEventArgs e)
    {
        Vector d = _inicioArrastre - e.GetPosition(null);
        return Math.Abs(d.X) >= SystemParameters.MinimumHorizontalDragDistance
            || Math.Abs(d.Y) >= SystemParameters.MinimumVerticalDragDistance;
    }

    // ── Utilidades de rutas POSIX ────────────────────────────────────────────

    private static string UnirPosix(string dir, string nombre)
        => string.IsNullOrEmpty(dir) || dir == "/" ? "/" + nombre : dir.TrimEnd('/') + "/" + nombre;

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
