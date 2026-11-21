namespace AetherTransfer.Models;

/// <summary>Progreso de una transferencia: porcentaje (0-100) y velocidad en bytes/s.</summary>
public readonly record struct ProgresoTransfer(double Porcentaje, double VelocidadBps);
