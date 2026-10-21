# Adds "Merge into PDF" to the right-click menu for PDFs and images.
# Writes to HKEY_CURRENT_USER only, so no admin rights are needed.
#
# Usage (from the repo folder):
#   powershell -ExecutionPolicy Bypass -File scripts\install-context-menu.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\install-context-menu.ps1 -ExePath "C:\Tools\PdfMerge.exe"

param(
    [string]$ExePath = (Join-Path $PSScriptRoot "..\publish\PdfMerge.exe")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ExePath)) {
    Write-Error "Couldn't find PdfMerge.exe at '$ExePath'. Run the publish step first (see README)."
}
$ExePath = (Resolve-Path $ExePath).Path

$extensions = @(".pdf", ".jpg", ".jpeg", ".png")
$verb = "PdfToolkit.Merge"

foreach ($ext in $extensions) {
    $key = "HKCU:\Software\Classes\SystemFileAssociations\$ext\shell\$verb"
    New-Item -Path $key -Force | Out-Null
    Set-ItemProperty -Path $key -Name "MUIVerb" -Value "Merge into PDF"
    Set-ItemProperty -Path $key -Name "Icon" -Value "`"$ExePath`""
    # "Player" lifts Explorer's default 15-file limit for multi-select verbs.
    Set-ItemProperty -Path $key -Name "MultiSelectModel" -Value "Player"

    $cmdKey = "$key\command"
    New-Item -Path $cmdKey -Force | Out-Null
    Set-ItemProperty -Path $cmdKey -Name "(default)" -Value "`"$ExePath`" `"%1`""
}

Write-Host "Done. Select PDFs or images in File Explorer, right-click, and choose 'Merge into PDF'."
Write-Host "(On Windows 11 it's under 'Show more options', or Shift+Right-click.)"
