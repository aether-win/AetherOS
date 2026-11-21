namespace AetherTransfer.Models;

/// <summary>Protocolos soportados.</summary>
public enum Protocolo
{
    Ftp,
    FtpsExplicito,
    FtpsImplicito,
    Sftp,
}

/// <summary>Datos de una conexión remota. La contraseña nunca se persiste en texto plano.</summary>
public sealed class ConexionInfo
{
    public string Nombre { get; set; } = "";
    public string Host { get; set; } = "";
    public int Puerto { get; set; } = 21;
    public string Usuario { get; set; } = "";
    public string Password { get; set; } = "";
    public Protocolo Protocolo { get; set; } = Protocolo.Ftp;
    public string? ClaveSshPath { get; set; }
    public string DirectorioInicial { get; set; } = "/";

    /// <summary>Puerto por defecto según el protocolo.</summary>
    public static int PuertoPorDefecto(Protocolo p) => p switch
    {
        Protocolo.Sftp => 22,
        Protocolo.FtpsImplicito => 990,
        _ => 21,
    };
}
