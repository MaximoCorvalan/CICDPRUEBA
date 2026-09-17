# Delega al arranque del servidor (API + agente) en Desktop\agente contable.
$ErrorActionPreference = "Stop"
$candidatos = @(
    (Join-Path (Split-Path $PSScriptRoot -Parent) "arrancar-servidor.ps1"),
    (Join-Path $env:USERPROFILE "Desktop\agente contable\arrancar-servidor.ps1")
)
foreach ($s in $candidatos) {
    if (Test-Path -LiteralPath $s) {
        & $s
        exit $LASTEXITCODE
    }
}
throw "Falta arrancar-servidor.ps1 en Desktop\agente contable"
