<#
    Aether: gestiona la foto de perfil predeterminada.
    - Si el usuario que inicia sesión tiene una cuenta de Microsoft, restaura las fotos
      predeterminadas ORIGINALES de Windows (para que se vea la foto normal / la de su cuenta).
    - Si es una cuenta local, deja la imagen de Aether.
    Corre en una tarea programada al iniciar sesión (contexto del usuario, con privilegios altos
    para poder escribir en ProgramData). Nunca falla de forma ruidosa.
#>
$ErrorActionPreference = 'Stop'
try {
    $destino = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'Microsoft\User Account Pictures'
    $originales = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'Aether\FotosPerfilOriginales'
    $aether = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'Aether\FotosPerfilAether'

    # ¿Cuenta de Microsoft? Si hay identidades almacenadas en el perfil del usuario, sí.
    $tieneMsa = $false
    foreach ($ruta in @(
            'HKCU:\SOFTWARE\Microsoft\IdentityCRL\UserExtendedProperties',
            'HKCU:\SOFTWARE\Microsoft\IdentityCRL\StoredIdentities')) {
        $k = Get-ChildItem -Path $ruta -ErrorAction SilentlyContinue
        if ($k) { $tieneMsa = $true; break }
    }

    $fuente = if ($tieneMsa) { $originales } else { $aether }
    if (Test-Path -LiteralPath $fuente) {
        Get-ChildItem -LiteralPath $fuente -File -ErrorAction SilentlyContinue | ForEach-Object {
            try { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $destino $_.Name) -Force -ErrorAction Stop } catch { Write-Verbose $_.Exception.Message }
        }
    }
}
catch {
    # Cosmético: nunca debe molestar al usuario.
    Write-Verbose $_.Exception.Message
}
