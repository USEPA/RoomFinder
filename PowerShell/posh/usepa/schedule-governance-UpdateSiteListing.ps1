[CmdletBinding()]
Param(
    [Parameter(Mandatory = $false)]
    [ValidateScript( { Test-Path $_ -PathType 'Container' })]
    [string]$scriptDirectory,

    [Parameter(Mandatory = $false)]
    [Switch]$ImportModule
)
BEGIN {
    $dateSeed = Get-Date -Format MM-yyyy

    # Specifies the directory in which this should run
    $runningscriptDirectory = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition)
    if ($scriptDirectory -eq "") {
        $scriptDirectory = $runningscriptDirectory
    }
    Set-Location $scriptDirectory

    $configFile = Join-Path -Path $scriptDirectory -ChildPath "appsettings.json"
    if (!(Test-Path -Path $configFile -PathType Leaf)) {
        Write-Error -Message "Can't find Config file. EXIT Program"
        break
    }
    $moduleDispose = $false
    if ($ImportModule.IsPresent) {
        Import-Module EPA.SharePoint.PowerShell -DisableNameChecking -Verbose:$false
        $moduleDispose = $true
    }

    $logDirectory = Join-Path -Path $scriptDirectory -ChildPath "_output\logs"
    if (!(Test-Path -Path $logDirectory -PathType Container)) {
        $logDirectory = (New-Item -Path $logDirectory -Force -ItemType Directory).FullName
    }

    $logFile = Join-Path -Path $logDirectory -ChildPath ("sitelisting-{0}.txt" -f $dateSeed)
    if (!(Test-Path -Path $logFile -PathType Leaf)) {
        Write-EPALogMessage -LogFile $logFile -Msg "--------------INITIALIZING---------------"
        Write-EPALogMessage -LogFile $logFile -Msg "--- Runtime"
    }
}
PROCESS {

    try {
        #Init config file
        $configKeys = Get-EPAConfigJson -jsonFile $configFile

        # App Config Settings
        $tenanturl = $configKeys.Commands.TenantAdminUrl

        # Set user creds .. will come from config file or system managed (Posh)
        $cred = Get-EPAStoredCredential -Name "spepaonline" -Type PSCredential

        # get all site collections [SharePoint | Groups]
        Connect-SPOService -Credential $cred -Url $tenanturl
        $TeamSites = Get-SPOSite -Limit All | Select-Object -Property Url

        # get user listing from O365 AD
        Write-EPALogMessage -LogFile $logFile -Msg "Get onedrive sites............. " -Verbose:$verbosepreference
        Connect-MsolService -Credential $cred
        $Users = Get-MsolUser -All | Where-Object { $_.IsLicensed -eq $true } | Select-Object -Property UserPrincipalName

        # Connect to the Powershell Module and persist into SharePoint
        & { invoke-expression (".\epa-console.exe GetEPAAnalyticO365SiteListing --upns {0} --sharepoint-urls {1} -v" -f $Users, $TeamSites) }

    }
    catch {
        $msg = ("{0} Stack {1}" -f $Error[0].tostring(), $Error[0].ScriptStackTrace)
        Write-EPALogMessage -LogFile $logFile -Msg $msg -EmitError
    }
    finally {

    }

}
END {
    Write-EPALogMessage -LogFile $logFile -Msg "End command" -Verbose:$verbosepreference
    # Remove module
    if ($moduleDispose -eq $true) {
        Remove-Module -Name EPA.SharePoint.PowerShell -ErrorAction:SilentlyContinue -Verbose:$false
    }
}