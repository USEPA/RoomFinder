Set-ExecutionPolicy Bypass -Scope Process -Force; 

# Validate running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal( [Security.Principal.WindowsIdentity]::GetCurrent() )  
if (-not($currentPrincipal.IsInRole( [Security.Principal.WindowsBuiltInRole]::Administrator ))) { 
  clear-host 
  Write-Error "Warning: PowerShell is NOT running as an Administrator.`n" 
  Exit 1
}

#check for existence of the readme file to determine if this script has already run (prevents re-running on re-deployments)
$installedSoftwareTxt = "C:\installs\InstalledChoco.txt"
if (!(Test-Path $installedSoftwareTxt)) {
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'));


  New-Item -Path "c:\installs" -ItemType Directory -Force | Out-Null
  ("{0}--Initialized" -f (get-date).ToString("o")) | Out-File -FilePath $installedSoftwareTxt;

  $CurrentPath = (Get-Location)
  Add-Content -Path $installedSoftwareTxt -Value ("{0}--installing-from-{1}" -f (get-date).ToString("o"), $CurrentPath)


  #potential next steps
  #clone our solution and pull down to the server
  #test coolbridge, fbit commands, etc
  Add-Content "c:\installs\DeploymentCount.txt" "1"
}
