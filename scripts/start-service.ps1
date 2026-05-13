$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Administrator rights are required to start PriorityGear Service."
}

Start-Service -Name "PriorityGearService"
Get-Service -Name "PriorityGearService"
