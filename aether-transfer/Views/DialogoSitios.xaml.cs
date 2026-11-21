using System.Collections.ObjectModel;
using System.Linq;
using AetherTransfer.Connections;
using AetherTransfer.Models;
using AetherTransfer.Security;
using Microsoft.UI.Xaml.Controls;

namespace AetherTransfer.Views;

public sealed partial class DialogoSitios : ContentDialog
{
    private readonly ObservableCollection<SitioGuardado> _sitios = new();

    /// <summary>El sitio que el usuario eligió para conectar (al pulsar Conectar).</summary>
    public SitioGuardado? SitioParaConectar { get; private set; }

    public DialogoSitios()
    {
        this.InitializeComponent();
        foreach (SitioGuardado s in AlmacenSitios.Cargar())
        {
            _sitios.Add(s);
        }
        SitiosLista.ItemsSource = _sitios;
        this.PrimaryButtonClick += (_, _) => SitioParaConectar = LeerFormularioComoSitio(SitiosLista.SelectedItem as SitioGuardado);
    }

    private void Sitio_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (SitiosLista.SelectedItem is SitioGuardado s)
        {
            FNombre.Text = s.Nombre;
            FHost.Text = s.Host;
            FPuerto.Text = s.Puerto.ToString();
            FProtocolo.SelectedIndex = (int)s.Protocolo;
            FUsuario.Text = s.Usuario;
            FPassword.Password = Credenciales.Desproteger(s.PasswordProtegida);
            FDirInicial.Text = s.DirectorioInicial;
        }
    }

    private void Nuevo_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SitiosLista.SelectedItem = null;
        FNombre.Text = "";
        FHost.Text = "";
        FPuerto.Text = "21";
        FProtocolo.SelectedIndex = 0;
        FUsuario.Text = "";
        FPassword.Password = "";
        FDirInicial.Text = "/";
        FAviso.Text = "Rellena los datos y pulsa Guardar.";
    }

    private void Protocolo_Changed(object sender, SelectionChangedEventArgs e)
    {
        // Sugerir el puerto por defecto del protocolo si el campo está vacío o es un puerto estándar.
        if (FPuerto is null)
        {
            return;
        }
        int actual = int.TryParse(FPuerto.Text, out int p) ? p : 0;
        if (actual is 0 or 21 or 22 or 990)
        {
            FPuerto.Text = ConexionInfo.PuertoPorDefecto((Protocolo)FProtocolo.SelectedIndex).ToString();
        }
    }

    private void Guardar_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SitioGuardado? sel = SitiosLista.SelectedItem as SitioGuardado;
        SitioGuardado nuevo = LeerFormularioComoSitio(sel);

        if (string.IsNullOrWhiteSpace(nuevo.Host))
        {
            FAviso.Text = "El servidor no puede estar vacío.";
            return;
        }

        if (sel is not null)
        {
            int i = _sitios.IndexOf(sel);
            if (i >= 0)
            {
                _sitios[i] = nuevo;
            }
        }
        else
        {
            _sitios.Add(nuevo);
            SitiosLista.SelectedItem = nuevo;
        }

        AlmacenSitios.Guardar(_sitios.ToList());
        FAviso.Text = "Sitio guardado.";
    }

    private void Eliminar_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (SitiosLista.SelectedItem is SitioGuardado s)
        {
            _sitios.Remove(s);
            AlmacenSitios.Guardar(_sitios.ToList());
            Nuevo_Click(sender, e);
            FAviso.Text = "Sitio eliminado.";
        }
    }

    private SitioGuardado LeerFormularioComoSitio(SitioGuardado? baseSitio)
    {
        int puerto = int.TryParse(FPuerto.Text.Trim(), out int p) ? p : 21;
        var proto = (Protocolo)(FProtocolo.SelectedIndex < 0 ? 0 : FProtocolo.SelectedIndex);
        string nombre = string.IsNullOrWhiteSpace(FNombre.Text) ? FHost.Text.Trim() : FNombre.Text.Trim();

        return new SitioGuardado
        {
            Id = baseSitio?.Id ?? System.Guid.NewGuid().ToString("N"),
            Nombre = nombre,
            Host = FHost.Text.Trim(),
            Puerto = puerto,
            Protocolo = proto,
            Usuario = FUsuario.Text.Trim(),
            PasswordProtegida = Credenciales.Proteger(FPassword.Password),
            DirectorioInicial = string.IsNullOrWhiteSpace(FDirInicial.Text) ? "/" : FDirInicial.Text.Trim(),
        };
    }
}
