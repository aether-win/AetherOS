using System.Collections.Generic;

namespace CentroAether.Services;

/// <summary>Una entrada de la base de compatibilidad.</summary>
public sealed class EntradaCompat
{
    public string Nombre { get; init; } = "";
    public bool Ok { get; init; } = true;   // true = funciona bien; false = requiere atención
    public string Nota { get; init; } = "";
}

/// <summary>
/// Base de datos curada de compatibilidad: cómo se comportan apps y funciones de
/// Windows tras los ajustes de Aether. Información honesta y general, para orientar.
/// </summary>
public static class CompatibilidadServicio
{
    public static IReadOnlyList<EntradaCompat> Todas { get; } = new List<EntradaCompat>
    {
        new() { Nombre = "Microsoft Office", Ok = true,
            Nota = "Funciona con normalidad. Las transparencias se desactivan en Word, Excel y PowerPoint para que el texto siempre se vea." },
        new() { Nombre = "Windows Update", Ok = true,
            Nota = "Sigue funcionando; Aether solo permite pausarlo de forma gestionada desde el módulo Actualizar." },
        new() { Nombre = "Microsoft Defender", Ok = true,
            Nota = "No se desactiva. La protección antivirus permanece activa." },
        new() { Nombre = "Microsoft Store", Ok = true,
            Nota = "Intacta. Las apps de la Store se siguen instalando y actualizando." },
        new() { Nombre = "Audio y Bluetooth", Ok = true,
            Nota = "Sin cambios. Aether no toca el volumen ni los dispositivos de sonido." },
        new() { Nombre = "Drivers AMD", Ok = true,
            Nota = "No se instalan ni modifican automáticamente (se retiró ese paso por no ser estándar)." },
        new() { Nombre = "Juegos con anti-cheat", Ok = false,
            Nota = "Normalmente funcionan, pero algunos anti-cheat son sensibles a mods. Si un juego competitivo da problemas, pausa Windhawk mientras juegas." },
        new() { Nombre = "Xbox Game Bar", Ok = false,
            Nota = "Si notas que falta, se puede reactivar desde Configuración de Windows; Aether no la elimina." },
    };
}
