function Set-AtlasThemeMru {
    Stop-ThemeProcesses
    Set-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes" -Name "ThemeMRU" -Value "$((@(
        "aether-oscuro.theme",
        "aether-claro.theme",
        "dark.theme",
        "aero.theme"
    ) | ForEach-Object { "$env:SystemRoot\resources\Themes\$_" }) -join ';');" -Type String -Force
}
