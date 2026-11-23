BeforeAll {
    . (Join-Path $PSScriptRoot 'AtlasTestHost.ps1')
    $script:CentroRoot = Join-Path $script:AtlasTestRepoRoot 'playbook\Executables\AtlasModules\Aether\Centro'
}

Describe 'Centro Aether' {
    It 'parses every Centro script without syntax errors' {
        foreach ($archivo in Get-ChildItem -LiteralPath $script:CentroRoot -Filter '*.ps1') {
            $tokens = $null
            $errores = $null
            [void][System.Management.Automation.Language.Parser]::ParseFile($archivo.FullName, [ref]$tokens, [ref]$errores)
            @($errores).Count | Should -Be 0 -Because "$($archivo.Name) must parse"
        }
    }

    It 'parses every standalone Aether script without syntax errors' {
        $carpeta = Join-Path $script:AtlasTestRepoRoot 'aether\scripts-sueltos\scripts'
        foreach ($archivo in Get-ChildItem -LiteralPath $carpeta -Filter '*.ps1') {
            $tokens = $null
            $errores = $null
            [void][System.Management.Automation.Language.Parser]::ParseFile($archivo.FullName, [ref]$tokens, [ref]$errores)
            @($errores).Count | Should -Be 0 -Because "$($archivo.Name) must parse"
        }
    }

    It 'loads the window layout and names every control the script uses' {
        $ruta = Join-Path $script:CentroRoot 'Centro-Aether.ps1'
        $tokens = $null
        $errores = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($ruta, [ref]$tokens, [ref]$errores)
        $ventanas = @($ast.FindAll({
                    param($nodo)
                    $nodo -is [System.Management.Automation.Language.StringConstantExpressionAst] -and
                    $nodo.Value.TrimStart().StartsWith('<Window')
                }, $true))
        $ventanas.Count | Should -Be 2
        $texto = @($ventanas | Where-Object { $_.Value -match 'Title="Aether Toolbox"' })[0].Value
        [xml]$bienvenida = @($ventanas | Where-Object { $_.Value -match 'Title="Bienvenido a Aether OS"' })[0].Value
        $nombresB = @($bienvenida.SelectNodes('//*[@*[local-name()="Name"]]') | ForEach-Object {
                $_.GetAttribute('Name', 'http://schemas.microsoft.com/winfx/2006/xaml')
            })
        $usadosB = [regex]::Matches((Get-Content -LiteralPath $ruta -Raw), '\$b\.([A-Za-z]+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        foreach ($nombre in $usadosB) {
            $nombresB | Should -Contain $nombre -Because "the welcome window uses `$b.$nombre"
        }
        $texto | Should -Not -BeNullOrEmpty
        [xml]$xaml = $texto
        $nombres = @($xaml.SelectNodes('//*[@*[local-name()="Name"]]') | ForEach-Object {
                $_.GetAttribute('Name', 'http://schemas.microsoft.com/winfx/2006/xaml')
            })
        $usados = [regex]::Matches((Get-Content -LiteralPath $ruta -Raw), '\$c\.([A-Za-z]+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        foreach ($nombre in $usados) {
            $nombres | Should -Contain $nombre -Because "the script uses `$c.$nombre"
        }

        if ($IsLinux -or $IsMacOS) { return }
        Add-Type -AssemblyName PresentationFramework
        $ventana = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $xaml))
        $ventana.FindName('BtnGaming') | Should -Not -BeNullOrEmpty
        $ventanaB = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $bienvenida))
        $ventanaB.FindName('BtnListo') | Should -Not -BeNullOrEmpty
    }

    It 'reads the current AC and DC index from the last two values of a powercfg query' {
        . (Join-Path $script:CentroRoot 'Aether.Centro.ps1')
        Mock powercfg.exe {
            'Índice de configuración: 0x00000000'
            'Valor mínimo posible: 0x00000000'
            'Valor máximo posible: 0x00000064'
            'Índice de configuración de corriente alterna actual: 0x00000002'
            'Índice de configuración de corriente continua actual: 0x00000001'
        }
        $valores = Get-AetherValorEnergia -Sub 'sub' -Ajuste 'ajuste'
        $valores | Should -Be @(2, 1)
    }

    It 'ships the Windhawk backups as valid Windhawk user data' {
        foreach ($nombre in 'Aether-Windhawk-Glass.json', 'Aether-Windhawk-Limpio.json', 'Aether-Windhawk-Todo-apagado.json') {
            $respaldo = Get-Content -LiteralPath (Join-Path $script:AtlasTestRepoRoot "playbook\Executables\AtlasModules\Aether\Windhawk\$nombre") -Raw -Encoding UTF8 | ConvertFrom-Json
            $respaldo.format | Should -Be 'windhawk-user-data-v1'
            @($respaldo.mods).Count | Should -BeGreaterThan 0
            @($respaldo.appSettings.engine.exclude) | Should -Contain 'WINWORD.EXE'
        }
    }
}
