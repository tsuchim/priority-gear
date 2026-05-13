param(
    [string]$ServiceExe = (Resolve-Path "$PSScriptRoot\..\src\PriorityGear.Service\bin\Release\net10.0\PriorityGear.Service.exe").Path
)

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Administrator rights are required to install PriorityGear Service."
}

if (-not (Test-Path $ServiceExe)) {
    throw "Service binary not found: $ServiceExe. Build Release first."
}

New-Service -Name "PriorityGearService" -DisplayName "PriorityGear Service" -BinaryPathName "`"$ServiceExe`"" -StartupType Manual
