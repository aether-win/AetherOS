<#
    Aether OS - Aether Toolbox (app de control; antes "Centro Aether"). Versión clásica de respaldo.
    Sin parámetros abre la ventana. Con -Bandeja queda en la bandeja del sistema: cambia de
    modo desde el menú o con atajos (Ctrl+Alt+T / G / B), mantiene la resolución alta del
    temporizador en Modo Gaming, activa el Modo Batería solo al 30% (configurable) y vuelve
    al modo anterior al conectar el cargador.
#>
[CmdletBinding()]
param(
    [switch]$Bandeja,
    [switch]$Bienvenida,
    [ValidateSet('Trabajo', 'Gaming', 'Bateria')][string]$Modo
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Aether.Centro.ps1')
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$script:Icono = Join-Path $PSScriptRoot 'aether.ico'
$script:EsteScript = $PSCommandPath
$script:NombresModo = @{ Trabajo = 'Trabajo'; Gaming = 'Gaming'; Bateria = 'Batería' }

# Cambio de modo directo (lo usan los accesos directos): -Modo Gaming
if ($Modo) {
    $resultado = Set-AetherModo -Modo $Modo
    if (-not $resultado.Ok) {
        [void][System.Windows.Forms.MessageBox]::Show($resultado.Mensaje, 'Aether OS', 'OK', 'Warning')
    }
    exit 0
}

# =========================================================================================
# Bandeja
# =========================================================================================
function Show-AetherCentro {
    # Abre la interfaz con el mismo lanzador que los accesos directos: el Centro nativo
    # (via su tarea programada, sin UAC) si esta, o la ventana clasica como respaldo.
    $vbs = Join-Path (Split-Path $script:EsteScript) 'Abrir-Centro.vbs'
    if (Test-Path -LiteralPath $vbs) {
        Start-Process -FilePath (Join-Path ([Environment]::SystemDirectory) 'wscript.exe') -ArgumentList @("`"$vbs`"")
    }
    else {
        Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', "`"$script:EsteScript`"")
    }
}

function Start-AetherBandeja {
    $creado = $false
    $mutex = New-Object System.Threading.Mutex($true, 'Local\AetherBandeja', [ref]$creado)
    if (-not $creado) { return }

    if (-not ('Aether.Atajos' -as [type])) {
        Add-Type -ReferencedAssemblies System.Windows.Forms -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Aether {
    public class Atajos : NativeWindow, IDisposable {
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mod, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        public event EventHandler<int> Pulsado;
        public Atajos() { CreateHandle(new CreateParams()); }
        public bool Registrar(int id, uint mod, uint vk) { return RegisterHotKey(Handle, id, mod, vk); }
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x0312 && Pulsado != null) { Pulsado(this, m.WParam.ToInt32()); }
            base.WndProc(ref m);
        }
        public void Dispose() { for (int i = 1; i <= 3; i++) { UnregisterHotKey(Handle, i); } DestroyHandle(); }
    }
}
'@
    }

    # Si está Aether Toolbox (AetherToolbox.exe), ÉL es el ícono de la bandeja, el menú y los
    # atajos (siempre en segundo plano). Esta bandeja queda solo como vigilante invisible
    # (batería automática, temporizador, unidades de red, colores de Wind-Aether, respaldo).
    $script:HayNativo = Test-Path -LiteralPath (Join-Path $PSScriptRoot 'app\AetherToolbox.exe') -PathType Leaf

    $script:IconoBandeja = New-Object System.Windows.Forms.NotifyIcon
    $script:IconoBandeja.Icon = New-Object System.Drawing.Icon($script:Icono)
    $script:IconoBandeja.Visible = -not $script:HayNativo
    $menu = New-Object System.Windows.Forms.ContextMenuStrip
    $script:ItemsModo = @{}
    foreach ($nombre in @('Trabajo', 'Gaming', 'Bateria')) {
        $item = $menu.Items.Add("Modo $($script:NombresModo[$nombre])")
        $item.Tag = $nombre
        $item.add_Click({ param($origen) Set-AetherModoDesdeBandeja -Modo ([string]$origen.Tag) })
        $script:ItemsModo[$nombre] = $item
    }
    [void]$menu.Items.Add('-')
    $abrir = $menu.Items.Add('Abrir Aether Toolbox')
    $abrir.add_Click({ Show-AetherCentro })
    $salir = $menu.Items.Add('Salir de la bandeja')
    $salir.add_Click({ [System.Windows.Forms.Application]::Exit() })
    $script:IconoBandeja.ContextMenuStrip = $menu
    $script:IconoBandeja.add_DoubleClick({ Show-AetherCentro })

    # Atajos: Ctrl (2) + Alt (1); T = 0x54, G = 0x47, B = 0x42. Con el Centro nativo los
    # registra él (y muestra la animación del modo); aquí no, para no quitárselos.
    $script:Atajos = New-Object Aether.Atajos
    if (-not $script:HayNativo) {
        [void]$script:Atajos.Registrar(1, 3, 0x54)
        [void]$script:Atajos.Registrar(2, 3, 0x47)
        [void]$script:Atajos.Registrar(3, 3, 0x42)
    }
    $script:Atajos.add_Pulsado({
            param($origen, $id)
            [void]$origen
            $destino = @{ 1 = 'Trabajo'; 2 = 'Gaming'; 3 = 'Bateria' }[[int]$id]
            Set-AetherModoDesdeBandeja -Modo $destino
        })

    $script:TemporizadorAlto = $false
    $script:AvisoCancelado = $false
    $script:UltimoErrorRespaldo = [string](Get-AetherAjuste -Nombre 'RespaldoError' -PorDefecto '')
    $script:Vueltas = 0
    $script:UnidadesAvisadas = ''
    $reloj = New-Object System.Windows.Forms.Timer
    $reloj.Interval = 5000
    $reloj.add_Tick({ Invoke-AetherVigilante })
    $reloj.Start()
    Update-AetherBandeja
    if ($script:HayNativo -and -not (Get-Process -Name 'AetherToolbox' -ErrorAction SilentlyContinue)) {
        Start-Process -FilePath (Join-Path ([Environment]::SystemDirectory) 'schtasks.exe') -ArgumentList @('/run', '/tn', '"\Aether\Toolbox segundo plano"') -WindowStyle Hidden
    }
    # Con Aether Toolbox (nativo) la bienvenida la muestra él, oscura y completa.
    if (-not $script:HayNativo -and (Get-AetherAjuste -Nombre 'BienvenidaVista' -PorDefecto 0) -ne 1) {
        Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', "`"$script:EsteScript`"", '-Bienvenida')
    }

    [System.Windows.Forms.Application]::Run()

    $reloj.Stop()
    $script:Atajos.Dispose()
    $script:IconoBandeja.Visible = $false
    $script:IconoBandeja.Dispose()
    $mutex.ReleaseMutex()
}

function Show-AetherGlobo {
    param([string]$Texto, [string]$Tipo = 'Info')
    if ($script:HayNativo -and -not $script:IconoBandeja.Visible) {
        # El ícono visible es el del Centro nativo: este se muestra solo un momento para el aviso.
        $script:IconoBandeja.Visible = $true
        $ocultar = New-Object System.Windows.Forms.Timer
        $ocultar.Interval = 7000
        $ocultar.add_Tick({
                param($origen)
                $origen.Stop()
                $origen.Dispose()
                $script:IconoBandeja.Visible = $false
            })
        $ocultar.Start()
    }
    $script:IconoBandeja.ShowBalloonTip(5000, 'Aether OS', $Texto, $Tipo)
}

function Update-AetherBandeja {
    $actual = Get-AetherModo
    foreach ($nombre in $script:ItemsModo.Keys) { $script:ItemsModo[$nombre].Checked = ($nombre -eq $actual) }
    $script:IconoBandeja.Text = "Aether OS - Modo $($script:NombresModo[$actual])"
}

function Set-AetherModoDesdeBandeja {
    param([string]$Modo)
    $resultado = Set-AetherModo -Modo $Modo
    if ($resultado.Ok) {
        Show-AetherGlobo -Texto $resultado.Mensaje
    }
    else {
        Show-AetherGlobo -Texto $resultado.Mensaje -Tipo 'Warning'
    }
    Update-AetherBandeja
}

function Invoke-AetherVigilante {
    try {
        $modo = Get-AetherModo
        # Resolución alta del temporizador (0,5 ms) solo mientras dure el Modo Gaming.
        if ($modo -eq 'Gaming' -and -not $script:TemporizadorAlto) {
            $actual = [uint32]0
            [void][Aether.Nativo]::NtSetTimerResolution(5000, $true, [ref]$actual)
            $script:TemporizadorAlto = $true
        }
        elseif ($modo -ne 'Gaming' -and $script:TemporizadorAlto) {
            $actual = [uint32]0
            [void][Aether.Nativo]::NtSetTimerResolution(5000, $false, [ref]$actual)
            $script:TemporizadorAlto = $false
        }

        $script:Vueltas++
        if ($script:Vueltas % 6 -ne 0) { return }   # batería y respaldo cada 30 s
        Update-AetherBandeja

        $cargador = Test-AetherCargador
        $bateria = Get-AetherBateria
        if ($cargador) {
            $script:AvisoCancelado = $false
            if ($modo -eq 'Bateria' -and (Get-AetherAjuste -Nombre 'ModoAutomatico' -PorDefecto 0) -eq 1) {
                $anterior = [string](Get-AetherAjuste -Nombre 'ModoAnterior' -PorDefecto 'Trabajo')
                if ($anterior -eq 'Bateria') { $anterior = 'Trabajo' }
                Set-AetherModoDesdeBandeja -Modo $anterior
            }
        }
        elseif ($null -ne $bateria) {
            if ($modo -eq 'Gaming') {
                Set-AetherModoDesdeBandeja -Modo 'Trabajo'
                Show-AetherGlobo -Texto 'Se desconectó el cargador: el Modo Gaming pasó a Trabajo.' -Tipo 'Warning'
            }
            $automatico = (Get-AetherAjuste -Nombre 'BateriaAuto' -PorDefecto 1) -eq 1
            $umbral = [int](Get-AetherAjuste -Nombre 'UmbralBateria' -PorDefecto 30)
            if ($automatico -and $modo -ne 'Bateria' -and -not $script:AvisoCancelado -and $bateria -le $umbral) {
                if (Confirm-AetherCambioBateria -Porcentaje $bateria) {
                    $resultado = Set-AetherModo -Modo 'Bateria' -Automatico
                    Show-AetherGlobo -Texto $resultado.Mensaje
                    Update-AetherBandeja
                }
                else {
                    $script:AvisoCancelado = $true
                }
            }
        }

        $perfilWind = [string](Get-AetherAjuste -Nombre 'PerfilWind' -PorDefecto 'ninguno')
        if ($perfilWind -ne 'ninguno' -and (Get-AetherTema) -ne [string](Get-AetherAjuste -Nombre 'TemaColores' -PorDefecto '')) {
            [void](Set-AetherColoresWind)
        }

        if ($script:Vueltas % 12 -eq 0) {
            $faltan = @(Connect-AetherUnidadesRed)
            $textoFaltan = $faltan -join ', '
            if ($faltan.Count -gt 0 -and $textoFaltan -ne $script:UnidadesAvisadas) {
                Show-AetherGlobo -Texto "No se pudo conectar: $textoFaltan. Aether seguirá intentando." -Tipo 'Warning'
            }
            $script:UnidadesAvisadas = $textoFaltan
        }

        $fallaRespaldo = [string](Get-AetherAjuste -Nombre 'RespaldoError' -PorDefecto '')
        if ($fallaRespaldo -and $fallaRespaldo -ne $script:UltimoErrorRespaldo) {
            Show-AetherGlobo -Texto "El respaldo falló: $fallaRespaldo" -Tipo 'Error'
        }
        $script:UltimoErrorRespaldo = $fallaRespaldo
    }
    catch {
        Write-Verbose "Vigilante: $($_.Exception.Message)"
    }
}

function Confirm-AetherCambioBateria {
    # Aviso de 10 segundos con Cancelar. Devuelve $true si no se canceló.
    param([int]$Porcentaje)
    # Ventana oscura sin borde (tema Aether: nada blanco).
    $fondo = [System.Drawing.Color]::FromArgb(15, 32, 54)
    $forma = New-Object System.Windows.Forms.Form
    $forma.Text = 'Aether OS'
    $forma.Size = New-Object System.Drawing.Size(440, 170)
    $forma.StartPosition = 'CenterScreen'
    $forma.TopMost = $true
    $forma.FormBorderStyle = 'None'
    $forma.ShowInTaskbar = $false
    $forma.BackColor = $fondo
    $forma.ForeColor = [System.Drawing.Color]::FromArgb(230, 238, 248)
    $forma.Icon = New-Object System.Drawing.Icon($script:Icono)
    $titulo = New-Object System.Windows.Forms.Label
    $titulo.AutoSize = $true
    $titulo.Text = 'Aether OS · Modo Batería'
    $titulo.Font = New-Object System.Drawing.Font('Segoe UI Semibold', 12)
    $titulo.ForeColor = [System.Drawing.Color]::FromArgb(79, 195, 247)
    $titulo.Location = New-Object System.Drawing.Point(20, 16)
    $forma.Controls.Add($titulo)
    $etiqueta = New-Object System.Windows.Forms.Label
    $etiqueta.AutoSize = $false
    $etiqueta.Size = New-Object System.Drawing.Size(400, 50)
    $etiqueta.Location = New-Object System.Drawing.Point(20, 52)
    $etiqueta.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $forma.Controls.Add($etiqueta)
    $cancelar = New-Object System.Windows.Forms.Button
    $cancelar.Text = 'Cancelar'
    $cancelar.Size = New-Object System.Drawing.Size(110, 34)
    $cancelar.Location = New-Object System.Drawing.Point(310, 116)
    $cancelar.FlatStyle = 'Flat'
    $cancelar.FlatAppearance.BorderColor = [System.Drawing.Color]::FromArgb(36, 69, 107)
    $cancelar.BackColor = [System.Drawing.Color]::FromArgb(22, 48, 79)
    $cancelar.ForeColor = [System.Drawing.Color]::FromArgb(230, 238, 248)
    $cancelar.DialogResult = 'Cancel'
    $forma.Controls.Add($cancelar)
    $forma.CancelButton = $cancelar
    $script:Restan = 10
    $etiqueta.Text = "La batería está en $Porcentaje%. En $($script:Restan) segundos se activa el Modo Batería."
    $reloj = New-Object System.Windows.Forms.Timer
    $reloj.Interval = 1000
    $reloj.add_Tick({
            $script:Restan--
            if ($script:Restan -le 0) {
                $forma.DialogResult = 'OK'
                $forma.Close()
            }
            else {
                $etiqueta.Text = "La batería está en $Porcentaje%. En $($script:Restan) segundos se activa el Modo Batería."
            }
        })
    $reloj.Start()
    $respuesta = $forma.ShowDialog()
    $reloj.Stop()
    $forma.Dispose()
    return ($respuesta -ne 'Cancel')
}

if ($Bandeja) {
    Start-AetherBandeja
    exit 0
}

# =========================================================================================
# Asistente de bienvenida (primer inicio de sesión)
# =========================================================================================
if ($Bienvenida) {
    Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
    [xml]$xamlB = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Bienvenido a Aether OS" Width="640" Height="680" WindowStartupLocation="CenterScreen"
        Background="#0A0F1E" FontFamily="Segoe UI" FontSize="14" ResizeMode="NoResize">
  <Window.Resources>
    <Style TargetType="TextBlock"><Setter Property="Foreground" Value="#F3F5F8"/><Setter Property="TextWrapping" Value="Wrap"/></Style>
    <Style TargetType="RadioButton"><Setter Property="Foreground" Value="#F3F5F8"/><Setter Property="Margin" Value="0,4,18,4"/></Style>
    <Style TargetType="CheckBox"><Setter Property="Foreground" Value="#F3F5F8"/><Setter Property="Margin" Value="0,4,0,4"/></Style>
    <Style TargetType="Button">
      <Setter Property="Background" Value="#2F8CFF"/><Setter Property="Foreground" Value="White"/>
      <Setter Property="BorderThickness" Value="0"/><Setter Property="Padding" Value="18,9"/><Setter Property="Margin" Value="10,0,0,0"/>
    </Style>
    <Style x:Key="Seccion" TargetType="TextBlock"><Setter Property="FontSize" Value="17"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="Foreground" Value="#4FC3F7"/><Setter Property="Margin" Value="0,16,0,4"/></Style>
  </Window.Resources>
  <DockPanel Margin="28">
    <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,16,0,0">
      <Button x:Name="BtnSaltar" Content="Ahora no" Background="#1B2A44"/>
      <Button x:Name="BtnListo" Content="Listo"/>
    </StackPanel>
    <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
      <TextBlock Text="Bienvenido a Aether OS" FontSize="26" FontWeight="SemiBold"/>
      <TextBlock Foreground="#C7CBD1" Margin="0,6,0,0" Text="Elige cómo quieres empezar. Todo se puede cambiar después en Aether Toolbox."/>
      <TextBlock Style="{StaticResource Seccion}" Text="Modo"/>
      <WrapPanel>
        <RadioButton x:Name="RbTrabajo" GroupName="Modo" Content="Trabajo" IsChecked="True"/>
        <RadioButton x:Name="RbGaming" GroupName="Modo" Content="Gaming (con cargador)"/>
        <RadioButton x:Name="RbBateria" GroupName="Modo" Content="Batería"/>
      </WrapPanel>
      <CheckBox x:Name="ChkAuto" Content="Activar el Modo Batería solo al 30% sin cargador" IsChecked="True"/>
      <TextBlock Style="{StaticResource Seccion}" Text="Tema"/>
      <WrapPanel>
        <RadioButton x:Name="RbOscuro" GroupName="Tema" Content="Oscuro"/>
        <RadioButton x:Name="RbClaro" GroupName="Tema" Content="Claro"/>
      </WrapPanel>
      <TextBlock Style="{StaticResource Seccion}" Text="Wind-Aether (aspecto de Windows)"/>
      <WrapPanel>
        <RadioButton x:Name="RbGlass" GroupName="Wind" Content="Aether Glass"/>
        <RadioButton x:Name="RbLimpio" GroupName="Wind" Content="Aether Limpio"/>
        <RadioButton x:Name="RbNinguno" GroupName="Wind" Content="Ninguno" IsChecked="True"/>
      </WrapPanel>
      <TextBlock x:Name="TxtWindNota" Foreground="#C7CBD1"/>
      <TextBlock Style="{StaticResource Seccion}" Text="Carpeta de red (opcional)"/>
      <TextBlock Foreground="#C7CBD1" Text="Por ejemplo \\192.168.1.10\SISTEMA EF RED. Queda fija como unidad Z: y lista para las macros de Excel."/>
      <TextBox x:Name="TxtRuta" Padding="6" Margin="0,6,0,0" Background="#0A1726" Foreground="#F3F5F8" BorderBrush="#2A4466" CaretBrush="#F3F5F8"/>
      <TextBlock Style="{StaticResource Seccion}" Text="Respaldo"/>
      <CheckBox x:Name="ChkRespaldo" Content="Abrir Aether Toolbox en Respaldo al terminar"/>
      <TextBlock x:Name="TxtResultado" Foreground="#4FC3F7" Margin="0,12,0,0"/>
    </StackPanel></ScrollViewer>
  </DockPanel>
</Window>
'@
    $ventanaB = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $xamlB))
    $ventanaB.Icon = [System.Windows.Media.Imaging.BitmapFrame]::Create((New-Object Uri($script:Icono)))
    $b = @{}
    foreach ($nombre in 'BtnSaltar', 'BtnListo', 'RbTrabajo', 'RbGaming', 'RbBateria', 'ChkAuto', 'RbOscuro', 'RbClaro', 'RbGlass', 'RbLimpio', 'RbNinguno', 'TxtWindNota', 'TxtRuta', 'ChkRespaldo', 'TxtResultado') {
        $b[$nombre] = $ventanaB.FindName($nombre)
    }
    if ((Get-AetherTema) -eq 'claro') { $b.RbClaro.IsChecked = $true } else { $b.RbOscuro.IsChecked = $true }
    if (-not (Get-AetherWindhawkCli)) {
        $b.TxtWindNota.Text = 'Windhawk no está instalado todavía; podrás elegirlo después en Aether Toolbox.'
        $b.RbGlass.IsEnabled = $false
        $b.RbLimpio.IsEnabled = $false
    }
    $b.BtnSaltar.add_Click({ Set-AetherAjuste -Nombre 'BienvenidaVista' -Valor 1; $ventanaB.Close() })
    $b.BtnListo.add_Click({
            $ventanaB.Cursor = [System.Windows.Input.Cursors]::Wait
            $avisos = New-Object System.Collections.Generic.List[string]
            try {
                Set-AetherAjuste -Nombre 'BateriaAuto' -Valor ([int][bool]$b.ChkAuto.IsChecked)
                $modo = if ($b.RbGaming.IsChecked) { 'Gaming' } elseif ($b.RbBateria.IsChecked) { 'Bateria' } else { 'Trabajo' }
                $resultado = Set-AetherModo -Modo $modo
                if (-not $resultado.Ok) { $avisos.Add($resultado.Mensaje) }
                Set-AetherTema -Tema $(if ($b.RbClaro.IsChecked) { 'claro' } else { 'oscuro' })
                if ($b.RbGlass.IsChecked) { $avisos.Add((Set-AetherPerfilWind -Perfil 'aether-glass')) }
                elseif ($b.RbLimpio.IsChecked) { $avisos.Add((Set-AetherPerfilWind -Perfil 'aether-limpio')) }
                $ruta = $b.TxtRuta.Text.Trim()
                if ($ruta) {
                    if (-not (Add-AetherUnidadRed -Letra 'Z' -Ruta $ruta)) { $avisos.Add('La carpeta de red quedó guardada; se conectará cuando la otra PC esté encendida.') }
                }
                Set-AetherAjuste -Nombre 'BienvenidaVista' -Valor 1
                if ($b.ChkRespaldo.IsChecked) {
                    Show-AetherCentro
                }
                if ($avisos.Count -gt 0) {
                    [void][System.Windows.MessageBox]::Show(($avisos -join "`n"), 'Aether OS', 'OK', 'Information')
                }
                $ventanaB.Close()
            }
            catch {
                $b.TxtResultado.Text = "Error: $($_.Exception.Message)"
            }
            finally { $ventanaB.Cursor = $null }
        })
    [void]$ventanaB.ShowDialog()
    exit 0
}

# Si Aether Toolbox (nuevo) esta instalado, se abre ese y la ventana clasica nunca aparece.
if (-not $Bandeja -and -not $Bienvenida -and (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'app\AetherToolbox.exe') -PathType Leaf)) {
    $vbsNuevo = Join-Path $PSScriptRoot 'Abrir-Centro.vbs'
    if (Test-Path -LiteralPath $vbsNuevo) {
        Start-Process -FilePath (Join-Path ([Environment]::SystemDirectory) 'wscript.exe') -ArgumentList @("`"$vbsNuevo`"")
        exit 0
    }
}

# =========================================================================================
# Ventana del Centro Aether (WPF)
# =========================================================================================
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

[xml]$xaml = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Aether Toolbox" Width="940" Height="640" MinWidth="820" MinHeight="560"
        WindowStartupLocation="CenterScreen" Background="#0A0F1E" FontFamily="Segoe UI" FontSize="14">
  <Window.Resources>
    <Style TargetType="TextBlock"><Setter Property="Foreground" Value="#F3F5F8"/><Setter Property="TextWrapping" Value="Wrap"/></Style>
    <Style TargetType="CheckBox"><Setter Property="Foreground" Value="#F3F5F8"/><Setter Property="Margin" Value="0,6,0,6"/></Style>
    <Style TargetType="Button">
      <Setter Property="Background" Value="#2F8CFF"/><Setter Property="Foreground" Value="White"/>
      <Setter Property="BorderThickness" Value="0"/><Setter Property="Padding" Value="14,8"/>
      <Setter Property="Margin" Value="0,6,10,6"/><Setter Property="Cursor" Value="Hand"/>
      <Setter Property="HorizontalAlignment" Value="Left"/>
    </Style>
    <Style x:Key="Titulo" TargetType="TextBlock"><Setter Property="FontSize" Value="24"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="Foreground" Value="White"/><Setter Property="Margin" Value="0,0,0,12"/></Style>
    <Style x:Key="Nota" TargetType="TextBlock"><Setter Property="Foreground" Value="#C7CBD1"/><Setter Property="TextWrapping" Value="Wrap"/><Setter Property="Margin" Value="0,4,0,8"/></Style>
    <Style TargetType="TabItem">
      <Setter Property="Foreground" Value="#F3F5F8"/><Setter Property="FontSize" Value="15"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="TabItem">
            <Border x:Name="Fondo" Padding="18,11" Margin="0,2" CornerRadius="6" Background="Transparent">
              <ContentPresenter ContentSource="Header"/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property="IsSelected" Value="True"><Setter TargetName="Fondo" Property="Background" Value="#1B2A44"/></Trigger>
              <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Fondo" Property="Background" Value="#14203A"/></Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>
  </Window.Resources>
  <DockPanel>
    <Border DockPanel.Dock="Bottom" Background="#0F1628" Padding="14,8">
      <TextBlock x:Name="TxtEstado" Foreground="#4FC3F7" Text="Listo."/>
    </Border>
    <TabControl TabStripPlacement="Left" Background="#11182B" BorderThickness="0" Padding="24">
      <TabItem Header="Modos">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Modos"/>
          <TextBlock x:Name="TxtModo" FontSize="18" Foreground="#4FC3F7" Margin="0,0,0,10"/>
          <WrapPanel>
            <Button x:Name="BtnTrabajo" Content="Modo Trabajo"/>
            <Button x:Name="BtnGaming" Content="Modo Gaming"/>
            <Button x:Name="BtnBateria" Content="Modo Batería"/>
          </WrapPanel>
          <TextBlock Style="{StaticResource Nota}" Text="Trabajo: todo activo, notificaciones y apps en segundo plano. Gaming (solo con cargador): máximo rendimiento sin estacionar núcleos, temporizador rápido, no molestar y mods pesados en pausa. Batería: ahorro de batería de Windows, procesador eficiente y Wind-Aether en Aether Limpio."/>
          <CheckBox x:Name="ChkLimitarTurbo" Content="En Gaming, limitar el turbo del procesador para dar más energía a la gráfica integrada"/>
          <StackPanel Orientation="Horizontal" Margin="0,6,0,6">
            <CheckBox x:Name="ChkBateriaAuto" Content="Activar el Modo Batería solo al llegar a " VerticalAlignment="Center" Margin="0"/>
            <ComboBox x:Name="CmbUmbral" Width="70" Margin="6,0"/>
            <TextBlock Text="sin cargador" VerticalAlignment="Center"/>
          </StackPanel>
          <TextBlock Style="{StaticResource Nota}" Text="Atajos: Ctrl+Alt+T (Trabajo), Ctrl+Alt+G (Gaming), Ctrl+Alt+B (Batería). También desde el ícono de Aether junto al reloj."/>
          <TextBlock Text="Tema" FontSize="18" FontWeight="SemiBold" Margin="0,16,0,4"/>
          <WrapPanel>
            <Button x:Name="BtnTemaOscuro" Content="Tema oscuro"/>
            <Button x:Name="BtnTemaClaro" Content="Tema claro"/>
            <Button x:Name="BtnBienvenida" Content="Abrir el asistente de bienvenida"/>
          </WrapPanel>
          <TextBlock Style="{StaticResource Nota}" Text="Los colores de Wind-Aether se ajustan solos al tema."/>
        </StackPanel></ScrollViewer>
      </TabItem>
      <TabItem Header="Red">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Red y carpetas compartidas"/>
          <Button x:Name="BtnRepararRed" Content="Diagnosticar y reparar la red"/>
          <StackPanel Orientation="Horizontal">
            <TextBox x:Name="TxtEquipo" Width="200" Margin="0,6,10,6" Padding="6"/>
            <Button x:Name="BtnProbarEquipo" Content="Probar con esta PC (nombre o IP)"/>
          </StackPanel>
          <Button x:Name="BtnScriptOtraPC" Content="Guardar el reparador en el Escritorio (para la otra PC)"/>
          <Button x:Name="BtnRedPrivada" Content="Hacer privada la red conectada (para compartir carpetas)"/>
          <Button x:Name="BtnCompartir" Content="Abrir opciones de uso compartido"/>
          <TextBlock x:Name="TxtRed" Style="{StaticResource Nota}" FontFamily="Consolas"/>
          <!-- Aether: "Unidades de red fijas" oculto por ahora (la IP de la otra PC cambia). -->
          <StackPanel Visibility="Collapsed">
          <TextBlock Text="Unidades de red fijas" FontSize="18" FontWeight="SemiBold" Margin="0,16,0,4"/>
          <TextBlock Style="{StaticResource Nota}" Text="Usa la IP de la otra PC (por ejemplo \\192.168.1.10\SISTEMA EF RED). Aether la reconecta sola si se cae, la pone en Intranet local y en las ubicaciones de confianza de Excel para que las macros funcionen."/>
          <ListBox x:Name="LstUnidades" Height="90" Background="#0F1628" Foreground="#F3F5F8"/>
          <StackPanel Orientation="Horizontal">
            <ComboBox x:Name="CmbLetra" Width="60" Margin="0,6,10,6"/>
            <TextBox x:Name="TxtRutaRed" Width="330" Padding="6" Margin="0,6,10,6"/>
            <Button x:Name="BtnAgregarUnidad" Content="Agregar"/>
          </StackPanel>
          <WrapPanel>
            <Button x:Name="BtnQuitarUnidad" Content="Quitar la elegida"/>
            <Button x:Name="BtnConectarUnidades" Content="Reconectar ahora"/>
          </WrapPanel>
          </StackPanel>
        </StackPanel></ScrollViewer>
      </TabItem>
      <TabItem Header="Actualizar">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Actualizaciones"/>
          <TextBlock x:Name="TxtPausa" Style="{StaticResource Nota}"/>
          <Button x:Name="BtnActualizarWindows" Content="Actualizar Windows ahora (quita la pausa)"/>
          <Button x:Name="BtnPausar" Content="Volver a pausar hasta el año 3000"/>
          <Button x:Name="BtnActualizarApps" Content="Actualizar todas mis apps"/>
          <Button x:Name="BtnEdgeGoogle" Content="Poner Google como buscador en Edge"/>
          <TextBlock Style="{StaticResource Nota}" Text="Edge no deja cambiar su buscador desde afuera: el botón abre la página de buscadores de Edge; elige Google y pulsa 'Establecer como predeterminado'."/>
        </StackPanel></ScrollViewer>
      </TabItem>
      <TabItem Header="Limpieza">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Espacio"/>
          <TextBlock Style="{StaticResource Nota}" Text="La limpieza segura nunca toca Descargas, Escritorio ni Documentos."/>
          <Button x:Name="BtnLimpiar" Content="Limpieza segura (temporales y papelera)"/>
          <Button x:Name="BtnLimpiezaProfunda" Content="Limpieza de Windows (temporales del sistema y descargas de Windows Update)"/>
          <Button x:Name="BtnQueOcupa" Content="¿Qué ocupa espacio?"/>
          <Button x:Name="BtnAlmacenamiento" Content="Abrir Almacenamiento de Windows"/>
          <TextBlock x:Name="TxtEspacio" Style="{StaticResource Nota}" FontFamily="Consolas"/>
        </StackPanel></ScrollViewer>
      </TabItem>
      <TabItem Header="Batería">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Batería"/>
          <TextBlock x:Name="TxtBateria" FontSize="16"/>
          <Button x:Name="BtnReporteBateria" Content="Ver el reporte de batería de Windows"/>
          <TextBlock Style="{StaticResource Nota}" Text="Consejo para tu HP: para que la batería dure más años, limita la carga al 80%. Reinicia, pulsa F10 para entrar al BIOS, busca 'Battery Health Manager' (Administrador de salud de la batería) y elige 'Maximize my battery health'."/>
        </StackPanel></ScrollViewer>
      </TabItem>
      <TabItem Header="Respaldo">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Respaldo"/>
          <TextBlock Style="{StaticResource Nota}" Text="Qué respaldar (carpetas o archivos sueltos):"/>
          <ListBox x:Name="LstOrigenes" Height="120" Background="#0F1628" Foreground="#F3F5F8"/>
          <WrapPanel>
            <Button x:Name="BtnAgregarCarpeta" Content="Agregar carpeta"/>
            <Button x:Name="BtnAgregarArchivo" Content="Agregar archivos"/>
            <Button x:Name="BtnQuitarOrigen" Content="Quitar el elegido"/>
          </WrapPanel>
          <StackPanel Orientation="Horizontal">
            <TextBlock Text="Guardar en:" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox x:Name="TxtDestino" Width="380" Padding="6" Margin="0,6,10,6"/>
            <Button x:Name="BtnDestino" Content="Elegir…"/>
          </StackPanel>
          <StackPanel Orientation="Horizontal">
            <TextBlock Text="Versiones a guardar:" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <ComboBox x:Name="CmbVersiones" Width="60" Margin="0,6,20,6"/>
            <TextBlock Text="Hora diaria (HH:mm):" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox x:Name="TxtHora" Width="70" Padding="6" Margin="0,6,0,6"/>
          </StackPanel>
          <CheckBox x:Name="ChkRespaldoActivo" Content="Respaldar todos los días a esa hora"/>
          <WrapPanel>
            <Button x:Name="BtnGuardarRespaldo" Content="Guardar"/>
            <Button x:Name="BtnRespaldarAhora" Content="Respaldar ahora"/>
            <Button x:Name="BtnAbrirRespaldos" Content="Abrir respaldos (restaurar)"/>
          </WrapPanel>
          <TextBlock x:Name="TxtRespaldo" Style="{StaticResource Nota}"/>
        </StackPanel></ScrollViewer>
      </TabItem>
      <TabItem Header="Wind-Aether">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Wind-Aether"/>
          <TextBlock x:Name="TxtWind" Style="{StaticResource Nota}"/>
          <Button x:Name="BtnGlass" Content="Aplicar Aether Glass (dock, vidrio y animaciones)"/>
          <Button x:Name="BtnLimpio" Content="Aplicar Aether Limpio (ligero)"/>
          <Button x:Name="BtnWindApagar" Content="Desactivar todos los mods de Wind-Aether"/>
          <Button x:Name="BtnAbrirWindhawk" Content="Abrir Windhawk"/>
        </StackPanel></ScrollViewer>
      </TabItem>
      <TabItem Header="Salud">
        <ScrollViewer VerticalScrollBarVisibility="Auto"><StackPanel>
          <TextBlock Style="{StaticResource Titulo}" Text="Revisión de salud"/>
          <Button x:Name="BtnRevisarRapido" Content="Revisión rápida"/>
          <Button x:Name="BtnRevisarSalud" Content="Revisión completa de Aether (pide permiso)"/>
          <TextBlock x:Name="TxtSalud" Style="{StaticResource Nota}" FontFamily="Consolas"/>
          <TextBlock Style="{StaticResource Nota}" Margin="0,30,0,0" Text="Aether OS, basado en AtlasOS. Software libre bajo la licencia GPLv3."/>
        </StackPanel></ScrollViewer>
      </TabItem>
    </TabControl>
  </DockPanel>
</Window>
'@

$ventana = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $xaml))
$ventana.Icon = [System.Windows.Media.Imaging.BitmapFrame]::Create((New-Object Uri($script:Icono)))
$c = @{}
$xaml.SelectNodes('//*[@*[local-name()="Name"]]') | ForEach-Object {
    $nombre = $_.GetAttribute('Name', 'http://schemas.microsoft.com/winfx/2006/xaml')
    if ($nombre) { $c[$nombre] = $ventana.FindName($nombre) }
}

function Set-AetherEstado {
    param([string]$Texto)
    $c.TxtEstado.Text = $Texto
}

function Invoke-AetherAccion {
    # Ejecuta un bloque mostrando el cursor de espera y cualquier error en la barra de estado.
    param([scriptblock]$Bloque)
    $ventana.Cursor = [System.Windows.Input.Cursors]::Wait
    try { & $Bloque }
    catch { Set-AetherEstado -Texto "Error: $($_.Exception.Message)" }
    finally { $ventana.Cursor = $null }
}

# --- Modos --------------------------------------------------------------------------------
function Update-AetherVistaModo {
    $actual = Get-AetherModo
    $c.TxtModo.Text = "Modo actual: $($script:NombresModo[$actual])" + $(if (Test-AetherCargador) { ' · con cargador' } else { ' · con batería' })
}
foreach ($par in @(@('BtnTrabajo', 'Trabajo'), @('BtnGaming', 'Gaming'), @('BtnBateria', 'Bateria'))) {
    $c[$par[0]].Tag = $par[1]
    $c[$par[0]].add_Click({
            param($origen)
            Invoke-AetherAccion {
                $destino = [string]$origen.Tag
                $resultado = Set-AetherModo -Modo $destino
                Set-AetherEstado -Texto $resultado.Mensaje
                if ($resultado.Ok -and $destino -eq 'Gaming') {
                    [void][System.Windows.MessageBox]::Show('¿El ventilador está conectado?', 'Modo Gaming', 'OK', 'Information')
                }
                Update-AetherVistaModo
            }
        })
}
foreach ($valor in 10, 15, 20, 25, 30, 35, 40, 45, 50) { [void]$c.CmbUmbral.Items.Add($valor) }
$c.CmbUmbral.SelectedItem = [int](Get-AetherAjuste -Nombre 'UmbralBateria' -PorDefecto 30)
$c.ChkBateriaAuto.IsChecked = (Get-AetherAjuste -Nombre 'BateriaAuto' -PorDefecto 1) -eq 1
$c.ChkLimitarTurbo.IsChecked = (Get-AetherAjuste -Nombre 'LimitarTurbo' -PorDefecto 0) -eq 1
$c.BtnTemaOscuro.add_Click({ Invoke-AetherAccion { Set-AetherTema -Tema 'oscuro'; Set-AetherEstado -Texto 'Tema oscuro aplicado.' } })
$c.BtnTemaClaro.add_Click({ Invoke-AetherAccion { Set-AetherTema -Tema 'claro'; Set-AetherEstado -Texto 'Tema claro aplicado.' } })
$c.BtnBienvenida.add_Click({ Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', "`"$script:EsteScript`"", '-Bienvenida') })
$c.CmbUmbral.add_SelectionChanged({ if ($c.CmbUmbral.SelectedItem) { Set-AetherAjuste -Nombre 'UmbralBateria' -Valor ([int]$c.CmbUmbral.SelectedItem) } })
$c.ChkBateriaAuto.add_Click({ Set-AetherAjuste -Nombre 'BateriaAuto' -Valor ([int][bool]$c.ChkBateriaAuto.IsChecked) })
$c.ChkLimitarTurbo.add_Click({ Set-AetherAjuste -Nombre 'LimitarTurbo' -Valor ([int][bool]$c.ChkLimitarTurbo.IsChecked) })

# --- Red ------------------------------------------------------------------------------------
$script:Reparador = Join-Path $script:AetherCarpeta 'Red\Reparar-Red-Aether.ps1'
function Update-AetherVistaRed {
    $lineas = foreach ($adaptador in @(Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object Status -eq 'Up')) {
        $ip = @(Get-NetIPAddress -InterfaceIndex $adaptador.ifIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue | ForEach-Object IPAddress) -join ', '
        $perfil = @(Get-NetConnectionProfile -InterfaceIndex $adaptador.ifIndex -ErrorAction SilentlyContinue | ForEach-Object { [string]$_.NetworkCategory }) -join ', '
        '{0}: {1}  (red {2})' -f $adaptador.Name, $ip, $perfil
    }
    $c.TxtRed.Text = "Este equipo: $env:COMPUTERNAME`n" + $(if ($lineas) { ($lineas -join "`n") } else { 'Sin adaptadores conectados.' })
}
$c.BtnRepararRed.add_Click({
        if (Start-AetherComoAdmin -Archivo 'powershell.exe' -Argumentos @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$script:Reparador`"")) {
            Set-AetherEstado -Texto 'Se abrió el reparador de red.'
        }
        else { Set-AetherEstado -Texto 'Se canceló el permiso de administrador.' }
    })
$c.BtnProbarEquipo.add_Click({
        $equipo = $c.TxtEquipo.Text.Trim()
        if (-not $equipo) { Set-AetherEstado -Texto 'Escribe el nombre o la IP de la otra PC.'; return }
        [void](Start-AetherComoAdmin -Archivo 'powershell.exe' -Argumentos @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$script:Reparador`"", '-Equipo', "`"$equipo`""))
    })
$c.BtnScriptOtraPC.add_Click({
        Invoke-AetherAccion {
            $destino = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Reparar-Red-Aether'
            New-Item -ItemType Directory -Path $destino -Force | Out-Null
            Copy-Item -Path (Join-Path $script:AetherCarpeta 'Red\*') -Destination $destino -Force
            Set-AetherEstado -Texto "Listo: copia la carpeta '$destino' a la otra PC y abre Reparar-Red-Aether.cmd."
        }
    })
$c.BtnRedPrivada.add_Click({
        $script = Join-Path $script:AetherCarpeta 'Red\Redes-Privadas.ps1'
        if (Start-AetherComoAdmin -Archivo 'powershell.exe' -Argumentos @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', "`"$script`"") -Esperar) {
            Update-AetherVistaRed
            Set-AetherEstado -Texto 'Listo: las redes conectadas quedaron como privadas. (Aether lo hace solo con cada red nueva.)'
        }
        else { Set-AetherEstado -Texto 'Se canceló el permiso de administrador.' }
    })
$c.BtnCompartir.add_Click({ Start-Process -FilePath 'control.exe' -ArgumentList '/name Microsoft.NetworkAndSharingCenter /page Advanced' })

function Update-AetherVistaUnidades {
    $c.LstUnidades.Items.Clear()
    foreach ($unidad in @(Get-AetherUnidades)) {
        $estado = if (Test-Path -LiteralPath "$($unidad.Letra):\") { 'conectada' } else { 'sin conexión' }
        [void]$c.LstUnidades.Items.Add("$($unidad.Letra):  $($unidad.Ruta)  ($estado)")
    }
}
foreach ($letra in [char[]](90..68)) { [void]$c.CmbLetra.Items.Add([string]$letra) }
$c.CmbLetra.SelectedIndex = 0
$c.BtnAgregarUnidad.add_Click({
        Invoke-AetherAccion {
            $ok = Add-AetherUnidadRed -Letra ([string]$c.CmbLetra.SelectedItem) -Ruta $c.TxtRutaRed.Text
            Set-AetherEstado -Texto $(if ($ok) { 'Unidad conectada y lista para Excel.' } else { 'Unidad guardada, pero no se pudo conectar ahora (¿la otra PC está encendida?). Aether reintentará solo.' })
            Update-AetherVistaUnidades
        }
    })
$c.BtnQuitarUnidad.add_Click({
        Invoke-AetherAccion {
            if ($null -eq $c.LstUnidades.SelectedItem) { Set-AetherEstado -Texto 'Elige una unidad de la lista.'; return }
            Remove-AetherUnidadRed -Letra ([string]$c.LstUnidades.SelectedItem).Substring(0, 1)
            Update-AetherVistaUnidades
            Set-AetherEstado -Texto 'Unidad quitada.'
        }
    })
$c.BtnConectarUnidades.add_Click({
        Invoke-AetherAccion {
            $faltan = @(Connect-AetherUnidadesRed)
            Set-AetherEstado -Texto $(if ($faltan.Count -eq 0) { 'Todas las unidades están conectadas.' } else { "Sin conexión: $($faltan -join ', ')" })
            Update-AetherVistaUnidades
        }
    })

# --- Actualizaciones ---------------------------------------------------------------------
$script:LanzadorQuitarPausa = '3. Configuracion general\Pausar actualizaciones\Quitar pausa de Windows Update.cmd'
$script:LanzadorPausar = '3. Configuracion general\Pausar actualizaciones\Pausar Windows Update (predeterminado).cmd'
function Update-AetherVistaPausa {
    $fin = $null
    try { $fin = Get-ItemPropertyValue -LiteralPath 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings' -Name 'PauseUpdatesExpiryTime' -ErrorAction Stop } catch { $fin = $null }
    $c.TxtPausa.Text = if ($fin -and ([string]$fin).StartsWith('3000')) { 'Windows Update está pausado hasta el año 3000.' } else { 'Windows Update no está pausado.' }
}
$c.BtnActualizarWindows.add_Click({
        Invoke-AetherAccion {
            if (Invoke-AetherLanzador -Relativa $script:LanzadorQuitarPausa -Silencioso) {
                Start-Process 'ms-settings:windowsupdate-action'
                Set-AetherEstado -Texto 'Pausa quitada. Instala las actualizaciones y después pulsa "Volver a pausar".'
            }
            else { Set-AetherEstado -Texto 'No se pudo quitar la pausa (¿se canceló el permiso?).' }
            Update-AetherVistaPausa
        }
    })
$c.BtnPausar.add_Click({
        Invoke-AetherAccion {
            if (Invoke-AetherLanzador -Relativa $script:LanzadorPausar -Silencioso) { Set-AetherEstado -Texto 'Windows Update quedó pausado hasta el año 3000.' }
            else { Set-AetherEstado -Texto 'No se pudo pausar (¿se canceló el permiso?).' }
            Update-AetherVistaPausa
        }
    })
$c.BtnActualizarApps.add_Click({
        Start-Process -FilePath 'cmd.exe' -ArgumentList '/k title Aether OS - Actualizar apps & winget upgrade --all --include-unknown --accept-package-agreements --accept-source-agreements'
    })
$c.BtnEdgeGoogle.add_Click({
        try { Start-Process -FilePath 'msedge.exe' -ArgumentList 'edge://settings/searchEngines' }
        catch { Set-AetherEstado -Texto 'No se encontró Microsoft Edge.' }
    })

# --- Limpieza -----------------------------------------------------------------------------
$c.BtnLimpiar.add_Click({
        Invoke-AetherAccion {
            $mb = Clear-AetherTemporales
            Set-AetherEstado -Texto "Limpieza segura terminada: se liberaron unos $mb MB."
        }
    })
$c.BtnLimpiezaProfunda.add_Click({
        $orden = 'Remove-Item -Path $env:windir\Temp\* -Recurse -Force -ErrorAction SilentlyContinue; ' +
        'Stop-Service wuauserv -Force -ErrorAction SilentlyContinue; ' +
        'Remove-Item -Path $env:windir\SoftwareDistribution\Download\* -Recurse -Force -ErrorAction SilentlyContinue; ' +
        'Start-Service wuauserv -ErrorAction SilentlyContinue; ' +
        'Delete-DeliveryOptimizationCache -Force -ErrorAction SilentlyContinue'
        if (Start-AetherComoAdmin -Archivo 'powershell.exe' -Argumentos @('-NoProfile', '-WindowStyle', 'Hidden', '-Command', $orden) -Esperar) {
            Set-AetherEstado -Texto 'Limpieza de Windows terminada.'
        }
        else { Set-AetherEstado -Texto 'Se canceló el permiso de administrador.' }
    })
$script:TrabajoEspacio = $null
$script:RelojEspacio = New-Object System.Windows.Threading.DispatcherTimer
$script:RelojEspacio.Interval = [TimeSpan]::FromSeconds(1)
$script:RelojEspacio.add_Tick({
        if ($script:TrabajoEspacio -and $script:TrabajoEspacio.State -ne 'Running') {
            $script:RelojEspacio.Stop()
            $c.TxtEspacio.Text = (Receive-Job -Job $script:TrabajoEspacio -ErrorAction SilentlyContinue) -join "`n"
            Remove-Job -Job $script:TrabajoEspacio -Force
            $script:TrabajoEspacio = $null
            Set-AetherEstado -Texto 'Listo.'
        }
    })
$c.BtnQueOcupa.add_Click({
        if ($script:TrabajoEspacio) { return }
        Set-AetherEstado -Texto 'Midiendo carpetas... puede tardar un poco.'
        $c.TxtEspacio.Text = ''
        $script:TrabajoEspacio = Start-Job -ScriptBlock {
            $disco = Get-PSDrive -Name ($env:SystemDrive.TrimEnd(':'))
            'Disco {0}: {1:N1} GB libres de {2:N1} GB' -f $env:SystemDrive, ($disco.Free / 1GB), (($disco.Free + $disco.Used) / 1GB)
            ''
            $carpetas = @('Desktop', 'Downloads', 'Documents', 'Pictures', 'Videos', 'Music', 'AppData') | ForEach-Object { Join-Path $env:USERPROFILE $_ }
            $carpetas += @("$env:windir\SoftwareDistribution", "$env:windir\Temp", "${env:ProgramFiles(x86)}\Steam\steamapps")
            $resultados = foreach ($carpeta in $carpetas) {
                if (-not (Test-Path -LiteralPath $carpeta)) { continue }
                $total = 0
                Get-ChildItem -LiteralPath $carpeta -Recurse -Force -File -ErrorAction SilentlyContinue | ForEach-Object { $total += $_.Length }
                [pscustomobject]@{ Carpeta = $carpeta; GB = [Math]::Round($total / 1GB, 2) }
            }
            $resultados | Sort-Object GB -Descending | ForEach-Object { '{0,8:N2} GB  {1}' -f $_.GB, $_.Carpeta }
        }
        $script:RelojEspacio.Start()
    })
$c.BtnAlmacenamiento.add_Click({ Start-Process 'ms-settings:storagesense' })

# --- Batería ------------------------------------------------------------------------------
function Update-AetherVistaBateria {
    $porcentaje = Get-AetherBateria
    if ($null -eq $porcentaje) { $c.TxtBateria.Text = 'Esta PC no tiene batería.'; return }
    $salud = Get-AetherSaludBateria
    $texto = "Carga: $porcentaje%" + $(if (Test-AetherCargador) { ' (cargando o con cargador)' } else { ' (con batería)' })
    if ($salud) {
        $texto += "`nSalud: $($salud.Salud)% (capacidad actual $($salud.Actual) mWh de $($salud.Diseno) mWh de fábrica)"
    }
    $c.TxtBateria.Text = $texto
}
$c.BtnReporteBateria.add_Click({
        Invoke-AetherAccion {
            $ruta = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Reporte de bateria.html'
            & powercfg.exe /batteryreport /output $ruta 2>&1 | Out-Null
            if (Test-Path -LiteralPath $ruta) { Start-Process -FilePath $ruta; Set-AetherEstado -Texto "Reporte guardado en $ruta" }
            else { Set-AetherEstado -Texto 'Windows no pudo crear el reporte de batería.' }
        }
    })

# --- Respaldo -----------------------------------------------------------------------------
$script:ConfigRespaldo = Join-Path $script:AetherDatos 'respaldo.json'
$script:ScriptRespaldo = Join-Path $PSScriptRoot 'Respaldo-Aether.ps1'
foreach ($valor in 1..10) { [void]$c.CmbVersiones.Items.Add($valor) }
function Read-AetherConfigRespaldo {
    $config = [pscustomobject]@{ Origenes = @(); Destino = ''; Versiones = 5; Hora = '20:00'; Activo = $false }
    if (Test-Path -LiteralPath $script:ConfigRespaldo) {
        try { $config = Get-Content -LiteralPath $script:ConfigRespaldo -Raw -Encoding UTF8 | ConvertFrom-Json }
        catch { Set-AetherEstado -Texto 'La configuración del respaldo estaba dañada; se empieza de nuevo.' }
    }
    $c.LstOrigenes.Items.Clear()
    foreach ($origen in @($config.Origenes)) { if ($origen) { [void]$c.LstOrigenes.Items.Add([string]$origen) } }
    $c.TxtDestino.Text = [string]$config.Destino
    $c.CmbVersiones.SelectedItem = [int]$config.Versiones
    $c.TxtHora.Text = [string]$config.Hora
    $c.ChkRespaldoActivo.IsChecked = [bool]$config.Activo
    $ultimo = [string](Get-AetherAjuste -Nombre 'RespaldoUltimo' -PorDefecto '')
    $falla = [string](Get-AetherAjuste -Nombre 'RespaldoError' -PorDefecto '')
    $c.TxtRespaldo.Text = $(if ($ultimo) { "Último respaldo: $ultimo." } else { 'Todavía no hay respaldos.' }) + $(if ($falla) { " Problema: $falla" } else { '' })
}
function Save-AetherConfigRespaldo {
    $hora = $c.TxtHora.Text.Trim()
    if ($hora -notmatch '^([01]?\d|2[0-3]):[0-5]\d$') { throw 'La hora debe tener el formato HH:mm, por ejemplo 20:00.' }
    $config = [pscustomobject]@{
        Origenes  = @($c.LstOrigenes.Items | ForEach-Object { [string]$_ })
        Destino   = $c.TxtDestino.Text.Trim()
        Versiones = [int]$(if ($c.CmbVersiones.SelectedItem) { $c.CmbVersiones.SelectedItem } else { 5 })
        Hora      = $hora
        Activo    = [bool]$c.ChkRespaldoActivo.IsChecked
    }
    New-Item -ItemType Directory -Path $script:AetherDatos -Force | Out-Null
    $config | ConvertTo-Json | Set-Content -LiteralPath $script:ConfigRespaldo -Encoding UTF8
    # Tarea diaria como este usuario (no necesita permisos de administrador).
    & schtasks.exe /Delete /TN 'Aether Respaldo' /F 2>&1 | Out-Null
    if ($config.Activo) {
        $accion = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$script:ScriptRespaldo`""
        & schtasks.exe /Create /TN 'Aether Respaldo' /SC DAILY /ST $hora /TR $accion /F 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo programar el respaldo diario.' }
    }
}
$c.BtnAgregarCarpeta.add_Click({
        $dialogo = New-Object System.Windows.Forms.FolderBrowserDialog
        $dialogo.Description = 'Elige una carpeta para respaldar'
        if ($dialogo.ShowDialog() -eq 'OK') { [void]$c.LstOrigenes.Items.Add($dialogo.SelectedPath) }
    })
$c.BtnAgregarArchivo.add_Click({
        $dialogo = New-Object Microsoft.Win32.OpenFileDialog
        $dialogo.Multiselect = $true
        $dialogo.Title = 'Elige archivos para respaldar'
        if ($dialogo.ShowDialog()) { foreach ($archivo in $dialogo.FileNames) { [void]$c.LstOrigenes.Items.Add($archivo) } }
    })
$c.BtnQuitarOrigen.add_Click({ if ($null -ne $c.LstOrigenes.SelectedItem) { $c.LstOrigenes.Items.Remove($c.LstOrigenes.SelectedItem) } })
$c.BtnDestino.add_Click({
        $dialogo = New-Object System.Windows.Forms.FolderBrowserDialog
        $dialogo.Description = 'Elige dónde guardar los respaldos (por ejemplo, un disco externo)'
        if ($dialogo.ShowDialog() -eq 'OK') { $c.TxtDestino.Text = $dialogo.SelectedPath }
    })
$c.BtnGuardarRespaldo.add_Click({ Invoke-AetherAccion { Save-AetherConfigRespaldo; Set-AetherEstado -Texto 'Respaldo guardado.' } })
$c.BtnRespaldarAhora.add_Click({
        Invoke-AetherAccion {
            Save-AetherConfigRespaldo
            Set-AetherEstado -Texto 'Respaldando...'
            $proceso = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', "`"$script:ScriptRespaldo`"") -Wait -PassThru
            Read-AetherConfigRespaldo
            Set-AetherEstado -Texto $(if ($proceso.ExitCode -eq 0) { 'Respaldo terminado.' } else { 'El respaldo terminó con problemas; mira el detalle abajo.' })
        }
    })
$c.BtnAbrirRespaldos.add_Click({
        $raiz = Join-Path $c.TxtDestino.Text.Trim() 'Respaldos Aether'
        if (Test-Path -LiteralPath $raiz) { Start-Process -FilePath 'explorer.exe' -ArgumentList "`"$raiz`"" }
        else { Set-AetherEstado -Texto 'Todavía no hay respaldos en ese destino.' }
    })

# --- Wind-Aether --------------------------------------------------------------------------
function Update-AetherVistaWind {
    $perfil = [string](Get-AetherAjuste -Nombre 'PerfilWind' -PorDefecto 'ninguno')
    $nombres = @{ 'aether-glass' = 'Aether Glass'; 'aether-limpio' = 'Aether Limpio'; 'ninguno' = 'ninguno (todos desactivados)' }
    $c.TxtWind.Text = if (Get-AetherWindhawkCli) { "Perfil actual: $($nombres[$perfil])." } else { 'Windhawk no está instalado (o es una versión sin herramienta de línea de comandos). Elige la casilla Wind-Aether al instalar, o instala Windhawk 2.0.' }
}
foreach ($par in @(@('BtnGlass', 'aether-glass'), @('BtnLimpio', 'aether-limpio'), @('BtnWindApagar', 'ninguno'))) {
    $c[$par[0]].Tag = $par[1]
    $c[$par[0]].add_Click({
            param($origen)
            Invoke-AetherAccion {
                Set-AetherEstado -Texto (Set-AetherPerfilWind -Perfil ([string]$origen.Tag))
                Update-AetherVistaWind
            }
        })
}
$c.BtnAbrirWindhawk.add_Click({
        $exe = @("$env:ProgramFiles\Windhawk\windhawk.exe", "${env:ProgramFiles(x86)}\Windhawk\windhawk.exe") | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if ($exe) { Start-Process -FilePath $exe } else { Set-AetherEstado -Texto 'Windhawk no está instalado.' }
    })

# --- Salud --------------------------------------------------------------------------------
$c.BtnRevisarRapido.add_Click({
        Invoke-AetherAccion {
            $lineas = New-Object System.Collections.Generic.List[string]
            $wks = Get-Service -Name LanmanWorkstation -ErrorAction SilentlyContinue
            $lineas.Add('Carpetas compartidas (Estación de trabajo): ' + $(if ($wks -and $wks.Status -eq 'Running') { 'OK' } else { 'detenido -> usa Diagnosticar y reparar la red' }))
            $firma = $null
            try { $firma = Get-ItemPropertyValue -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters' -Name RequireSecuritySignature -ErrorAction Stop } catch { $firma = $null }
            $lineas.Add('Firma SMB obligatoria: ' + $(if ($firma -eq 0) { 'desactivada (OK para tu red)' } else { 'activada -> usa Diagnosticar y reparar la red' }))
            $lineas.Add('Modo: ' + $script:NombresModo[(Get-AetherModo)])
            $disco = Get-PSDrive -Name ($env:SystemDrive.TrimEnd(':'))
            $libre = [Math]::Round($disco.Free / 1GB, 1)
            $lineas.Add("Espacio libre: $libre GB" + $(if ($libre -lt 20) { ' -> poco espacio, usa Limpieza' } else { '' }))
            $defender = Get-Service -Name WinDefend -ErrorAction SilentlyContinue
            $lineas.Add('Microsoft Defender: ' + $(if ($defender) { [string]$defender.Status } else { 'quitado' }))
            $falla = [string](Get-AetherAjuste -Nombre 'RespaldoError' -PorDefecto '')
            $lineas.Add('Respaldo: ' + $(if ($falla) { "falló ($falla)" } else { 'sin errores' }))
            $c.TxtSalud.Text = $lineas -join "`n"
            Set-AetherEstado -Texto 'Revisión rápida terminada.'
        }
    })
$c.BtnRevisarSalud.add_Click({
        if (-not (Invoke-AetherLanzador -Relativa '9. Solucion de problemas\Revisar la salud de Aether.cmd')) {
            Set-AetherEstado -Texto 'No se pudo abrir la revisión completa.'
        }
    })

# --- Inicio -------------------------------------------------------------------------------
Update-AetherVistaModo
Update-AetherVistaRed
Update-AetherVistaUnidades
Update-AetherVistaPausa
Update-AetherVistaBateria
Read-AetherConfigRespaldo
Update-AetherVistaWind
[void]$ventana.ShowDialog()
