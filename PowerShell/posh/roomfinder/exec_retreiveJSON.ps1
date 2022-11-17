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
        -configFile $configFile -logPrefixFileName "log-retreivejson" -formattedDateRun $formattedDateRun -Verbose:$VerbosePreference

    # variables
    $CmdLetLogFile = $Directories.CmdLetLogFile
    $configFilePath = $Directories.ConfigFilePath
    $OutputDirectory = $Directories.OutputDirectory
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

        # Establish a connection to MSOL and PSSession
        $ConnectingUserId = Connect-EPAExchangeOnline -CmdLetLogFile $CmdLetLogFile -StoredName $StoredName -cloud AzureCloud -UseMSOL
        if ($null -ne $ConnectingUserId)
        {
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("EXO: Retreive JSON using ID:{0}" -f $ConnectingUserId) -Verbose:$VerbosePreference
            
            $jsonFile = Join-Path -Path $OutputDirectory -ChildPath ("resources-objects.{0}-{1}.json" -f $Environment.ToLower(), $formattedDateRun)
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("retreiveJSON -File ({0})" -f $jsonFile)
            
            # Run the scriptlet
            $jsonObjects = Receive-ExchangeResources -CmdLetLogFile $CmdLetLogFile -TenantID $TenantID -MailboxFilter "CIN" -Verbose:$verbosepreference 
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Writing JSON to {0}" -f $jsonFile) -Verbose:$VerbosePreference
            ConvertTo-Json $jsonObjects -Depth 5 | Out-File -FilePath $jsonFile -Force
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