[cmdletbinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("DEV", "CIN", "GreenDoor")]
    [string]$Environment = "DEV",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("sleonard", "jahunte", "SERVER")]
    [string]$Developer = "SERVER"
)
BEGIN
{
    # AD properties to retreive
    $formattedDateRun = ([System.DateTime]::UtcNow).ToString("yyyy-MM-ddTZ")
    
    # Development Tenant
    $outputDirectory = "C:\Source\Repos\EnhancedRoomOutlookAddIn\Angular\posh"
    $configFile = ("roomfinder.dev-{0}.json" -f $Developer)

    if ($Environment -eq "CIN")
    {
        # PROD USEPA
        $outputDirectory = "C:\usepa\roomfinder\posh"
        $configFile = "roomfinder.cin.json"
    } elseif ($Environment -eq "GreenDoor")
    {
        # PROD USEPA - GreenDoor
        $outputDirectory = "C:\usepa\roomfinder\posh"
        $configFile = "roomfinder.cin-greendoor.json"
    }  

    # Move to running directory
    $scriptDirectory = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition)
    $Directories = Set-EPAOutputDirectories -outputDirectory $outputDirectory -scriptDirectory $scriptDirectory `
        -configFile $configFile -logPrefixFileName "log-provision-exo" -formattedDateRun $formattedDateRun -Verbose:$VerbosePreference

    # variables
    $CmdLetLogFile = $Directories.CmdLetLogFile
    $configFilePath = $Directories.ConfigFilePath
}
PROCESS
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    try
    {
        #Init config file
        $configKeys = Get-EPAConfigJson -jsonFile $configFilePath
        $StoredName = $configkeys.AdminCredName
        $TenantID = $configkeys.TenantID
        $roomResourceJSON = (Resolve-Path -Path $configkeys.roomResourceJSON).Path

        # Establish a connection to MSOL and PSSession
        $ConnectingUserId = Connect-EPAExchangeOnline -CmdLetLogFile $CmdLetLogFile -StoredName $StoredName -cloud AzureCloud -UseMSOL -ErrorAction Stop
        if ($null -ne $ConnectingUserId)
        {
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("EXO: Provisioning using ID:{0}" -f $ConnectingUserId) -Verbose:$VerbosePreference
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"

            try
            {
                # Room Sync with On-Premises resource
                Sync-EPAExchangeProvisioning -CmdLetLogFile $CmdLetLogFile -TenantID $TenantID -RoomResourceJSONFile $roomResourceJSON -MailboxType Room -MailboxFilter "CIN"
            } catch
            {
                # Write any Error message
                $errmsg = ("{0} stack {1}" -f $Error[0].ToString(), $Error[0].ScriptStackTrace)
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  "ERROR:--------"
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  ("Error message: {0}" -f $errmsg) -EmitError
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  "ERROR:--------"
            }

            try
            {
                # Equipment Sync with On-Premises resource
                Sync-EPAExchangeProvisioning -CmdLetLogFile $CmdLetLogFile -TenantID $TenantID -RoomResourceJSONFile $roomResourceJSON -MailboxType Equipment -MailboxFilter "CIN"
            } catch
            {
                # Write any Error message
                $errmsg = ("{0} stack {1}" -f $Error[0].ToString(), $Error[0].ScriptStackTrace)
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  "ERROR:--------"
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  ("Error message {0}:" -f $errmsg) -EmitError
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  "ERROR:--------"
            }
        } else
        {
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Failed to connect with ID:{0}" -f $StoredName) -EmitWarning
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
        }
    } catch
    {
        $msg = ("{0} stack {1}" -f $Error[0].ToString(), $Error[0].ScriptStackTrace)
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"  
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "Error: $msg" -EmitError
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------" 
    } finally
    {

        # Disconnect the PS Session [remove memory]
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "Disconnecting session" -Verbose:$VerbosePreference
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
        Disconnect-EPAExchangeOnline
    }
}