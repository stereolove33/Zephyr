param([switch]$Pronto, [string]$RuntimeDirectory)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verification\originals-sha256.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.files) {
    $path = Join-Path $PSScriptRoot $entry.path
    if (-not (Test-Path -LiteralPath $path)) { throw "Arquivo original ausente: $($entry.path)" }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($actual -ne $entry.sha256) { throw "Arquivo original alterado: $($entry.path)" }
}
$gameDll = 'originals\R3nzSkin.dll'
if ($env:ZEPHYR_ORIGINAL_MENU -ne '1') {
    $gameDll = 'customized\R3nzSkin.dll'
    $customManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'customized\menu-sha256.json') -Raw | ConvertFrom-Json
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $gameDll) -Algorithm SHA256).Hash -ne $customManifest.sha256) { throw 'Customized menu DLL does not match its manifest.' }
}
if ($Pronto) {
    if (-not $RuntimeDirectory) { $RuntimeDirectory = Join-Path $PSScriptRoot 'runtime' }
    foreach ($pair in @(
        @('originals\j5nYI6re.exe', 'Zephyr.exe'),
        @($gameDll, 'R3nzSkin.dll')
    )) {
        $a = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $pair[0]) -Algorithm SHA256).Hash
        $b = (Get-FileHash -LiteralPath (Join-Path $RuntimeDirectory $pair[1]) -Algorithm SHA256).Hash
        if ($a -ne $b) { throw "Copia do R3nz alterada: $($pair[1])" }
    }
}
Write-Host 'Preservacao do R3nz verificada por SHA-256.'
