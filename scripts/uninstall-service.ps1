$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Administrator rights are required to uninstall PriorityGear Service."
}

if (Get-Service -Name "PriorityGearService" -ErrorAction SilentlyContinue) {
    Stop-Service -Name "PriorityGearService" -ErrorAction SilentlyContinue
    sc.exe delete "PriorityGearService" | Out-Host
} else {
    Write-Host "PriorityGearService is not installed."
}
