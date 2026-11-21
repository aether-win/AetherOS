using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AetherTransfer.Models;
using AetherTransfer.Protocols;
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

        CargarLocal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Log("Aether Transfer listo.");
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

    private async void LocalLista_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
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
            await SubirUno(item);
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

        var info = new ConexionInfo
        {
            Host = TxtHost.Text.Trim(),
            Usuario = TxtUsuario.Text.Trim(),
            Password = TxtPass.Password,
            Puerto = puerto,
            Protocolo = proto,
        };

        if (string.IsNullOrWhiteSpace(info.Host))
        {
            Log("Escribe la dirección del servidor.");
            return;
        }

        BtnConectar.IsEnabled = false;
        await DesconectarInterno();

        _servidor = proto == Protocolo.Sftp ? new ServidorSftp() : new ServidorFtp();
        Log($"Conectando a {info.Host}:{info.Puerto} ({proto})…");

        try
        {
            await _servidor.ConectarAsync(info);
            Log("Sesión iniciada correctamente.");
            EstadoPunto.Fill = new SolidColorBrush(Colors.LightGreen);
            TxtEstado.Text = $"Conectado: {info.Host} ({proto})";
            _rutaRemota = "/";
            await CargarRemoto("/");
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
            await DescargarUno(item);
        }
    }

    // ── Transferencias ───────────────────────────────────────────────────────

    private async void Subir_Click(object sender, RoutedEventArgs e)
    {
        foreach (ElementoRemoto item in LocalLista.SelectedItems.OfType<ElementoRemoto>().Where(x => !x.EsCarpeta).ToList())
        {
            await SubirUno(item);
        }
    }

    private async Task SubirUno(ElementoRemoto archivo)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            Log("Conéctate primero para subir archivos.");
            return;
        }
        try
        {
            string destino = UnirPosix(_rutaRemota, archivo.Nombre);
            Log($"Subiendo {archivo.Nombre}…");
            await _servidor.SubirAsync(archivo.Ruta, destino);
            Log($"Subido: {archivo.Nombre}");
            await CargarRemoto(_rutaRemota);
        }
        catch (Exception ex)
        {
            Log($"Error al subir {archivo.Nombre}: {ex.Message}");
        }
    }

    private async void Descargar_Click(object sender, RoutedEventArgs e)
    {
        foreach (ElementoRemoto item in RemotoLista.SelectedItems.OfType<ElementoRemoto>().Where(x => !x.EsCarpeta).ToList())
        {
            await DescargarUno(item);
        }
    }

    private async Task DescargarUno(ElementoRemoto archivo)
    {
        if (_servidor is null || !_servidor.Conectado)
        {
            return;
        }
        try
        {
            string destino = Path.Combine(_rutaLocal, archivo.Nombre);
            Log($"Descargando {archivo.Nombre}…");
            await _servidor.DescargarAsync(archivo.Ruta, destino);
            Log($"Descargado: {archivo.Nombre}");
            CargarLocal(_rutaLocal);
        }
        catch (Exception ex)
        {
            Log($"Error al descargar {archivo.Nombre}: {ex.Message}");
        }
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
