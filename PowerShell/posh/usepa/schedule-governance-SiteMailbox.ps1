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

    $logFile = Join-Path -Path $logDirectory -ChildPath ("mailbox-{0}.txt" -f $dateSeed)
    if (!(Test-Path -Path $logFile -PathType Leaf)) {
        Write-EPALogMessage -LogFile $logFile -Msg "--------------INITIALIZING---------------"
        Write-EPALogMessage -LogFile $logFile -Msg "--- Runtime"
    }
}
PROCESS {

    try {

        #Create PS Session + Import
        $username = Connect-EPAExchangeOnline -CmdLetLogFile $logFile -StoredName "spepaonline"
        Write-EPALogMessage -LogFile $logFile -Msg ("PSSession imported for {0}" -f $username) -Verbose:$verbosepreference


        #Get site mailboxes
        $siteMailboxes = Get-SiteMailbox -ResultSize unlimited -BypassOwnerCheck
        $siteProcessing = @()
        # Enumerate through site mailboxes
        foreach ($siteMailbox in $siteMailboxes) {
            $siteEmail = @()
            $siteUrl = $siteMailbox.SharePointUrl.AbsoluteUri
            $mailBoxEmailAddresses = $siteMailbox.EmailAddresses

            Write-EPALogMessage -LogFile $logFile -Msg ("Processing: {0}" -f $siteUrl) -Verbose:$verbosepreference
            foreach ($email in $mailBoxEmailAddresses) {
                $siteEmail += $email | Out-String
            }

            $siteProcessing += [PSCustomObject]@{
                SharePointUrl     = $siteUrl
                EmailAddresses    = $siteEmail
                DistinguishedName = $siteMailbox.DistinguishedName
                UPN               = $siteMailbox.UserPrincipalName
            }
        }


        #Init config file
        #Connect to report site and get & groups list
        & { invoke-expression (".\epa-console.exe ScanEPASiteMailboxes -m {0} -d {1} -v" -f $siteProcessing, $logDirectory) }

        $removalMailboxes = Get-Content -LiteralPath ("{0}\\SiteMailbox.json" -f $logDirectory) -Raw | ConvertFrom-Json
        foreach ($mailBox in $removalMailboxes) {
            Write-EPALogMessage -LogFile $logFile -Msg ("Deleting mailbox: {0}" -f $mailBox.EmailAddresses) -EmitWarning
            Remove-Mailbox -Identity  $mailBox.DistinguishedName -Force -Confirm:$false
        }

        # close session
        Disconnect-EPAExchangeOnline
        Write-EPALogMessage -LogFile $logFile -Msg "Disconnected exchange session" -Verbose:$verbosepreference
    }
    catch {
        $msg = ("{0} Stack {1}" -f $Error[0].tostring(), $Error[0].ScriptStackTrace)
        Write-EPALogMessage -LogFile $logFile -Msg $msg -EmitError
    }

}
END {
    Write-EPALogMessage -LogFile $logFile -Msg "End command" -Verbose:$verbosepreference
    # Remove module
    if ($moduleDispose -eq $true) {
        Remove-Module -Name EPA.SharePoint.PowerShell -ErrorAction:SilentlyContinue -Verbose:$false
    }
}