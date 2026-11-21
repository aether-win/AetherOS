using System;
using System.Security.Cryptography;
using System.Text;

namespace AetherTransfer.Security;

/// <summary>
/// Cifra y descifra secretos con DPAPI (ámbito del usuario actual). Las contraseñas
/// nunca se guardan en texto plano: solo este usuario, en esta máquina, puede leerlas.
/// </summary>
public static class Credenciales
{
    public static string Proteger(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return "";
        }
        try
        {
            byte[] cifrado = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(texto), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cifrado);
        }
        catch
        {
            return "";
        }
    }

    public static string Desproteger(string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return "";
        }
        try
        {
            byte[] plano = ProtectedData.Unprotect(
                Convert.FromBase64String(base64), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plano);
        }
        catch
        {
            return "";
        }
    }
}
