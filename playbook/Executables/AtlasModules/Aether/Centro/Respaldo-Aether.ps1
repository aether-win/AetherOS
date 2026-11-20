<#
    Aether OS - Respaldo de carpetas y archivos elegidos por el usuario.
    Lo corre la tarea programada "Aether Respaldo" (como el usuario) o el botón
    "Respaldar ahora" del Centro Aether. Configuración: %APPDATA%\Aether\respaldo.json.
    Cada respaldo es una carpeta con fecha dentro de <destino>\Respaldos Aether; se
    guardan las últimas N versiones.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Aether.Centro.ps1')

$configuracion = Join-Path $script:AetherDatos 'respaldo.json'
$registro = Join-Path $script:AetherDatos 'respaldo.log'
New-Item -ItemType Directory -Path $script:AetherDatos -Force | Out-Null

function Write-AetherRespaldoLog {
    param([string]$Texto)
    Add-Content -LiteralPath $registro -Value ("[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Texto) -Encoding UTF8
}

try {
    if (-not (Test-Path -LiteralPath $configuracion)) { throw 'No hay respaldo configurado.' }
    $config = Get-Content -LiteralPath $configuracion -Raw -Encoding UTF8 | ConvertFrom-Json
    $origenes = @($config.Origenes | Where-Object { $_ })
    if ($origenes.Count -eq 0) { throw 'No elegiste carpetas ni archivos para respaldar.' }
    if (-not $config.Destino) { throw 'No elegiste dónde guardar el respaldo.' }
    if (-not (Test-Path -LiteralPath $config.Destino)) { throw "No se encuentra el destino '$($config.Destino)' (¿disco desconectado?)." }

    $raiz = Join-Path $config.Destino 'Respaldos Aether'
    $version = Join-Path $raiz (Get-Date -Format 'yyyy-MM-dd_HH-mm')
    New-Item -ItemType Directory -Path $version -Force | Out-Null
    Write-AetherRespaldoLog "Inicio del respaldo en '$version'."

    $fallos = New-Object System.Collections.Generic.List[string]
    foreach ($origen in $origenes) {
        $origen = [string]$origen
        if (-not (Test-Path -LiteralPath $origen)) { $fallos.Add("No existe: $origen"); continue }
        $nombre = Split-Path -Leaf $origen
        if (Test-Path -LiteralPath $origen -PathType Container) {
            & robocopy.exe $origen (Join-Path $version $nombre) /E /R:1 /W:1 /XJ /NFL /NDL /NJH /NJS /NP | Out-Null
            # robocopy: 0-7 es éxito (8 o más, errores).
            if ($LASTEXITCODE -ge 8) { $fallos.Add("Falló la copia de $origen (código $LASTEXITCODE)") }
        }
        else {
            try { Copy-Item -LiteralPath $origen -Destination (Join-Path $version $nombre) -Force }
            catch { $fallos.Add("Falló la copia de $($origen): $($_.Exception.Message)") }
        }
    }

    # Solo se guardan las últimas N versiones.
    $versiones = [int]$config.Versiones
    if ($versiones -lt 1) { $versiones = 5 }
    $viejas = @(Get-ChildItem -LiteralPath $raiz -Directory | Sort-Object Name -Descending | Select-Object -Skip $versiones)
    foreach ($vieja in $viejas) {
        Remove-Item -LiteralPath $vieja.FullName -Recurse -Force -ErrorAction SilentlyContinue
        Write-AetherRespaldoLog "Versión vieja borrada: $($vieja.Name)."
    }

    Set-AetherAjuste -Nombre 'RespaldoUltimo' -Valor (Get-Date -Format 'yyyy-MM-dd HH:mm')
    if ($fallos.Count -gt 0) {
        $texto = $fallos -join '; '
        Set-AetherAjuste -Nombre 'RespaldoError' -Valor $texto
        Write-AetherRespaldoLog "Terminado con problemas: $texto"
        exit 1
    }
    Set-AetherAjuste -Nombre 'RespaldoError' -Valor ''
    Write-AetherRespaldoLog 'Respaldo terminado sin errores.'
    exit 0
}
catch {
    Set-AetherAjuste -Nombre 'RespaldoError' -Valor $_.Exception.Message
    Write-AetherRespaldoLog "Error: $($_.Exception.Message)"
    exit 1
}
