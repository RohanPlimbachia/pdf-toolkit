# Removes the "Merge into PDF" right-click entry added by install-context-menu.ps1.
#   powershell -ExecutionPolicy Bypass -File scripts\uninstall-context-menu.ps1

$extensions = @(".pdf", ".jpg", ".jpeg", ".png")
$verb = "PdfToolkit.Merge"

foreach ($ext in $extensions) {
    $key = "HKCU:\Software\Classes\SystemFileAssociations\$ext\shell\$verb"
    if (Test-Path $key) { Remove-Item -Path $key -Recurse -Force }
}

Write-Host "Removed 'Merge into PDF' from the right-click menu."
