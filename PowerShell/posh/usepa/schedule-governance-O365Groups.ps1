[CmdletBinding()]
<#
.SUMMARY
Will provision O365 Groups based on the Group request site approval
#>
Param(
    [Parameter(Mandatory = $False, Position = 1)]
    [ValidateSet("create-groups", "update-groups", "cleanup-groups")]
    [string]$scriptOption = "create-groups",

    [Parameter(Mandatory = $false)]
    [ValidateScript( { Test-Path $_ -PathType 'Container' })]
    [string]$scriptDirectory
)
BEGIN {
    $runTimeDate = Get-Date -Format MM-dd-yyyy_HH_mm_ss
    $logFile = ("{0}-{1}.txt" -f $scriptOption, $runTimeDate)

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

    $logDirectory = Join-Path -Path $scriptDirectory -ChildPath "_output\logs_O365Groups"
    if (!(Test-Path -Path $logDirectory -PathType Container)) {
        $logDirectory = (New-Item -Path $logDirectory -Force -ItemType Directory).FullName
    }
}
PROCESS {

    #Set how far back in time we check for created groups
    & { invoke-expression (".\epa-console.exe ScanEPAGroupRequests -o CreateGroups -d {0} -l {1} -v" -f $logDirectory, $logFile) }
}
END {
    Write-Verbose -Message ("End command at {0}" -f $runTimeDate)
}