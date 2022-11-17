[cmdletbinding(SupportsShouldProcess = $True)]
<#
.SUMMARY
Will run the analytics with interactive login/scoped permissions
#>
param(
    [Parameter(Mandatory = $false)]
    [ValidateScript( { Test-Path $_ -PathType 'Container' })]
    [string]$scriptDirectory
)
BEGIN {
    $runTimeDate = Get-Date -Format MM-dd-yyyy_HH_mm_ss
    $logFile = "o365groups.txt"

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

    # Connect to Tenant Admin
    # Process O365 Unified Groups
    & { invoke-expression (".\epa-console.exe getoffice365groups --starts-with-letter 'A' --ends-with-letter 'Z' --starts-with-number 1 -l {0}" -f $logFile) }

}
END {
    Write-Verbose -Message ("End command at {0}" -f $runTimeDate)
}