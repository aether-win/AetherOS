using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AetherTransfer.Models;
using AetherTransfer.Security;

namespace AetherTransfer.Connections;

/// <summary>Un sitio guardado. La contraseña se almacena cifrada con DPAPI.</summary>
public sealed class SitioGuardado
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nombre { get; set; } = "";
    public string Host { get; set; } = "";
    public int Puerto { get; set; } = 21;
    public Protocolo Protocolo { get; set; } = Protocolo.Ftp;
    public string Usuario { get; set; } = "";
    public string PasswordProtegida { get; set; } = "";
    public string DirectorioInicial { get; set; } = "/";

    public ConexionInfo AConexion() => new()
    {
        Nombre = Nombre,
        Host = Host,
        Puerto = Puerto,
        Protocolo = Protocolo,
        Usuario = Usuario,
        Password = Credenciales.Desproteger(PasswordProtegida),
        DirectorioInicial = DirectorioInicial,
    };
}

/// <summary>
/// Guarda y lee los sitios en %APPDATA%\Aether Transfer\sitios.json, con la
/// contraseña cifrada. Nunca se escribe una contraseña en texto plano.
/// </summary>
public static class AlmacenSitios
{
    private static string Carpeta =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Aether Transfer");

    private static string Ruta => Path.Combine(Carpeta, "sitios.json");

    private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public static List<SitioGuardado> Cargar()
    {
        try
        {
            if (File.Exists(Ruta))
            {
                return JsonSerializer.Deserialize<List<SitioGuardado>>(File.ReadAllText(Ruta)) ?? new();
            }
        }
        catch { /* lista vacía */ }
        return new List<SitioGuardado>();
    }

    public static bool Guardar(List<SitioGuardado> sitios)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Ruta, JsonSerializer.Serialize(sitios, Opciones));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Crea un sitio desde datos sueltos, cifrando la contraseña.</summary>
    public static SitioGuardado Crear(string nombre, string host, int puerto, Protocolo proto, string usuario, string password, string dirInicial)
        => new()
        {
            Nombre = nombre,
            Host = host,
            Puerto = puerto,
            Protocolo = proto,
            Usuario = usuario,
            PasswordProtegida = Credenciales.Proteger(password),
            DirectorioInicial = string.IsNullOrWhiteSpace(dirInicial) ? "/" : dirInicial,
        };
}
