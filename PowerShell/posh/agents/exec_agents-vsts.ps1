<#
$accessToken = ""
$agentPassword = ""
$url = "https://dev.azure.com/msft-epa"

# deploy agents
.\exec_agents-vsts.ps1 -agentAction deploy -agentBinarySourceDir "C:\Installs\vsts-agent-win-x64" -agentRootDirectory "C:\appdev\vsts-agent\" -agentWorkingDirectory "C:\USEPA\vsts-working\" `
  -devOpsUrl $url -personalAccessToken $accessToken -agentUserName "aa\svc_roomandresource" -agentPassword $agentPassword
  
# remove agent
.\exec_agents-vsts.ps1 -agentAction remove -agentBinarySourceDir "C:\Installs\vsts-agent-win-x64" -agentRootDirectory "C:\appdev\vsts-agent\devopsagent01" -agentWorkingDirectory "C:\USEPA\vsts-working\" `
  -devOpsUrl $url -personalAccessToken $accessToken -agentUserName "aa\svc_roomandresource" -agentPassword $agentPassword
  
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [Parameter(Mandatory = $true, HelpMessage = "The directory where the agents have been downloaded EX: C:\Installs\vsts-agent-win-x64")]
  [ValidateScript( { Test-Path $_ -PathType 'Container' })]
  [string]$agentBinarySourceDir,
    
  [Parameter(Mandatory = $true, HelpMessage = "The local target root directory to which the agents will be copied, EX: C:\appdev\vsts-agent")]
  [ValidateScript( { Test-Path $_ -PathType 'Container' })]
  [string]$agentRootDirectory,
    
  [Parameter(Mandatory = $true, HelpMessage = "The local target root directory to which the agents will be copied, EX: C:\USEPA\vsts-working")]
  [ValidateScript( { Test-Path $_ -PathType 'Container' })]
  [string]$agentWorkingDirectory,

  [ValidateSet("deploy", "remove", "query")]
  [Parameter(HelpMessage = "If not specified will query")]
  [string]$agentAction = "query",

  [Parameter(Mandatory = $true, HelpMessage = "PAT with Agent Pools (Read/Manage) permissions")]
  [string]$personalAccessToken,
    
  [Parameter(Mandatory = $true, HelpMessage = "Should be URL to the collection; https://dev.azure.com/msft-epa")]
  [string]$devOpsUrl,

  [Parameter(Mandatory = $true, HelpMessage = "Should be aa\username pattern.")]
  [string]$agentUserName,

  [Parameter(Mandatory = $true, HelpMessage = "Should be aa\username password.")]
  [string]$agentPassword,
    
  [Parameter(Mandatory = $false, HelpMessage = "Defaults to 4 agents; DevOps team internal specs are (24 cores/48 Gbs RAM) for 4 agents.")]
  [int]$numberOfAgents = 2,
    
  [Parameter(Mandatory = $false, HelpMessage = "Should be the Agent Pool to which the PAT has (Agent Pools (read, manage)) and (Deployment group (read, manage))")]
  [string]$poolName = "Default",
    
  [Parameter(Mandatory = $false, HelpMessage = "Should be unique prefix for the agent, registered in the Agent Pool")]
  [string]$agentNamePrefix = "devopsagent"
)
BEGIN {

  $currentPrincipal = New-Object Security.Principal.WindowsPrincipal( [Security.Principal.WindowsIdentity]::GetCurrent() )  
  if (-not($currentPrincipal.IsInRole( [Security.Principal.WindowsBuiltInRole]::Administrator ))) { 
    clear-host 
    Write-Error "Warning: PowerShell is NOT running as an Administrator.`n" 
    Exit 1
  }
}
PROCESS {

  if ($agentAction -eq "deploy") {
    [System.Environment]::SetEnvironmentVariable('epa.build', 'cincinatti', [System.EnvironmentVariableTarget]::Machine)
    1..$numberOfAgents | ForEach-Object {
      $agentName = ("{0}{1}" -F $agentNamePrefix, $_.ToString().PadLeft(2, '0'))
      $agentNameWithPath = Join-Path -Path $agentRootDirectory -ChildPath $agentName
      $agentWorkingWithPath = Join-Path -Path $agentWorkingDirectory -ChildPath $agentName
      Write-Host "Agent Name: $agentNameWithPath"
      If ($PSCmdlet.ShouldProcess("Adding Agent Name: $agentNameWithPath")) {
        copy-item $agentBinarySourceDir -Destination $agentNameWithPath -recurse -force
        & "$($agentNameWithPath)\config.cmd" --unattended --url $devOpsUrl --auth pat --token $personalAccessToken --agent $agentName --runAsService --windowslogonaccount $agentUserName --windowslogonpassword $agentPassword --work $agentWorkingWithPath --pool $poolName
      }
      Else {
        Write-Warning "Agent Name: $agentNameWithPath would be deployed"
      }
    }
  }
  elseif ($agentAction -eq "remove") {
    # Remove Agents example
    $agentFolders = Get-Item -Path $agentRootDirectory
    Write-Host "[Removing] Agent in folder path: $agentRootDirectory"
    If ($PSCmdlet.ShouldProcess("Removing Agents from: $agentRootDirectory")) {
      $agentFolders | ForEach-Object { 
        Write-Warning ("[Removing] Agent: {0}" -f $_.FullName)
        & "$($_.FullName)\config.cmd" remove --unattended --url $devOpsUrl --auth pat --token $personalAccessToken
      }
    }
    Else {
      Write-Warning ("Removing ({0}) Agents from: {1}" -f $agentFolders.count, $agentRootDirectory)
    }
  }
  else {
    # Query Agents example
    $agentFolders = Get-ChildItem -Path $agentRootDirectory -Directory -Force
    $agentFolders | ForEach-Object { 
      Write-Host ("Agent folder: {0}" -f $_.FullName)
    }
  }

}
