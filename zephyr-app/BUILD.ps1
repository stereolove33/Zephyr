param(
    [string]$CacheCargo,
    [switch]$SomenteMenu
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Run-Checked([string]$Program, [string[]]$Arguments) {
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Program falhou (codigo $LASTEXITCODE)."
    }
}

try {
    & (Join-Path $PSScriptRoot 'VERIFY-ORIGINALS.ps1')

    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) {
        throw 'Falta o compilador do .NET Framework x64. Instale o .NET Framework 4.8.'
    }

    $ready = Join-Path $PSScriptRoot 'runtime'
    New-Item -ItemType Directory -Force -Path $ready | Out-Null
    Write-Host 'Compilando somente a extensao do menu. O R3nz nao sera recompilado.'
    Run-Checked $compiler @(
        '/nologo', '/target:library', '/platform:x64', '/optimize+',
        '/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll',
        '/r:System.Windows.Forms.dll', '/r:System.Web.Extensions.dll', '/r:System.Net.Http.dll',
        ('/r:' + (Join-Path $PSScriptRoot 'addon\webview2\Microsoft.Web.WebView2.Core.dll')),
        ('/r:' + (Join-Path $PSScriptRoot 'addon\webview2\Microsoft.Web.WebView2.WinForms.dll')),
        ('/out:' + (Join-Path $ready 'SkinFusion.Menu.dll')),
        (Join-Path $PSScriptRoot 'addon\SkinFusionMenu.cs'),
        (Join-Path $PSScriptRoot 'addon\ZephyrMenu.cs'),
        (Join-Path $PSScriptRoot 'addon\UpdateChecker.cs')
    )
    foreach ($dll in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'addon\webview2') -Filter '*.dll') {
        $destination = Join-Path $ready $dll.Name
        Copy-Item -LiteralPath $dll.FullName -Destination $destination -Force
        Unblock-File -LiteralPath $destination
    }
    $uiReady = Join-Path $ready 'ui'
    New-Item -ItemType Directory -Force -Path $uiReady | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'ui') -File | Copy-Item -Destination $uiReady -Force

    if (-not $SomenteMenu) {
        foreach ($tool in @('git', 'cargo', 'rustc', 'rustup')) {
            if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
                throw "Falta $tool no PATH. Reabra o terminal depois da instalacao."
            }
        }

        if (-not (Get-Command cl.exe -ErrorAction SilentlyContinue)) {
            $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
            if (-not (Test-Path -LiteralPath $vswhere)) {
                throw 'Falta Visual Studio Build Tools com Desenvolvimento para desktop com C++.'
            }
            $install = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
            if (-not $install) { throw 'Falta o componente Desenvolvimento para desktop com C++.' }

            $dev = Join-Path $install 'Common7\Tools\VsDevCmd.bat'
            if (-not (Test-Path -LiteralPath $dev)) {
                throw "Arquivo do Visual Studio ausente: $dev"
            }

            # ProcessStartInfo preserva as aspas sem a conversao de argumentos
            # nativos do Windows PowerShell 5.1.
            $startInfo = New-Object System.Diagnostics.ProcessStartInfo
            $startInfo.FileName = $env:ComSpec
            $startInfo.Arguments = '/d /u /s /c "call "' + $dev + '" -arch=x64 -host_arch=x64 >nul && set"'
            $startInfo.UseShellExecute = $false
            $startInfo.CreateNoWindow = $true
            $startInfo.RedirectStandardOutput = $true
            $startInfo.RedirectStandardError = $true
            $startInfo.StandardOutputEncoding = [Text.Encoding]::Unicode

            $process = New-Object System.Diagnostics.Process
            $process.StartInfo = $startInfo
            try {
                if (-not $process.Start()) { throw 'Nao foi possivel iniciar cmd.exe.' }
                $errors = $process.StandardError.ReadToEndAsync()
                $environmentText = $process.StandardOutput.ReadToEnd()
                $process.WaitForExit()
                $errorText = $errors.Result
                if ($process.ExitCode -ne 0) {
                    throw "Falha ao carregar o ambiente C++: $errorText"
                }
                $lines = $environmentText -split '\r?\n'
            } finally {
                $process.Dispose()
            }

            foreach ($line in $lines) {
                if ($line -match '^([^=]+)=(.*)$') {
                    [Environment]::SetEnvironmentVariable($matches[1], $matches[2], 'Process')
                }
            }
            if (-not (Get-Command cl.exe -ErrorAction SilentlyContinue)) {
                throw 'O Visual Studio nao adicionou cl.exe ao PATH.'
            }
        }

        Run-Checked 'rustup' @('target', 'add', 'x86_64-pc-windows-msvc')
        if (-not $CacheCargo) {
            $searchRoots = @((Split-Path $PSScriptRoot -Parent), (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
            foreach ($searchRoot in $searchRoots) {
                foreach ($relative in @(
                    'SkinFusion-R3nz-base-v0.1\SkinFusion-R3nz-base\ltk\target',
                    'SkinFusion-R3nz-base\ltk\target',
                    'SkinFusion-v0.1-fontes\SkinFusion\target',
                    'SkinFusion-R3nz-base-v0.1\SkinFusion-R3nz-base\target'
                )) {
                    $candidate = Join-Path $searchRoot $relative
                    if (Test-Path -LiteralPath (Join-Path $candidate 'x86_64-pc-windows-msvc\release\skinfusion-customs.exe')) {
                        $CacheCargo = $candidate
                        break
                    }
                }
                if ($CacheCargo) { break }
            }
            if (-not $CacheCargo) { $CacheCargo = Join-Path $PSScriptRoot 'ltk\target' }
        }
        $CacheCargo = [IO.Path]::GetFullPath($CacheCargo)
        Write-Host "Cache do Cargo: $CacheCargo"
        Write-Host 'Compilando somente o componente de customs e reutilizando o cache. Nao e preciso pnpm ou Tauri.'
        Push-Location -LiteralPath (Join-Path $PSScriptRoot 'ltk')
        try {
            Run-Checked 'cargo' @(
                'build', '--release', '--locked', '--target', 'x86_64-pc-windows-msvc',
                '--target-dir', $CacheCargo, '-p', 'skinfusion-customs'
            )
        } finally { Pop-Location }
        $built = Join-Path $CacheCargo 'x86_64-pc-windows-msvc\release\skinfusion-customs.exe'
        Copy-Item -LiteralPath $built -Destination (Join-Path $ready 'skinfusion-customs.exe') -Force
    }

    foreach ($name in @('ltk_patcher_host.exe', 'ltk_patcher_dll.dll')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('ltk\src-tauri\resources\' + $name)) -Destination $ready -Force
    }
    $gameDll = 'originals\R3nzSkin.dll'
    if ($env:ZEPHYR_ORIGINAL_MENU -ne '1') { $gameDll = 'customized\R3nzSkin.dll' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $gameDll) -Destination (Join-Path $ready 'R3nzSkin.dll') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'originals\j5nYI6re.exe') -Destination (Join-Path $ready 'Zephyr.exe') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'addon\SkinFusion.exe.config') -Destination (Join-Path $ready 'Zephyr.exe.config') -Force
    & (Join-Path $PSScriptRoot 'VERIFY-ORIGINALS.ps1') -Pronto
    $resources = Join-Path $PSScriptRoot 'resources'
    New-Item -ItemType Directory -Force (Join-Path $resources 'originals'), (Join-Path $resources 'customized') | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'originals\j5nYI6re.exe') -Destination (Join-Path $resources 'originals\ZephyrCore.exe') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'originals\R3nzSkin.dll') -Destination (Join-Path $resources 'originals\R3nzSkin.dll') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'customized\R3nzSkin.dll') -Destination (Join-Path $resources 'customized\R3nzSkin.dll') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'addon\SkinFusion.exe.config') -Destination (Join-Path $resources 'ZephyrCore.config') -Force
    & (Join-Path $PSScriptRoot "BUILD-LAUNCHER.cmd")
    if ($LASTEXITCODE -ne 0) { throw "Launcher compilation failed." }
    Write-Host 'Concluido. Os dois binarios originals do R3nz continuam identicos.'
    if ($SomenteMenu) { Write-Host 'SomenteMenu e para testes. As funcoes Zephyr precisam do componente de customs atualizado.' }
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
