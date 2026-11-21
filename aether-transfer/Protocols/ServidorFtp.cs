using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AetherTransfer.Models;
using AetherTransfer.Settings;
using FluentFTP;

namespace AetherTransfer.Protocols;

/// <summary>Servidor FTP / FTPS explícito o implícito, sobre FluentFTP.</summary>
public sealed class ServidorFtp : IServidorRemoto
{
    private AsyncFtpClient? _cliente;

    public bool Conectado => _cliente is { IsConnected: true };

    public async Task ConectarAsync(ConexionInfo info, CancellationToken ct = default)
    {
        var cliente = new AsyncFtpClient(info.Host, info.Usuario, info.Password, info.Puerto);
        cliente.Config.EncryptionMode = info.Protocolo switch
        {
            Protocolo.FtpsExplicito => FtpEncryptionMode.Explicit,
            Protocolo.FtpsImplicito => FtpEncryptionMode.Implicit,
            _ => FtpEncryptionMode.None,
        };
        ConfigApp.Datos cfg = ConfigApp.Actual;
        cliente.Config.ConnectTimeout = Math.Max(5, cfg.TimeoutSegundos) * 1000;
        cliente.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
        if (cfg.LimiteVelocidadKBps > 0)
        {
            cliente.Config.DownloadRateLimit = (uint)cfg.LimiteVelocidadKBps;
            cliente.Config.UploadRateLimit = (uint)cfg.LimiteVelocidadKBps;
        }

        await cliente.Connect(ct);
        _cliente = cliente;
    }

    public async Task DesconectarAsync()
    {
        if (_cliente is not null)
        {
            try { await _cliente.Disconnect(); } catch { /* ignore */ }
            _cliente.Dispose();
            _cliente = null;
        }
    }

    public async Task<IReadOnlyList<ElementoRemoto>> ListarAsync(string ruta, CancellationToken ct = default)
    {
        if (_cliente is null)
        {
            return Array.Empty<ElementoRemoto>();
        }
        FtpListItem[] items = await _cliente.GetListing(ruta, ct);
        return items
            .Where(i => i.Name is not "." and not "..")
            .Select(i => new ElementoRemoto
            {
                Nombre = i.Name,
                EsCarpeta = i.Type == FtpObjectType.Directory,
                Tamano = i.Size < 0 ? 0 : i.Size,
                Modificado = i.Modified,
            })
            .ToList();
    }

    public async Task DescargarAsync(string rutaRemota, string rutaLocal, IProgress<ProgresoTransfer>? progreso = null, bool reanudar = false, CancellationToken ct = default)
    {
        if (_cliente is null)
        {
            return;
        }
        IProgress<FtpProgress>? fp = progreso is null ? null : new Progress<FtpProgress>(p => progreso.Report(new ProgresoTransfer(p.Progress, p.TransferSpeed)));
        FtpLocalExists modo = reanudar ? FtpLocalExists.Resume : FtpLocalExists.Overwrite;
        await _cliente.DownloadFile(rutaLocal, rutaRemota, modo, FtpVerify.None, fp, ct);
    }

    public async Task SubirAsync(string rutaLocal, string rutaRemota, IProgress<ProgresoTransfer>? progreso = null, bool reanudar = false, CancellationToken ct = default)
    {
        if (_cliente is null)
        {
            return;
        }
        IProgress<FtpProgress>? fp = progreso is null ? null : new Progress<FtpProgress>(p => progreso.Report(new ProgresoTransfer(p.Progress, p.TransferSpeed)));
        FtpRemoteExists modo = reanudar ? FtpRemoteExists.Resume : FtpRemoteExists.Overwrite;
        await _cliente.UploadFile(rutaLocal, rutaRemota, modo, false, FtpVerify.None, fp, ct);
    }

    public async Task CrearCarpetaAsync(string ruta, CancellationToken ct = default)
    {
        if (_cliente is not null)
        {
            await _cliente.CreateDirectory(ruta, ct);
        }
    }

    public async Task EliminarAsync(string ruta, bool esCarpeta, CancellationToken ct = default)
    {
        if (_cliente is null)
        {
            return;
        }
        if (esCarpeta)
        {
            await _cliente.DeleteDirectory(ruta, ct);
        }
        else
        {
            await _cliente.DeleteFile(ruta, ct);
        }
    }

    public async Task RenombrarAsync(string origen, string destino, CancellationToken ct = default)
    {
        if (_cliente is not null)
        {
            await _cliente.Rename(origen, destino, ct);
        }
    }

    public async ValueTask DisposeAsync() => await DesconectarAsync();
}
