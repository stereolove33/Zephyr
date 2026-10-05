$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Run-Checked {
    param([string]$Program, [string[]]$Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Program failed (exit code $LASTEXITCODE)."
    }
}

foreach ($Program in @('node.exe', 'npx.cmd', 'git.exe', 'cargo.exe', 'cl.exe')) {
    if (-not (Get-Command $Program -ErrorAction SilentlyContinue)) {
        throw "Missing $Program. See BUILDING.md."
    }
}

$VendorPath = Join-Path $PSScriptRoot 'crates/dxbc-spirv-sys/vendor/dxbc-spirv'
if (-not (Test-Path (Join-Path $VendorPath 'meson.build'))) {
    $TemporaryRepo = Join-Path ([IO.Path]::GetTempPath()) ('skin-injector-source-' + [guid]::NewGuid())
    try {
        Run-Checked 'git.exe' @('init', $TemporaryRepo)
        Run-Checked 'git.exe' @('-C', $TemporaryRepo, 'remote', 'add', 'origin', 'https://github.com/LeagueToolkit/ltk-manager.git')
        Run-Checked 'git.exe' @('-C', $TemporaryRepo, 'fetch', '--depth', '1', 'origin', '3b78087aaabbd6b4afb44ec1e6e73b7dd0916d19')
        $TreeEntry = & git.exe -C $TemporaryRepo ls-tree FETCH_HEAD crates/dxbc-spirv-sys/vendor/dxbc-spirv
        if ($LASTEXITCODE -ne 0 -or $TreeEntry -notmatch '^160000 commit ([0-9a-f]{40})') {
            throw 'Could not resolve the exact dxbc-spirv submodule revision.'
        }
        $VendorCommit = $Matches[1]
        if (Test-Path $VendorPath) {
            if (@(Get-ChildItem -Force $VendorPath).Count -gt 0) {
                throw "The folder $VendorPath contains incomplete files. Check it before continuing."
            }
        }
        Run-Checked 'git.exe' @('init', $VendorPath)
        Run-Checked 'git.exe' @('-C', $VendorPath, 'remote', 'add', 'origin', 'https://github.com/doitsujin/dxbc-spirv.git')
        Run-Checked 'git.exe' @('-C', $VendorPath, 'fetch', '--depth', '1', 'origin', $VendorCommit)
        Run-Checked 'git.exe' @('-C', $VendorPath, 'checkout', '--detach', 'FETCH_HEAD')
    } finally {
        if (Test-Path $TemporaryRepo) {
            Remove-Item -LiteralPath $TemporaryRepo -Recurse -Force
        }
    }
}
Run-Checked 'git.exe' @('-C', $VendorPath, 'submodule', 'update', '--init', '--recursive')

Run-Checked 'cl.exe' @('/nologo', '/std:c++17', '/EHsc', '/W4', '/O2', '/MT', 'native/official-loader.cpp', '/Fonative/official-loader.obj', '/Fesrc-tauri/resources/r3nz/official-loader.exe')
Run-Checked 'node.exe' @('--test', 'injector-ui/model.test.mjs')

$env:HUSKY = '0'
Run-Checked 'npx.cmd' @('--yes', 'pnpm@9.14.2', 'install', '--frozen-lockfile')
Run-Checked 'npx.cmd' @('--yes', 'pnpm@9.14.2', 'build:injector')
Run-Checked 'npx.cmd' @('--yes', 'pnpm@9.14.2', 'generate:types')
Run-Checked 'npx.cmd' @('--yes', 'pnpm@9.14.2', 'typecheck')
Run-Checked 'npx.cmd' @('--yes', 'pnpm@9.14.2', 'test')
Run-Checked 'npx.cmd' @('--yes', 'pnpm@9.14.2', 'tauri', 'build')

Write-Host 'Installer generated in target/release/bundle/nsis. The executable is in target/release.'
