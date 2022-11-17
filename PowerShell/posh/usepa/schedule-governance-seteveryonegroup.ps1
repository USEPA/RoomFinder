[CmdletBinding()]
Param(
    [Parameter(Mandatory = $false)]
    [ValidateScript( { Test-Path $_ -PathType 'Container' })]
    [string]$scriptDirectory
)
BEGIN {
    $runTimeDate = Get-Date -Format MM-dd-yyyy_HH_mm_ss
    $logFile = "seteveryonegroup.txt"

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

    $logDirectory = Join-Path -Path $scriptDirectory -ChildPath "_output\logs"
    if (!(Test-Path -Path $logDirectory -PathType Container)) {
        $logDirectory = (New-Item -Path $logDirectory -Force -ItemType Directory).FullName
    }
}
PROCESS {

    # Query sites and look for the Everyone group and replace it with the UG Group
    # What would happen if this runs [does not actually perform updates]
    & { invoke-expression (".\epa-console.exe scanEPASetEveryoneGroup --process-eg --process-shared -l {0} -v" -f $logFile) }

}
END {
    Write-Verbose -Message ("End command at {0}" -f $runTimeDate)
}