Set-ExecutionPolicy Bypass -Scope Process -Force; 
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$installedSoftwareTxt = "C:\installs\InstalledChocoPackages.txt"

# Validate running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal( [Security.Principal.WindowsIdentity]::GetCurrent() )  
if (-not($currentPrincipal.IsInRole( [Security.Principal.WindowsBuiltInRole]::Administrator )))
{ 
  clear-host 
  Write-Error "Warning: PowerShell is NOT running as an Administrator.`n" 
  Exit 1
}

#check for existence of the readme file to determine if this script has already run (prevents re-running on re-deployments)
if (!(Test-Path $installedSoftwareTxt))
{

  $module = Get-Module AzureAD -ListAvailable
  if ($null -eq $module)
  {
    Install-Module AzureAD -Scope CurrentUser -Verbose -ErrorAction:SilentlyContinue
  }
  $module = Get-Module MSOnline -ListAvailable
  if ($null -eq $module)
  {
    Install-Module MSOnline -Scope CurrentUser -Verbose -ErrorAction:SilentlyContinue
  }

  $CurrentPath = (Get-Location)
  Add-Content -Path $installedSoftwareTxt -Value ("{0}--installing-from-{1}" -f (get-date).ToString("o"), $CurrentPath)

  choco install cmder -y;
  Add-Content -Path $installedSoftwareTxt -Value ("{0}--cmder" -f (get-date).ToString("o"))
  choco install git -y; 
  Add-Content -Path $installedSoftwareTxt -Value ("{0}--git" -f (get-date).ToString("o"))
  choco install dotnetcore-sdk --version 3.1.101 -y;
  Add-Content -Path $installedSoftwareTxt -Value ("{0}--dotnetcore-sdk" -f (get-date).ToString("o"))
  choco install azure-cli -y;
  Add-Content -Path $installedSoftwareTxt -Value ("{0}--azure-cli" -f (get-date).ToString("o"))
  choco install powershell-core -y
  Add-Content -Path $installedSoftwareTxt -Value ("{0}--powershell-core" -f (get-date).ToString("o"))


  $azdoagenturi = 'https://vstsagentpackage.azureedge.net/agent/2.169.1/vsts-agent-win-x64-2.169.1.zip'
  $zipFile = 'C:\installs\vsts-agent-win-x64-2.169.1.zip'
  $installAgent = 'C:\installs\vsts-agent-win-x64'
  Invoke-WebRequest $azdoagenturi -OutFile $zipFile
  Expand-Archive -Path $zipFile -DestinationPath $installAgent -Force -Verbose:$VerbosePreference

      
  [System.Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', 1, [System.EnvironmentVariableTarget]::Machine)


  #potential next steps
  #clone our solution and pull down to the server
  #test coolbridge, fbit commands, etc
  Add-Content "c:\installs\DeploymentCount.txt" "2"
}