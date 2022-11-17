$ErrorActionPreference = "Stop";

$personalAccessToken = ""
$devOpsUrl = "https://dev.azure.com/msft-epa"
$agentRootDirectory = "C:\appdev\vsts-agent\azagent\A1"
$agentFolders = Get-Item -Path $agentRootDirectory
Write-Host "[Removing] Agent in folder path: $agentRootDirectory"
$agentFolders | ForEach-Object { 
    Write-Warning ("[Removing] Agent: {0}" -f $_.FullName)
    & "$($_.FullName)\config.cmd" remove --unattended --url $devOpsUrl --auth pat --token $personalAccessToken
}


.\config.cmd remove --unattended --auth PAT --token $personalAccessToken
