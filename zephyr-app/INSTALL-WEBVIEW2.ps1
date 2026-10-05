$ErrorActionPreference = 'Stop'
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $installer = Join-Path ([IO.Path]::GetTempPath()) ('Zephyr-WebView2-' + [Guid]::NewGuid().ToString('N') + '.exe')
    try {
        Write-Host 'Baixando Microsoft Edge WebView2 Runtime do site oficial da Microsoft...'
        Invoke-WebRequest -UseBasicParsing -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $installer
        $signature = Get-AuthenticodeSignature -LiteralPath $installer
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
            throw 'O instalador nao passou na verification da assinatura da Microsoft.'
        }
        $process = Start-Process -FilePath $installer -ArgumentList @('/silent', '/install') -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "WebView2 retornou codigo $($process.ExitCode)." }
        Write-Host 'Concluido. Feche o aplicativo e execute Zephyr.exe.'
    } finally {
        if (Test-Path -LiteralPath $installer) { Remove-Item -LiteralPath $installer -Force }
    }
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
