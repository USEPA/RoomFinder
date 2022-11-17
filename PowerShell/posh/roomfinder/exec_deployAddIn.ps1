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
    $o365group = $false

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
        $o365group = $true
    }   

    # Move to running directory
    $scriptDirectory = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition)
    $Directories = Set-EPAOutputDirectories -outputDirectory $outputDirectory -scriptDirectory $scriptDirectory `
        -configFile $configFile -logPrefixFileName "log-deployaddinscript" -formattedDateRun $formattedDateRun -Verbose:$VerbosePreference

    # variables
    $CmdLetLogFile = $Directories.CmdLetLogFile
    $configFilePath = $Directories.ConfigFilePath
}
PROCESS
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    try
    {
        ###
        ### Ensure you have run exec_agents-pkgs.ps1 or exec_developer.ps1 to install the appropriate Modules
        ###    
        Import-Module -Name AzureAD
        Import-Module -Name MSOnline  

        #Init config file
        $configKeys = Get-EPAConfigJson -jsonFile $configFilePath
        $StoredName = $configkeys.AdminCredName
        $RoomFinderAddInName = $configkeys.RoomFinderAddInName
        $RoomFinderDistroGroup = $configkeys.RoomFinderDistroGroup
        $appmanifestxml = (Resolve-Path -Path $configkeys.appmanifestxml).Path

        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "Begin deploy addin script: $formattedDateRun"  -Verbose:$VerbosePreference
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"  


        # Establish a connection to MSOL and PSSession
        $ConnectingUserId = Connect-EPAExchangeOnline -CmdLetLogFile $CmdLetLogFile -StoredName $StoredName -cloud AzureCloud -UseMSOL -Verbose:$VerbosePreference
        if ($null -ne $ConnectingUserId)
        {
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("EXO: Deploy Add-In using ID:{0}" -f $ConnectingUserId) -Verbose:$VerbosePreference
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"

            # Run the scriptlet
            Import-EPAAddIn -CmdLetLogFile $CmdLetLogFile -AppManifestXml $appmanifestxml -AppName $RoomFinderAddInName -AppRestrictionGroup $RoomFinderDistroGroup `
                -O365Group:$o365group -Verbose:$VerbosePreference
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
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "Error message: $msg" -EmitError
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