# CPU_Logger
A Windows service written in C# that will track any programs that utilize more than 20% of my CPU to try and figure out what is randomly spiking my CPU.

# Installation
```PowerShell
Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -Command `"& { cd '[path\to\exe\folder]'; & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe' .\CpuLoggerService.exe }`""
```

# Uninstall
```PowerShell
Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -Command `"& { cd 'path\to\exe\folder'; & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe' /u .\CpuLoggerService.exe }`""
```