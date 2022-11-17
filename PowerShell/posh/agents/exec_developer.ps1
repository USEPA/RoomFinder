Set-ExecutionPolicy Bypass -Scope Process -Force; 
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Validate running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal( [Security.Principal.WindowsIdentity]::GetCurrent() )  
if (-not($currentPrincipal.IsInRole( [Security.Principal.WindowsBuiltInRole]::Administrator )))
{ 
    clear-host 
    Write-Error "Warning: PowerShell is NOT running as an Administrator.`n" 
    Exit 1
}


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

choco install 7zip -y; 
choco install cmder -y;
choco install googlechrome -y;
choco install firefox -y
choco install git -y; 
choco install dotnetcore-sdk --version 3.1.101 -y;
    
[System.Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Development', [System.EnvironmentVariableTarget]::Machine)
[System.Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', 1, [System.EnvironmentVariableTarget]::Machine)
    
choco install nodejs-lts --version 12.16.1 -y; 
[System.Environment]::SetEnvironmentVariable('npm_config_cache', 'c:\data\npm-cache', [System.EnvironmentVariableTarget]::Machine)
npm config set msvs_version 2019 --global

choco install vscode -y; 
choco install azure-cli -y;
choco install powershell-core -y
