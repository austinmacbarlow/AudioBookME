#Requires -Version 5.1
<#
    build.ps1 — Compile AudiobookMaker into a Windows exe.
    Run from the repo root in a Windows PowerShell terminal:
        cd C:\Users\Austin\code\audioprocessor
        .\build.ps1

    Produces:  .\publish\AudiobookMaker.exe

    The exe is framework-dependent (~1 MB) and requires .NET 8 runtime.
    Windows will prompt to install it automatically if missing.

    For a fully self-contained exe (~80 MB, no runtime needed at all):
        Change:  --self-contained false
        To:      --self-contained true
#>

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error '.NET SDK not found. Download it from https://dot.net and re-run.'
}

$proj   = Join-Path $PSScriptRoot 'AudiobookMaker.csproj'
$outDir = Join-Path $PSScriptRoot 'publish'

Write-Host 'Building AudiobookMaker...'
dotnet publish $proj -c Release -o $outDir

$exe = Join-Path $outDir 'AudiobookMaker.exe'
if (Test-Path $exe) {
    Write-Host "Done: $exe ($([math]::Round((Get-Item $exe).Length / 1MB, 1)) MB)"
} else {
    Write-Error "Build failed - AudiobookMaker.exe not found in $outDir"
}
