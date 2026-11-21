using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AetherTransfer.Models;
using Renci.SshNet;

namespace AetherTransfer.Protocols;

/// <summary>Servidor SFTP sobre SSH, con SSH.NET. (SSH.NET es síncrono: se envuelve en tareas.)</summary>
public sealed class ServidorSftp : IServidorRemoto
{
    private SftpClient? _cliente;

    public bool Conectado => _cliente is { IsConnected: true };

    public Task ConectarAsync(ConexionInfo info, CancellationToken ct = default) => Task.Run(() =>
    {
        SftpClient cliente;
        if (!string.IsNullOrWhiteSpace(info.ClaveSshPath) && File.Exists(info.ClaveSshPath))
        {
            var clave = new PrivateKeyFile(info.ClaveSshPath);
            cliente = new SftpClient(info.Host, info.Puerto, info.Usuario, clave);
        }
        else
        {
            cliente = new SftpClient(info.Host, info.Puerto, info.Usuario, info.Password);
        }

        // La validación de clave de host (pinning) se refinará en la fase de seguridad.
        cliente.HostKeyReceived += (_, e) => e.CanTrust = true;
        cliente.Connect();
        _cliente = cliente;
    }, ct);

    public Task DesconectarAsync() => Task.Run(() =>
    {
        if (_cliente is not null)
        {
            try { _cliente.Disconnect(); } catch { /* ignore */ }
            _cliente.Dispose();
            _cliente = null;
        }
    });

    public Task<IReadOnlyList<ElementoRemoto>> ListarAsync(string ruta, CancellationToken ct = default) => Task.Run(() =>
    {
        if (_cliente is null)
        {
            return (IReadOnlyList<ElementoRemoto>)Array.Empty<ElementoRemoto>();
        }
        return _cliente.ListDirectory(ruta)
            .Where(f => f.Name is not "." and not "..")
            .Select(f => new ElementoRemoto
            {
                Nombre = f.Name,
                EsCarpeta = f.IsDirectory,
                Tamano = f.Length < 0 ? 0 : f.Length,
                Modificado = f.LastWriteTime,
            })
            .ToList();
    }, ct);

    public Task DescargarAsync(string rutaRemota, string rutaLocal, IProgress<double>? progreso = null, CancellationToken ct = default) => Task.Run(() =>
    {
        if (_cliente is null)
        {
            return;
        }
        long total = 0;
        try { total = _cliente.GetAttributes(rutaRemota).Size; } catch { /* sin tamaño */ }
        using FileStream fs = File.Create(rutaLocal);
        _cliente.DownloadFile(rutaRemota, fs, b =>
        {
            if (total > 0)
            {
                progreso?.Report((double)b / total * 100.0);
            }
        });
    }, ct);

    public Task SubirAsync(string rutaLocal, string rutaRemota, IProgress<double>? progreso = null, CancellationToken ct = default) => Task.Run(() =>
    {
        if (_cliente is null)
        {
            return;
        }
        long total = new FileInfo(rutaLocal).Length;
        using FileStream fs = File.OpenRead(rutaLocal);
        _cliente.UploadFile(fs, rutaRemota, b =>
        {
            if (total > 0)
            {
                progreso?.Report((double)b / total * 100.0);
            }
        });
    }, ct);

    public Task CrearCarpetaAsync(string ruta, CancellationToken ct = default) => Task.Run(() => _cliente?.CreateDirectory(ruta), ct);

    public Task EliminarAsync(string ruta, bool esCarpeta, CancellationToken ct = default) => Task.Run(() =>
    {
        if (_cliente is null)
        {
            return;
        }
        if (esCarpeta)
        {
            _cliente.DeleteDirectory(ruta);
        }
        else
        {
            _cliente.DeleteFile(ruta);
        }
    }, ct);

    public Task RenombrarAsync(string origen, string destino, CancellationToken ct = default)
        => Task.Run(() => _cliente?.RenameFile(origen, destino), ct);

    public async ValueTask DisposeAsync() => await DesconectarAsync();
}
