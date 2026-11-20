# Companion de aether-edge.psd1: escribe initial_preferences de Edge con Google como
# buscador. Edge solo lo lee al crear un perfil nuevo; las directivas de buscador
# (DefaultSearchProvider*) se ignoran en PCs no administradas, por eso no se usan.
#
# Aether: este ajuste es cosmético. Bajo TrustedInstaller ${env:ProgramFiles(x86)}
# puede venir vacío; antes eso tumbaba TODA la instalación en Join-Path. Ahora el
# cuerpo va dentro de un try/catch global: si algo falla, solo se registra un aviso
# y la instalación continúa (regla de Aether: nada bloquea el acceso, solo avisa).
$ErrorActionPreference = 'Stop'
try {

$preferencias = @'
{
  "default_search_provider_data": {
    "template_url_data": {
      "keyword": "google.com",
      "short_name": "Google",
      "url": "https://www.google.com/search?q={searchTerms}",
      "suggestions_url": "https://www.google.com/complete/search?client=chrome&q={searchTerms}",
      "favicon_url": "https://www.google.com/favicon.ico"
    }
  },
  "distribution": {
    "skip_first_run_ui": true,
    "suppress_first_run_default_browser_prompt": true
  }
}
'@

# Se filtran raíces nulas/vacías ANTES de Join-Path para que una variable de entorno
# ausente (habitual bajo TrustedInstaller) no provoque un error de parámetro nulo.
$raices = @(
    ${env:ProgramFiles(x86)}
    $env:ProgramFiles
    ${env:ProgramW6432}
    'C:\Program Files (x86)'
    'C:\Program Files'
) | Where-Object { $_ -and $_.Trim() } | Select-Object -Unique
$carpetas = foreach ($raiz in $raices) { Join-Path $raiz 'Microsoft\Edge\Application' }
$carpetas = @($carpetas | Select-Object -Unique)
$escrito = $false
foreach ($carpeta in $carpetas) {
    if (-not (Test-Path -LiteralPath $carpeta -PathType Container)) { continue }
    try {
        [IO.File]::WriteAllText((Join-Path $carpeta 'initial_preferences'), $preferencias, (New-Object Text.UTF8Encoding($false)))
        $escrito = $true
    }
    catch {
        Write-AtlasLog -Level Warning -Message "Edge Aether: no se pudo escribir initial_preferences en '$carpeta': $($_.Exception.Message)" -ErrorRecord $_
    }
}
if ($escrito) {
    Write-AtlasLog -Message 'Edge Aether: Google queda como buscador para los perfiles nuevos de Edge.'
}
else {
    Write-AtlasLog -Level Warning -Message 'Edge Aether: no se encontró la carpeta de Edge; initial_preferences no se escribió.'
}

}
catch {
    # Nada en este ajuste cosmético puede detener la instalación de Aether.
    Write-AtlasLog -Level Warning -Message "Edge Aether: ajuste omitido por un error no crítico: $($_.Exception.Message)" -ErrorRecord $_
}
