$ErrorActionPreference = "Stop";
$personalAccessToken = ""
$devOpsUrl = "https://dev.azure.com/msft-epa"
If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent() ).IsInRole( [Security.Principal.WindowsBuiltInRole] "Administrator"))
{ 
    throw "Run command in an administrator PowerShell prompt"
};
If ($PSVersionTable.PSVersion -lt (New-Object System.Version("3.0")))
{ 
    throw "The minimum version of Windows PowerShell that is required by the script (3.0) does not match the currently running version of Windows PowerShell." 
}
If (-NOT (Test-Path $env:SystemDrive\'appdev\vsts-agent\azagent'))
{ 
    mkdir $env:SystemDrive\'appdev\vsts-agent\azagent' 
}; 
Set-Location $env:SystemDrive\'appdev\vsts-agent\azagent'; 
for ($i = 1; $i -lt 100; $i++)
{
    $destFolder = "A" + $i.ToString(); 
    if (-NOT (Test-Path ($destFolder)))
    {
        mkdir $destFolder;
        Set-Location $destFolder; break;
    }
}; 
$agentZip = "$PWD\agent.zip"; 
$DefaultProxy = [System.Net.WebRequest]::DefaultWebProxy; 
$securityProtocol = @();
$securityProtocol += [Net.ServicePointManager]::SecurityProtocol; 
$securityProtocol += [Net.SecurityProtocolType]::Tls12; [Net.ServicePointManager]::SecurityProtocol = $securityProtocol;
$WebClient = New-Object Net.WebClient; 
$azdoagenturi = 'https://vstsagentpackage.azureedge.net/agent/2.169.1/vsts-agent-win-x64-2.169.1.zip'; 
if ($DefaultProxy -and (-not $DefaultProxy.IsBypassed($azdoagenturi)))
{
    $WebClient.Proxy = New-Object Net.WebProxy($DefaultProxy.GetProxy($azdoagenturi).OriginalString, $True);
}; 
$WebClient.DownloadFile($azdoagenturi, $agentZip); 
Add-Type -AssemblyName System.IO.Compression.FileSystem; 
[System.IO.Compression.ZipFile]::ExtractToDirectory( $agentZip, "$PWD"); 
.\config.cmd --deploymentgroup --deploymentgroupname "gaway00-cincy" --agent $env:COMPUTERNAME --runasservice --work '_work' --url $devOpsUrl --projectname 'EnhancedRoomOutlookAddIn' --addDeploymentGroupTags --deploymentGroupTags "iis, usepa" --auth PAT --token $personalAccessToken --acceptTeeEula 
Remove-Item $agentZip;