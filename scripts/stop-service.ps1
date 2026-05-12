$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Administrator rights are required to stop PriorityGear Service."
}

Stop-Service -Name "PriorityGearService"
Get-Service -Name "PriorityGearService"
