
class JsonRoomFinder
{
    [string[]]$equipment;
    [string[]]$equipmentList;
    [string[]]$locations;
    [JsonConfigMailbox[]]$mailboxes;
    [JsonConfigEquipment[]]$equipments;
    JsonRoomFinder()
    {
        $this.equipment = [string[]]::new(0);
        $this.equipmentList = [string[]]::new(0);
        $this.locations = [JsonConfigLocations[]]::new(0);
        $this.mailboxes = [JsonConfigMailbox[]]::new(0)
        $this.equipments = [JsonConfigEquipment[]]::new(0)
    }
}

class JsonConfigLocations
{
    [string]$displayName;
    [string]$emailAddress;
}

class JsonConfigMailbox
{
    [string]$samAccountName;
    [string]$userPrincipalName;
    [string]$primarySmtpAddress;
    [string]$accountName;
    [string]$name;
    [string]$emailAddress;
    [string]$displayName;
    [int]$resourceCapacity;
    [string]$restrictionType;
    [string]$bookingProcessing;
    [string]$city;
    [string]$company;
    [string]$countryOrRegion;
    [string]$department;
    [string]$floor;
    [string]$office;
    [string]$phone;
    [string]$postalCode;
    [string]$stateOrProvince;
    [JsonRestrictedDelegates[]]$restrictedDelegates;
    [string[]]$dependencies;
    [string[]]$equipment;
    [string[]]$equipmentDependencies;
    [string]$samGUID;
    [string]$exchangeObjectId;
    [string]$guid;
    JsonConfigMailbox()
    {
        $this.restrictionType = "None";
        $this.resourceCapacity = 0;
        $this.bookingProcessing = "AutoAccept";
        $this.equipment = [string[]]::new(0);
        $this.equipmentDependencies = [string[]]::new(0);
        $this.dependencies = [string[]]::new(0);
        $this.restrictedDelegates = [JsonRestrictedDelegates[]]::new(0);
    }
}

class JsonConfigEquipment
{
    [string]$samAccountName;
    [string]$userPrincipalName;
    [string]$primarySmtpAddress;
    [string]$accountName;
    [string]$name;
    [string]$emailAddress;
    [string]$displayName;
    [int]$resourceCapacity;
    [string]$restrictionType;
    [string]$bookingProcessing;
    [string]$city;
    [string]$company;
    [string]$countryOrRegion;
    [string]$department;
    [string]$floor;
    [string]$office;
    [string]$phone;
    [string]$postalCode;
    [string]$stateOrProvince;
    [string[]]$equipment;
    [string]$equipmentType;
    [JsonRestrictedDelegates[]]$restrictedDelegates;
    [string[]]$dependencies;
    [string]$samGUID;
    [string]$exchangeObjectId;
    [string]$guid;
    JsonConfigEquipment()
    {
        $this.restrictionType = "None";
        $this.resourceCapacity = 0;
        $this.bookingProcessing = "AutoAccept";
        $this.dependencies = [string[]]::new(0);
        $this.restrictedDelegates = [JsonRestrictedDelegates[]]::new(0);
    }
}

class JsonRestrictedDelegates
{
    [string]$userPrincipalName;
    [string]$userType;
}



function Use-EPAMSOnline
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile
    )
    begin
    {
        Write-Verbose "..  Loading MSOnline"
    }
    process
    {

        Import-Module MSOnline -Force
    }
    end
    {
        Write-Verbose ".. MSOnline loaded"
    }
}

function Connect-EPAExchangeOnline
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [outputtype([string])]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,

        [Parameter(Mandatory = $true, HelpMessage = "The generic credentials name")]
        [string]$StoredName,

        [Parameter(Mandatory = $false)]
        [ValidateSet("AzureCloud", "USGovernment")]
        [string]$cloud = "AzureCloud",

        [Parameter(Mandatory = $false, HelpMessage = "MSOL module switch for connection with MSOL Service.")]
        [switch]$UseMSOL
    )
    begin
    {
        $upn = $null
    }
    process
    {
        try
        {
            $credentials = Get-EPAStoredCredential -Name $StoredName -Type PSCredential -ErrorAction:Stop
            $upn = $credentials.UserName

            if ($UseMSOL.IsPresent -and $UseMSOL)
            {
                Connect-MSOLService -credential $credentials -ErrorAction Stop
            }

            $session365 = Get-PSSession
            if ($null -eq $session365 -or $session365[0].State -ne "Opened")
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Establishing new PSSession for {0}" -f $upn) -Verbose:$VerbosePreference
                $session365 = New-PSSession -ConfigurationName Microsoft.Exchange -ConnectionUri https://ps.outlook.com/powershell/ -Credential $credentials -Authentication Basic -AllowRedirection
            }

            # Import the PS Module globally so the consuming scripts can have access to the commands
            Import-Module (Import-PSSession $session365 -AllowClobber -Verbose:$false) -Global -Verbose:$false
        } catch
        {
            $msg = ("{0} stack {1}" -f $Error[0].ToString(), $Error[0].ScriptStackTrace)
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------"  
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "Error: $msg" -EmitError
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------------------------------------" 
        }
        Write-Output $upn
    }
}

function Get-EPAConfigKeys
{
    <#
.SUMMARY
Reads config keyvaluepairs from config file
#>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true)]
        [ValidateScript( { Test-Path $_ -PathType 'Leaf' })]
        [string]$configFile
    )
    PROCESS
    {
        [xml]$config = Get-Content $configFile
        $configKeys = @{ }
        select-xml -xml $config -XPath "//configuration/appSettings/add" | ForEach-Object {
            $configKeys.Add($_.Node.key, $_.Node.value);
        }
        Write-Output $configKeys
    }
}

function Get-EPAConfigJson
{
    <#
.SUMMARY
Reads config keyvaluepairs from config file
#>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateScript( { Test-Path $_ -PathType 'Leaf' })]
        [string]$jsonFile
    )
    PROCESS
    {
        $json = get-content -Raw -Path $jsonFile
        $configKeys = ConvertFrom-Json -InputObject $json
        Write-Output $configKeys
    }
}

function Set-EPAOutputDirectories
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateScript( { Test-Path $_ -PathType 'Container' })]
        [string]$outputDirectory,
        
        [Parameter(Mandatory = $true)]
        [string]$scriptDirectory,
        
        [Parameter(Mandatory = $true)]
        [string]$configFile,
        
        [Parameter(Mandatory = $true)]
        [string]$logPrefixFileName,

        [Parameter(Mandatory = $true)]
        [string]$formattedDateRun
    )
    PROCESS
    {
        if (!(Test-Path -Path $scriptDirectory -PathType 'Container' -ErrorAction SilentlyContinue))
        {
            $scriptDirectory = $outputDirectory
        }
        Set-Location $scriptDirectory
    
        # Create output path
        $scriptingOutputDirectory = Join-Path -Path $scriptDirectory -ChildPath "_output"
        if (!(Test-Path -Path $scriptingOutputDirectory -PathType Container))
        {
            $scriptingOutputDirectory = (New-Item -Path $scriptingOutputDirectory -Force -ItemType Directory).FullName
        }
    
        # Setup the log file. 
        $logDirectory = Join-Path -Path $scriptingOutputDirectory -ChildPath "logs"
        if (!(Test-Path -Path $logDirectory -PathType Container))
        {
            $logDirectory = (New-Item -Path $logDirectory -Force -ItemType Directory).FullName
        }
    
        # build exported JSON file
        $LogFile = Join-Path -Path $logDirectory -ChildPath ("{0}.log" -f $logPrefixFileName)
        $CmdLetLogFile = Join-Path -Path $logDirectory -ChildPath ("{0}-{1}.log" -f $logPrefixFileName, $formattedDateRun)
        $configFilePath = Join-Path -Path $scriptDirectory -ChildPath $configFile 
        
        # Logging
        Write-EPALogMessage -LogFile $LogFile -Msg "------------------------------------------------" -ErrorAction Stop 
        Write-EPALogMessage -LogFile $LogFile -Msg "Log file: $CmdLetLogFile" 
        Write-EPALogMessage -LogFile $LogFile -Msg "------------------------------------------------"  
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg "------------------Initialized-------------------"  
        
        $LogObject = @{
            LogFile = $LogFile
            OutputDirectory = $scriptingOutputDirectory
            CmdLetLogFile = $CmdLetLogFile
            ConfigFilePath = $configFilePath
        }

        Write-Output $LogObject
    } 
}

function Get-EPACollectionOfUsers
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true, HelpMessage = "An object representing a distribution group.")]
        $group, 

        [Parameter(Mandatory = $true, HelpMessage = "A collection of users to be enumerated.")]
        $users
    ) 
    PROCESS
    {
        $user_list = @()
        $usersLength = ($users | Measure-Object).Count
        for ($i = 0; $i -lt $usersLength; $i++)
        {
            $user_info = $users[$i]
            if ($null -ne $user_info.PrimarySmtpAddress -and ($user_info.PrimarySmtpAddress.contains("epa.gov") -or $user_info.PrimarySmtpAddress.contains("usepa.onmicrosoft.com")))
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Group {0} existing member {1} will be enabled to the Add-In" -f $group.DisplayName, $user_info.PrimarySmtpAddress)  -Verbose:$VerbosePreference
                $user_list += $user_info.PrimarySmtpAddress
            }
        }
        Write-Output $user_list
    }
}

Function Remove-EPAADInvalidCharacters
{
    <#
.SUMMARY
Function to remove invalid characters.
#>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [outputtype([string])]
    param(        
        [Parameter(Mandatory = $true)]
        [string]$SamAccountName
    )
    process
    {

        [regex]$Reg = "(`"|:|;|=|<|>|/|,|\?|\[|\]|\||\+|\*|\\)"
        If ($Reg.Matches($SamAccountName).Count -eq 0)
        {
            If ($SamAccountName.Length -gt 0)
            { $SamAccountName = $SamAccountName.Trim() 
            }
            Return $SamAccountName
        } Else
        {
            Write-Verbose ("## Invalid Characters Removed: Name {0}" -f $SamAccountName)
            # Remove invalid characters and trim leading and trailing blanks.
            $SamAccountName = $SamAccountName.Replace("`"", "").Replace("[", "").Replace("]", "")
            $SamAccountName = $SamAccountName.Replace(":", "").Replace(";", "").Replace("|", "")
            $SamAccountName = $SamAccountName.Replace("=", "").Replace("+", "").Replace("*", "")
            $SamAccountName = $SamAccountName.Replace("?", "").Replace("<", "").Replace(">", "")
            $SamAccountName = $SamAccountName.Replace("\", "").Replace("/", "")
            If ($SamAccountName.Length -gt 0)
            { $SamAccountName = $SamAccountName.Trim() 
            }
            $Script:Invalid = $Script:Invalid + 1
            Return $SamAccountName
        }
    }
}

Function Set-EPAADTruncateSamAccountName
{
    <#
.SUMMARY
Function to truncate names longer than 20 characters.
#>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [outputtype([string])]
    param(        
        [Parameter(Mandatory = $true)]
        [string]$SamAccountName
    )
    process
    {
        If ($SamAccountName.Length -le 20)
        { Return $SamAccountName 
        } Else
        {
            Write-Verbose ("## Truncated: sAMAccountName {0} is too long" -f $SamAccountName)
            # Truncate to first 20 characters.
            $SamAccountName = $SamAccountName.SubString(0, 20).Trim()
            $Script:Long = $Script:Long + 1
            Return $SamAccountName
        }
    }
}

Function Get-EPAUserPrincipalName
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [outputtype([string])]
    param(        
        [Parameter(Mandatory = $true)]
        [string]$windowsEmailAddress,

        [Parameter(Mandatory = $true)]
        [string]$TenantID
    )
    begin
    {
        $DomainObj = $null
    }
    process
    {

        $NewUniqueName = $windowsEmailAddress
        $obj = $windowsEmailAddress.Split("{@}")
        if (($obj | Measure-Object).Count -gt 0)
        {
            $NewUniqueName = $obj[0]
        }

        $DomainObj = [pscustomobject]@{
            userPrincipalName = ("{0}@{1}" -f $NewUniqueName, $TenantID)
            UniqueName        = $NewUniqueName
        }
        Write-Output $DomainObj
    }
}

Function Get-EPAADUserPrincipalName
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [outputtype([string])]
    param(        
        [Parameter(Mandatory = $true)]
        [string]$windowsEmailAddress,

        [Parameter(Mandatory = $true)]
        [string]$TenantID
    )
    begin
    {
        $DomainObj = $null
    }
    process
    {

        $NewUniqueName = $windowsEmailAddress
        $obj = $windowsEmailAddress.Split("{@}")
        if (($obj | Measure-Object).Count -gt 0)
        {
            $NewUniqueName = $obj[0]
        }

        $DomainObj = [pscustomobject]@{
            userPrincipalName = ("{0}@{1}" -f $NewUniqueName, $TenantID)
            UniqueName        = $NewUniqueName
        }
        Write-Output $DomainObj
    }
}

Function Get-EPACleanEmailAddress
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Alias
    )
    Process
    {
        $IllegalCharacters = 0..34 + 40..41 + 44, 47 + 58..60 + 62 + 64 + 91..93 + 127..160
        foreach ($c in $IllegalCharacters)
        {
            $escaped = [regex]::Escape([char]$c)

            if ($Alias -match $escaped)
            {
                Write-Verbose "illegal character code detected: '$c'"
                $Alias = $Alias -replace $escaped
            }
        }
        Write-Output $Alias
    }
}

function Get-EPAWorkingPath
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [OutputType([string])]
    param(
    )
    process
    {
        # get running path
        $myScriptPath = (Split-Path -Parent $MyInvocation.MyCommand.Path)
        Write-Output $myScriptPath
    }
    end
    {
    }
}

function Get-EPAADImmutableId
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true, Position = 0)]
        $aduser
    )
    BEGIN
    {
        $immutableid = $null
    }
    PROCESS
    {
        try
        {
            $objguid = $aduser.ObjectGUID
            $immutableid = [System.Convert]::ToBase64String($objguid.ToByteArray())
        } catch
        {
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Failed to parse immutable ID {0}" -f $error[0]) -EmitWarning
        }
    }
    END
    {
        Write-Output $immutableid
    }
}

function Get-EPAADRoomFinderLocationName
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [string]$objectName
    )
    process
    {
        $gObjectName = $objectName -replace '[^a-zA-Z0-9]', ''
        $gSamAccount = ("loc_{0}" -f $gObjectName)
        return $gSamAccount
    }
}

function Get-EPAADRoomFinderEquipmentName
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [string]$objectName
    )
    process
    {
        $gObjectName = $objectName -replace '[^a-zA-Z0-9]', ''
        $gSamAccount = ("equip_{0}" -f $gObjectName)
        return $gSamAccount
    }
}

function Get-EPAADRoomFinderEquipmentTypeName
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [string]$objectName
    )
    process
    {
        $gObjectName = $objectName -replace '[^a-zA-Z0-9]', ''
        $gSamAccount = ("equType_{0}" -f $gObjectName)
        return $gSamAccount
    }
}

function Wait-EPAExchangeMailboxCreated
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$userPrincipalName,

        [Parameter(Mandatory = $false)]
        [Int]$maxRetries = 20
    )
    BEGIN
    {
        $uidx = 0
        $midx = 0
        $MSOUserObj = $null
        if ($?)
        {
            $Error.Clear()
        }
    }
    PROCESS
    {

        Do
        {
            $check = $true
            $MSOUserObj = $null
            $MSOUserObj = Get-MsolUser -UserPrincipalName $userPrincipalName -ErrorAction SilentlyContinue
            if ($null -eq $MSOUserObj -or (($MSOUserObj | Measure-Object).Count -le 0))
            {
                $uidx = $uidx + 1
                if ($uidx -ge $maxRetries)
                {
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Failed to Get-MsolUser within {0} retries" -f $maxRetries) -EmitWarning
                    $check = $false
                    break
                }
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Checking MSOL {0} iteration {1} pausing 5 seconds ...." -f $userPrincipalName, $uidx) -Verbose:$VerbosePreference
                Start-Sleep -Seconds 5
            } else
            {
                $check = $false
            }
        } while ($check -eq $true)

        Do
        {
            $check = $true
            $EXOMailbox = $null
            $EXOMailbox = Get-Mailbox -Identity $userPrincipalName -ErrorAction SilentlyContinue
            if ($null -eq $EXOMailbox -or (($EXOMailbox | Measure-Object).Count -le 0))
            {
                $midx = $midx + 1
                if ($midx -ge $maxRetries)
                {
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Failed to Get-Mailbox within {0} retries" -f $maxRetries) -EmitWarning
                    $check = $false
                    break
                }
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Checking EXO {0} iteration {1} pausing 5 seconds ...." -f $userPrincipalName, $midx) -Verbose:$VerbosePreference
                Start-Sleep -Seconds 5
            } else
            {
                $check = $false
            }

        } while ($check -eq $true)

    }
    END
    {
        return $MSOUserObj
    }
}

function New-EPAExchangeMailbox
{
    [cmdletbinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$DisplayName,
        [Parameter(Mandatory = $true)]
        [string]$userPrincipalName,
        [Parameter(Mandatory = $true)]
        [string]$MailboxAlias,
        [Parameter(Mandatory = $true)]
        [string]$PrimarySmtpAddress,
        [Parameter(Mandatory = $true)]
        [ValidateSet("Room", "Equipment")]
        [string]$MailboxType,
        [Parameter(Mandatory = $false)]
        [string]$ResourceCapacity = 0
    )
    BEGIN
    {
        $CloudMailboxObj = $null
    }
    PROCESS
    {

        if ($PSCmdlet.ShouldProcess("ShouldProcess new exchange mailbox?"))
        {
            # Mailbox [Room]
            if ($MailboxType -eq "Room")
            {
                $CloudMailboxObj = New-Mailbox -Room -DisplayName $DisplayName `
                    -Alias $MailboxAlias -PrimarySmtpAddress $PrimarySmtpAddress -Name $Name `
                    -ResourceCapacity $ResourceCapacity
            }

            # Mailbox [Equipment]
            if ($MailboxType -eq "Equipment")
            {
                $CloudMailboxObj = New-Mailbox -Equipment -DisplayName $DisplayName `
                    -Alias $MailboxAlias -PrimarySmtpAddress $PrimarySmtpAddress -Name $Name

                $CloudMailboxObj = Get-Mailbox -Identity $userPrincipalName
                if ($null -ne $CloudMailboxObj)
                {
                    $CloudMailboxObj | Set-Mailbox -ResourceCapacity $ResourceCapacity
                }
            }

            # due to replication this process might take a few minutes; lets pause until we find it
            Wait-EPAExchangeMailboxCreated -CmdLetLogFile $CmdLetLogFile -userPrincipalName $userPrincipalName
        }
        # Azure AD and EXO should have the object by now
        $CloudMailboxObj = $null
        $CloudMailboxObj = Get-EPAExchangeMailbox -CmdLetLogFile $CmdLetLogFile -upn $userPrincipalName
    }
    END
    {
        return $CloudMailboxObj
    }
}

function Get-EPAMailboxProperties
{
    <#
    .SUMMARY
    Provides an exchange object composed of the User/ContactInfo/Booking information
    #>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [OutputType([JsonConfigMailbox])]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true)]
        $CloudMailboxObj
    )
    PROCESS
    {
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Mailbox processing {0} - {1}" -f $CloudMailboxObj.UserPrincipalName, $CloudMailboxObj.ResourceType) -Verbose:$VerbosePreference
        [string] $restricted = "None"
        $BookingInfo = Get-CalendarProcessing -Identity $CloudMailboxObj.SamAccountName
        $delegates = [JsonRestrictedDelegates[]]::new(0)

        $bookingProcessing = $BookingInfo.AutomateProcessing
        if (($BookingInfo.ResourceDelegates | Measure-Object).Count -gt 0)
        {
            $restricted = "ApprovalRequired"
            foreach ($delegateuser in $BookingInfo.ResourceDelegates)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Checking {0} delegates {1}" -f $CloudMailboxObj.WindowsEmailAddress, $delegateuser) -Verbose:$VerbosePreference
                [JsonRestrictedDelegates] $userId = Get-EPAExchangeMailboxBookingResource -CmdLetLogFile $CmdLetLogFile -upn $delegateuser
                $delegates += $userId
            }
        }

        if (($BookingInfo.BookInPolicy | Measure-Object).Count -gt 0)
        {
            $restricted = "Restricted"
            foreach ($bookinuser in $BookingInfo.BookInPolicy)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Checking {0} bookinpolicy {1}" -f $CloudMailboxObj.WindowsEmailAddress, $bookinuser) -Verbose:$VerbosePreference
                [JsonRestrictedDelegates] $userId = Get-EPAExchangeMailboxBookingResource -CmdLetLogFile $CmdLetLogFile -upn $bookinuser
                $delegates += $userId
            }
        }

        # retreive room/equipment location/properties
        $ContactInfo = (Get-User -Identity $CloudMailboxObj.SamAccountName)


        [JsonConfigMailbox] $jObject = [pscustomobject]@{
            samAccountName        = $CloudMailboxObj.SamAccountName
            userPrincipalName     = $CloudMailboxObj.UserPrincipalName
            primarySmtpAddress    = $CloudMailboxObj.PrimarySmtpAddress
            name                  = $CloudMailboxObj.Name
            emailAddress          = $CloudMailboxObj.WindowsEmailAddress
            displayName           = $CloudMailboxObj.DisplayName
            resourceCapacity      = $CloudMailboxObj.ResourceCapacity
            restrictionType       = $restricted
            bookingProcessing     = $bookingProcessing
            city                  = $ContactInfo.City
            company               = $ContactInfo.Company
            countryOrRegion       = $ContactInfo.CountryOrRegion
            department            = $ContactInfo.Department
            floor                 = ""
            office                = $ContactInfo.Office
            phone                 = $ContactInfo.Phone
            postalCode            = $ContactInfo.PostalCode
            stateOrProvince       = $ContactInfo.StateOrProvince
            equipment             = @()
            equipmentDependencies = @()
            dependencies          = @()
            restrictedDelegates   = $delegates
        }
        return $jObject
    }
}

function Get-EPAEquipmentProperties
{
    <#
    .SUMMARY
    Provides an exchange object composed of the User/ContactInfo/Booking information
    #>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [OutputType([JsonConfigEquipment])]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true)]
        $CloudMailboxObj
    )
    PROCESS
    {
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Mailbox processing {0} - {1}" -f $CloudMailboxObj.UserPrincipalName, $CloudMailboxObj.ResourceType) -Verbose:$VerbosePreference
        [string] $restricted = "None"
        $BookingInfo = Get-CalendarProcessing -Identity $CloudMailboxObj.SamAccountName
        $delegates = [JsonRestrictedDelegates[]]::new(0)

        $bookingProcessing = $BookingInfo.AutomateProcessing
        if (($BookingInfo.ResourceDelegates | Measure-Object).Count -gt 0)
        {
            $restricted = "ApprovalRequired"
            foreach ($delegateuser in $BookingInfo.ResourceDelegates)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Checking {0} delegates {1}" -f $CloudMailboxObj.WindowsEmailAddress, $delegateuser) -Verbose:$VerbosePreference
                [JsonRestrictedDelegates] $userId = Get-EPAExchangeMailboxBookingResource -CmdLetLogFile $CmdLetLogFile -upn $delegateuser
                $delegates += $userId
            }
        }

        if (($BookingInfo.BookInPolicy | Measure-Object).Count -gt 0)
        {
            $restricted = "Restricted"
            foreach ($bookinuser in $BookingInfo.BookInPolicy)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Checking {0} bookinpolicy {1}" -f $CloudMailboxObj.WindowsEmailAddress, $bookinuser) -Verbose:$VerbosePreference
                [JsonRestrictedDelegates] $userId = Get-EPAExchangeMailboxBookingResource -CmdLetLogFile $CmdLetLogFile -upn $bookinuser
                $delegates += $userId
            }
        }

        # retreive room/equipment location/properties
        $ContactInfo = (Get-User -Identity $CloudMailboxObj.SamAccountName)


        [JsonConfigEquipment] $jObject = [pscustomobject]@{
            samAccountName      = $CloudMailboxObj.SamAccountName
            userPrincipalName   = $CloudMailboxObj.UserPrincipalName
            primarySmtpAddress  = $CloudMailboxObj.PrimarySmtpAddress
            name                = $CloudMailboxObj.Name
            emailAddress        = $CloudMailboxObj.WindowsEmailAddress
            displayName         = $CloudMailboxObj.DisplayName
            resourceCapacity    = $CloudMailboxObj.ResourceCapacity
            restrictionType     = $restricted
            equipmentType       = ""
            bookingProcessing   = $bookingProcessing
            city                = $ContactInfo.City
            company             = $ContactInfo.Company
            countryOrRegion     = $ContactInfo.CountryOrRegion
            department          = $ContactInfo.Department
            floor               = ""
            office              = $ContactInfo.Office
            phone               = $ContactInfo.Phone
            postalCode          = $ContactInfo.PostalCode
            stateOrProvince     = $ContactInfo.StateOrProvince
            dependencies        = @()
            restrictedDelegates = $delegates
        }
        return $jObject
    }  
}

function Get-EPAExchangeMailbox
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$upn
    )
    BEGIN
    {
        $uobj = $null
    }
    PROCESS
    {
        try
        {
            $uobj = Get-Mailbox -Identity $upn
        } catch
        {
            $uobj = $null
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Retreive {0} message {1}" -f $upn, $error[0]) -EmitWarning
        }
    }
    END
    {
        return $uobj
    }
}

function Get-EPAExchangeMailboxes
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Room", "Equipment")]
        [string]$MailboxType,
        [Parameter(Mandatory = $false)]
        [string]$MailboxFilter
    )
    BEGIN
    {
        $mailboxes = @()
    }
    PROCESS
    {
        # Mailbox [Room]
        if ($MailboxType -eq "Room")
        {
            if ($null -eq $MailboxFilter -or $MailboxFilter.Length -le 0)
            {
                $mailboxes = Get-Mailbox -ResultSize unlimited -RecipientTypeDetails roommailbox
            } else
            {
                $mailboxes = Get-Mailbox -ResultSize unlimited -RecipientTypeDetails roommailbox -Filter "(EmailAddresses -like '*$MailboxFilter*' -or DisplayName -like '*$MailboxFilter*')"
            }
        }
        # Mailbox [Equipment]
        if ($MailboxType -eq "Equipment")
        {
            if ($null -eq $MailboxFilter -or $MailboxFilter.Length -le 0)
            {
                $mailboxes = Get-Mailbox -ResultSize unlimited -RecipientTypeDetails equipment
            } else
            {
                $mailboxes = Get-Mailbox -ResultSize unlimited -RecipientTypeDetails equipment -Filter "(EmailAddresses -like '*$MailboxFilter*' -or DisplayName -like '*$MailboxFilter*')"
            }
        }
    }
    END
    {
        return $mailboxes
    }
}

function Get-EPAExchangeMailboxBookingResource
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    [OutputType([JsonRestrictedDelegates])]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true)]
        [string]$upn
    )
    BEGIN
    {
        $resourceobj = $null
    }
    PROCESS
    {
        try
        {
            $uobj = $null
            $uobj = Get-User -Identity $upn -ErrorAction Stop
            [JsonRestrictedDelegates]$resourceobj = [pscustomobject]@{
                userPrincipalName = $uobj.WindowsEmailAddress
                userType          = "User"
            }
        } catch
        {
            $resourceobj = $null
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("ERROR: Failed to retrieve user {0}" -f $upn) -EmitWarning
            try
            {
                $uobj = $null
                $uobj = Get-DistributionGroup -Identity $upn -ErrorAction Stop
                $resourceobj = [pscustomobject]@{
                    userPrincipalName = $uobj.WindowsEmailAddress
                    userType          = "Distribution"
                }
            } catch
            {
                $resourceobj = $null
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("ERROR: Failed to retrieve distribution group {0}" -f $upn) -EmitWarning
            }
        }
    }
    END
    {
        return $resourceobj
    }
}

function Set-EPAExchangeMailboxBooking
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true)]
        [string]$UserPrincipalName,

        [Parameter(Mandatory = $false)]
        [ValidateSet("None", "ApprovalRequired", "Restricted")]
        [string]$BookingRestriction = "None",

        [Parameter(Mandatory = $false)]
        [JsonRestrictedDelegates[]]$RestrictedDelegates
    )
    BEGIN
    {
        # Collection of delgates
        $retry = 0
        $maxRetry = 5
    }
    PROCESS
    {

        do
        {
            $errBooking = $null
            $jmailboxDelegates = @()
            if ($?)
            {
                $Error.Clear()
            }
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Setting Booking details {0} IDX:{1} after pausing 3 seconds...." -f $UserPrincipalName, $retry) -Verbose:$VerbosePreference
            Start-Sleep -Seconds 3

            if ($BookingRestriction -eq "None")
            {
                Set-CalendarProcessing -Identity $UserPrincipalName -AutomateProcessing AutoAccept -DeleteComments $true -AddOrganizerToSubject $true -AllowConflicts $false -ErrorAction SilentlyContinue -ErrorVariable $errBooking
            }

            if ($BookingRestriction -eq "ApprovalRequired")
            {

                foreach ($delegate in $RestrictedDelegates)
                {
                    $jmailboxDelegates += $delegate.userPrincipalName
                }
                Set-CalendarProcessing -Identity $UserPrincipalName -AutomateProcessing AutoAccept -AllRequestInPolicy $true -AllBookInPolicy $false -ResourceDelegates $jmailboxDelegates -ErrorAction SilentlyContinue -ErrorVariable $errBooking
            }

            if ($BookingRestriction -eq "Restricted")
            {

                foreach ($delegate in $RestrictedDelegates)
                {
                    $jmailboxDelegates += $delegate.userPrincipalName
                }
                Set-CalendarProcessing -Identity $UserPrincipalName -AllBookInPolicy $false -BookInPolicy $jmailboxDelegates -ErrorAction SilentlyContinue -ErrorVariable $errBooking
            }

            $err = $Error[0]
            if ($null -eq $err)
            {
                $retry = $maxRetry
            } elseif ($err.CategoryInfo.Reason -ne "MailboxUnavailableException")
            {
                $retry = $maxRetry
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Mailbox {0} Exception isn't in state to retry" -f $UserPrincipalName) -EmitWarning
            } else
            {
                $retry += 1
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Mailbox {0} Failed {1}" -f $UserPrincipalName, $err.Exception) -EmitWarning
            }
        }
        until($Error.Count -eq 0 -or $retry -ge $maxRetry)
    }
    END
    {

    }
}

function Rename-ExchangeMailboxes
{
    <#
    .SYNOPSIS
    Renames Exchange Online mailbox Alias
    
    .DESCRIPTION
    Will set EXO new names based on the specified CSV files
    - Connect to PSSession and MSOL; Retreive App based on $AppName; Add or Update App with Provided list
    
    .PARAMETER TenantID
    The unique Azure AD domain name
    
    .PARAMETER RenamedMailboxesCsv
    Full path to CSV file containing mailbxoes that need to be renamed
    
    .PARAMETER CmdLetLogFile
    The full path to the log file to which the log will write logs
    
    .EXAMPLE
    Rename-ExchangeMailboxes -CmdLetLogFile $CmdLetLogFile -TenantID $TenantID -RenamedMailboxesCsv $RenamedMailboxesCsv
    
    .NOTES
    MSOnline and AzureAD modules must be installed and available for this to run.
    #>
    [cmdletbinding()]
    PARAM(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true, HelpMessage = "shawniq.onmicrosoft.com")]
        [string]$TenantID,

        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [Parameter(Mandatory = $true)]
        [string]$RenamedMailboxesCsv
    )
    BEGIN
    {
        if ($?)
        {
            $Error.Clear()
        }
    }
    PROCESS
    {
        # Read in Mailboxes to be Renamed
        $changes = Import-Csv -Path $RenamedMailboxesCsv
        foreach ($mailboxChange in $changes)
        {

            $newname = $mailboxChange.NewName
            $oldname = $mailboxChange.OldName
            $alias = $null

            # validate incoming data set
            if (($newname -match '\@') -eq $true)
            {
                $alias = $newname.Split("{@}")[0]
                $newname = $alias
            } else
            {
                $alias = $newname -replace '[^a-zA-Z0-9]', ''
                $newname = $alias
            }

            $oldupn = Get-MsolUser -SearchString $oldname | Select-Object userprincipalname
            $oldemailaddress = $oldupn.UserPrincipalName

            if ($null -eq $oldupn)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("No UPN found for {0}" -f $oldname) -EmitWarning
            } else
            {

                $MailboxPrincipalName = ("{0}@{1}" -f $alias, $TenantID)
                $WindowsEmailAddress = ("{0}@{1}" -f $alias, $TenantID)
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Changing UPN from {0} to {1}" -f $oldname, $WindowsEmailAddress)  -Verbose:$VerbosePreference
                Set-MsolUserPrincipalName -UserPrincipalName $oldemailaddress -NewUserPrincipalName $WindowsEmailAddress -Verbose:$VerbosePreference -ErrorVariable changeNameError
                if ($changeNameError.Count -gt 0)
                {
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("ERROR: Failed to change the UPN with msg {0}" -f $changeNameError) -EmitError
                } else
                {
                    # due to replication this process might take a few minutes; lets pause until we find it
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Validating UPN for {0} and {1}" -f $MailboxPrincipalName, $WindowsEmailAddress)  -Verbose:$VerbosePreference
                    Wait-EPAExchangeMailboxCreated -CmdLetLogFile $CmdLetLogFile -UserPrincipalName $WindowsEmailAddress -Verbose:$VerbosePreference

                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Setting-Mailbox UPN from {0} to {1}" -f $oldemailaddress, $WindowsEmailAddress)  -Verbose:$VerbosePreference
                    Set-Mailbox -Id $oldemailaddress -Name $newname -DisplayName $newname -Alias $alias -WindowsEmailAddress $WindowsEmailAddress -EmailAddresses $WindowsEmailAddress -Verbose:$VerbosePreference
                }
            }
        }
    }
}

function Import-EPAAddIn
{
    <#
    .SYNOPSIS
    Add the Add-In to Exchange and isolate to individual users
    
    .DESCRIPTION
    Will Add the RoomFinder Add-In; Set the Add-In to SpecificUsers
    
    .PARAMETER AppManifestXml
    The full path to the App Manifest XML file
    
    .PARAMETER AppName
    The name of the App to be deployed into Office 365 Exchange Online
    
    .PARAMETER AppRestrictionGroup
    The name of an O365 Group or AD Distribution group containing members to which the Add-In will be restricted
    
    .PARAMETER O365Group
    Switch indicating the [AppRestrictionGroup] is an O365 Group
    
    .PARAMETER CmdLetLogFile
    The full path to the file to which logs/events will be written
    
    .EXAMPLE
    Import-EPAAddIn -AppManifestXml $appmanifestxml -AppName $RoomFinderAddInName -AppRestrictionGroup $RoomFinderDistroGroup `
            -O365Group:$o365group -CmdLetLogFile $CmdLetLogFile -Verbose:$VerbosePreference
    
    .NOTES
    Requires an active connection to PSSession cmdlets for Exchange
    #>
    [cmdletbinding()]
    PARAM(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [Parameter(Mandatory = $true)]
        [string]$AppManifestXml,
    
        [Parameter(Mandatory = $true, HelpMessage = "Room Availability - Dev")]
        [string]$AppName,
        
        [Parameter(Mandatory = $false, HelpMessage = "roomfinderaddingroup@shawniq.onmicrosoft.com")]
        [string]$AppRestrictionGroup,
    
        [Parameter(Mandatory = $false, HelpMessage = "Is this an O365 group or another Azure AD sync'd group.")]
        [Switch]$O365Group
    )
    BEGIN
    {
        if ($?)
        {
            $Error.Clear()
        }     
    }
    PROCESS
    {
    
        # Read distro membership from EXO
        $user_list = @()
        if ($null -ne $AppRestrictionGroup -and $AppRestrictionGroup -ne "")
        {
            Write-Verbose ("Constructing array of specific users")
    
            if ($O365Group -eq $true)
            {
                $group = Get-UnifiedGroup -Identity $AppRestrictionGroup
                $members = Get-UnifiedGroupLinks -LinkType Members -Identity $AppRestrictionGroup
                $user_list = Get-EPACollectionOfUsers -CmdLetLogFile $CmdLetLogFile -group $group -users $members -Verbose:$VerbosePreference
                    
                $owners = Get-UnifiedGroupLinks -LinkType Owners -Identity $AppRestrictionGroup
                $owners_list = Get-EPACollectionOfUsers -CmdLetLogFile $CmdLetLogFile -group $group -users $owners -Verbose:$VerbosePreference
            } else
            {
                $group = Get-DistributionGroup -Identity $AppRestrictionGroup
                $members = Get-DistributionGroupMember -Identity $AppRestrictionGroup
                $user_list = Get-EPACollectionOfUsers -CmdLetLogFile $CmdLetLogFile -group $group -users $members -Verbose:$VerbosePreference
            }
        }
    
        # Check for the Add-In
        $roomFinderApp = Get-App -OrganizationApp | Where-Object { $_.DisplayName -eq $AppName }
        if ($null -eq $roomFinderApp)
        {
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("App {0} not found in Organization Add-In Catalog... Now adding" -f $AppName) -EmitWarning
    
            $AppManifestData = Get-Content -Path $AppManifestXml -Encoding Byte -ReadCount 0
    
            if ($user_list.Count -gt 0)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("App {0} not found in Organization Add-In Catalog... adding to enabled for SpecificUsers" -f $AppName)  -Verbose:$VerbosePreference
                New-App -OrganizationApp -FileData $AppManifestData -ProvidedTo SpecificUsers -UserList $user_list -DefaultStateForUser Enabled 
            } else
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("App {0} not found in Organization Add-In Catalog... adding to enabled for everyone" -f $AppName)  -Verbose:$VerbosePreference
                New-App -OrganizationApp -FileData $AppManifestData -DefaultStateForUser Enabled 
            }
        } else
        {
    
            if ($user_list.Count -gt 0)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("App {0} found in Organization Add-In Catalog... Now updating to enabled for SpecificUsers" -f $AppName)  -Verbose:$VerbosePreference
                Set-App -OrganizationApp -Identity $roomFinderApp.Identity -ProvidedTo SpecificUsers -UserList $user_list -Enabled $true
            } else
            {
                # Just in case (do we Enable here)
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("App {0} found in Organization Add-In Catalog... Now updating to enabled for Everyone" -f $AppName)  -Verbose:$VerbosePreference
                Set-App -OrganizationApp -Identity $roomFinderApp.Identity -ProvidedTo Everyone -Enabled $true
            }
        }
    }  
}

function Receive-ExchangeResources
{
    <#
    .SYNOPSIS
    Generate JSON for mailboxes
    
    .DESCRIPTION
    Generate JSON for mailboxes
    
    .PARAMETER TenantID
    The unique name of the Azure AD Domain name
    
    .PARAMETER MailboxFilter
    The string filter for querying Exchange Online

    .PARAMETER CmdLetLogFile
    The full path to which logs/events will be written
    
    .EXAMPLE
    Receive-ExchangeResources -TenantID $TenantID -MailboxFilter "CIN" -CmdLetLogFile $CmdLetLogFile
    
    .NOTES
    An active connection to PSSession in the current runspace must be enabled and activated
    #>
    [cmdletbinding()]
    [OutputType([JsonRoomFinder])]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true, HelpMessage = "shawniq.onmicrosoft.com")]
        [string]$TenantID,
    
        [Parameter(Mandatory = $false)]
        [string]$MailboxFilter = "CIN"
    )
    BEGIN
    {
        if ($?)
        {
            $Error.Clear()
        }
    }
    PROCESS
    {
        [JsonRoomFinder] $jsonObjects = New-Object JsonRoomFinder;

        # Pull all mailboxes
        $mailboxes = Get-EPAExchangeMailboxes -MailboxType "Room" -MailboxFilter $MailboxFilter
        foreach ($CloudMailboxObj in $mailboxes | Sort-Object -Property DisplayName -CaseSensitive:$false)
        {
            [JsonConfigMailbox] $jsonObject = $null
            $jsonObject = Get-EPAMailboxProperties -CmdLetLogFile $CmdLetLogFile -CloudMailboxObj $CloudMailboxObj
            $jsonObjects.mailboxes += $jsonObject
        }
    
        $equipments = Get-EPAExchangeMailboxes -MailboxType "Equipment" -MailboxFilter $MailboxFilter
        foreach ($CloudMailboxObj in $equipments | Sort-Object -Property DisplayName -CaseSensitive:$false)
        {
            [JsonConfigEquipment] $jsonObject = $null
            $jsonObject = Get-EPAEquipmentProperties -CmdLetLogFile $CmdLetLogFile -CloudMailboxObj $CloudMailboxObj
            $jsonObjects.equipments += $jsonObject
        }
    
        Write-Output $jsonObjects
    }
}

function Find-EPAUnifiedGroup
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $groupName
    )
    PROCESS
    {
        $returnValue = $true
        try
        {
            Write-Host "Checking group"  $groupName
            $tmpGroup = Get-UnifiedGroup -Identity $groupName
            if (!$tmpGroup)
            {
                $returnValue = $false
            }
        } catch
        {
            $returnValue = $false
        }
        return $returnValue
    }
}

function Test-EPAGetUnifiedGroup
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $groupEmail
    )
    PROCESS
    {
        Write-Host "Getting group: " + $groupEmail
        $returnGroupValue = $null
        try
        {
            $returnGroupValue = Get-UnifiedGroup -Identity $groupEmail
        } catch
        {
            $returnGroupValue = $null
            Write-Host "Failed to get group: " + $groupEmail
        }
        return  $returnGroupValue
    }
}

function Wait-EPAUnifiedGroupExists
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $groupName,

        [Parameter(Mandatory = $false)]
        [Int]$maxRetries = 20
    )
    PROCESS
    {
        #todo: add counter to kill script after x tries..
        $check = $false

        Do
        {
            Write-Verbose ("Checking ... {0} retry left {1}" -f $groupName, $maxRetries)
            $check = Find-EPAUnifiedGroup -groupName $groupName
            Start-Sleep -s 2
            $maxRetries--
            if ($maxRetries -le 0)
            {
                break
            }
        }
        While ($check -eq $false)

        return $check
    }
}

function Test-EPAMailboxExists
{
    <#
	.SUMMARY
      Function: Check if mailbox with the same email address exists
      #>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $mailBox
    )
    PROCESS
    {
        $returnValue = $true

        try
        {
            Write-Host "Checking mailbox... "  $mailBox
            $tmpmailBox = Get-Mailbox -Identity $mailBox
            if (!$tmpmailBox)
            {
                $returnValue = $false
            }
        } catch
        {
            $returnValue = $false
            Write-Host "Failed in checking mailbox... "  $mailBox
        }

        Write-Host $returnValue
        return $returnValue
    }
}

function Test-EPADistributionGroupExists
{
    <#
	.SUMMARY
       Function: Check if mailbox with the same email address exists
       #>
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $mailBox
    )
    PROCESS
    {
        $returnValue = $true
        try
        {
            Write-Host "Checking DL... "  $mailBox
            $tmpDL = Get-DistributionGroup -Identity $mailBox
            if (!$tmpDL)
            {
                $returnValue = $false
            }
        } catch
        {
            $returnValue = $false
            Write-Host "Failed in checking DL... "  $mailBox
        }
        return $returnValue
    }
}

function Enable-EPAImpersonation
{
    <#
    .SYNOPSIS
    Ensure the svc account has proper impersonation authorization 
    
    .DESCRIPTION
    Will create the new impersonation role along with applying it to the appropriate account
    
    .PARAMETER roomacct
    The UPN of the Azure AD Object which will be enabled for impersonation
    
    .PARAMETER CmdLetLogFile
    The full path to the log file to which logs/events will be written
    
    .EXAMPLE
    Enable-EPAImpersonation -roomacct $roomacct -CmdLetLogFile $CmdLetLogFile
    
    .NOTES
    Must have an active connection in which PSSession has been imported into the current runspace
    #>
    [cmdletbinding()]
    param(
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $false, HelpMessage = "The service account which needs impersonation")]
        [string]$roomacct = "roomwizard1.mcs@testusepa.onmicrosoft.com"
    )
    BEGIN
    {
        if ($null -ne $Error)
        {
            $Error.Clear()
        }        
    }
    PROCESS
    {    
        # Create the management scope
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Newup {0} scope with restriction" -f $roomacct) -Verbose:$VerbosePreference
        New-ManagementScope -Name "ResourceMailboxes" -RecipientRestrictionFilter { RecipientTypeDetails -eq "RoomMailbox" -or RecipientTypeDetails -eq "EquipmentMailbox" }
    
        # Create the Role assignment
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Newup {0} role assignment for impersonation" -f $roomacct) -Verbose:$VerbosePreference
        New-ManagementRoleAssignment -Name "ResourceImpersonation" -User $roomacct -CustomRecipientWriteScope "ResourceMailboxes" -Role ApplicationImpersonation -User $roomacct -CustomRecipientWriteScope "ResourceMailboxes"
        New-ManagementRoleAssignment -Name "ResourceImpersonation" -User $roomacct -CustomRecipientWriteScope "ResourceMailboxes" -Role "Mail Recipients"
        New-ManagementRoleAssignment -Name "ResourceImpersonation" -User $roomacct -CustomRecipientWriteScope "ResourceMailboxes" -Role "MeetingGraphApplication"
        New-ManagementRoleAssignment -Name "ResourceImpersonation" -User $roomacct -CustomRecipientWriteScope "ResourceMailboxes" -Role "Team Mailboxes"
    }
}

function Sync-EPAExchangeProvisioning
{
    <#
    .SYNOPSIS
        Provisioning [Rooms] || [Equipment] in Exchange
    .DESCRIPTION
        Load Room JSON into memory, Retreive Mailboxes by the provisioning type, Enumerate JSON
        Provision rooms/equipment if they do not exist, Add rooms/equipment into allocated resources
    
    .PARAMETER TenantID
    The Tenant name or unique identifier in Azure AD
    
    .PARAMETER MailboxType
    Resource: Room or Equipment
    
    .PARAMETER MailboxFilter
    The string prefix for the mailbox resources
    
    .PARAMETER CountryCode
    The Country code for which the object will be bound
    
    .PARAMETER CountryOrRegion
    The Country or Region for which the object will be bound
    
    .PARAMETER Company
    The Company or owner of the Tenant and these resources
    
    .PARAMETER RoomResourceJSONFile
    A full path to a JSON file from which we will sync Exchange objects
    
    .PARAMETER CmdLetLogFile
    A full path to a log file to which log entries will be asserted.
    
    .EXAMPLE
    Sync-EPAExchangeProvisioning -TenantID $TenantID -RoomResourceJSONFile ".\objects.json" -MailboxType Room -MailboxFilter "CIN"
    
    .NOTES
    The assumption that whomever is running this script has appropriate EXO permissions and is authorized to run these commands.
    #>
    [cmdletbinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    PARAM (
        [Parameter(Mandatory = $true, HelpMessage = "Provide the literal path to the log file")]
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [string]$CmdLetLogFile,
        
        [Parameter(Mandatory = $true, HelpMessage = "shawniq.onmicrosoft.com")]
        [string]$TenantID,
    
        [Parameter(Mandatory = $true)]
        [ValidateSet("Room", "Equipment")]
        [string]$MailboxType,
    
        [Parameter(Mandatory = $false)]
        [string]$MailboxFilter,
    
        [Parameter(Mandatory = $false)]
        [string]$CountryCode = "US",
        
        [Parameter(Mandatory = $false)]
        [string]$CountryOrRegion = "United States",
        
        [Parameter(Mandatory = $false)]
        [string]$Company = "EPA",
    
        [ValidateScript( { Test-Path $_ -PathType Leaf })]
        [Parameter(Mandatory = $true, HelpMessage = "A JSON file containing the collection of Equipment,EquipmentTypes,Locations")]
        [string]$RoomResourceJSONFile
    )
    BEGIN
    {
        if ($?)
        {
            $Error.Clear()
        }
    }
    PROCESS
    {

        # Process the JSON
        $jsonResources = $null
        $jsonResources = Get-Content -LiteralPath $RoomResourceJSONFile -Raw | ConvertFrom-Json
    
    
        $jsonRooms = $null
        $jsonRooms = $jsonResources.mailboxes
        if ($MailboxType -eq "Equipment")
        {
            $jsonRooms = $jsonResources.equipments
        }
        $jsonRoomsCount = ($jsonRooms | Measure-Object).Count
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  ("JSON File has {0} items" -f $jsonRoomsCount)  -Verbose:$VerbosePreference
    
        # Mailboxes [Pull from Exchange for Mapping]
        $mailboxes = Get-EPAExchangeMailboxes -MailboxType $MailboxType -MailboxFilter $MailboxFilter
        $mailboxesCount = ($mailboxes | Measure-Object).Count
        Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  ("Exchange mailboxes of type [{0}] and filter [{1}] has {2} items" -f $MailboxType, $MailboxFilter, $mailboxesCount)  -Verbose:$VerbosePreference
    
        # if exchange and the JSON file have values
        if ($jsonroomscount -le 0)
        {
            # Switch is set - Exchange object should be controlled by a ticket
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  "WARN: --------"
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  ("WARN: No [{0}] objects found in JSON file {1}" -f $MailboxType, $RoomResourceJSONFile) -EmitWarning
            Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  "WARN: --------"
        } else
        {
            Write-Verbose -Message "Processing JSON Rooms files"
            $jsonpropchange = $false
    
            for ($idx = 0; $idx -lt $jsonRoomsCount; $idx++)
            {
                $jsonMailbox = $jsonRooms[$idx]

                # TODO: use HASH compare
                $MailboxObj = Get-EPAUserPrincipalName -windowsEmailAddress $jsonMailbox.userPrincipalName -TenantID $TenantID
                $MailboxAlias = $MailboxObj.UniqueName;
    
                $PrimarySmtpAddress = $jsonMailbox.primarySmtpAddress
                $UserPrincipalName = $jsonMailbox.userPrincipalName
                $RestrictedDelegates = $jsonMailbox.restrictedDelegates
    
                # found a match between the JSON and Exchange
                $CloudMailboxObj = $null
                $CloudMailboxObj = $mailboxes | Where-Object { $_.UserPrincipalName -eq $UserPrincipalName }
                if ($null -eq $CloudMailboxObj)
                {
    
                    # Switch is set - create Exchange object
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg  ("Adding Name {0} w/ UPN {1} to Exchange" -f $jsonMailbox.name, $UserPrincipalName)  -Verbose:$VerbosePreference
                    $CloudMailboxObj = New-EPAExchangeMailbox -CmdLetLogFile $CmdLetLogFile -UserPrincipalName $UserPrincipalName -Name $jsonMailbox.name `
                        -DisplayName $jsonMailbox.displayName -MailboxAlias $MailboxAlias -PrimarySmtpAddress $PrimarySmtpAddress `
                        -MailboxType $MailboxType -ResourceCapacity $jsonMailbox.resourceCapacity -Verbose:$VerbosePreference
    
                    # Variables
                    $SamAccountName = $CloudMailboxObj.SamAccountName
                    $exchangeGuid = $CloudMailboxObj.ExchangeGuid       # Unique => Get-Mailbox -Identity $_
                    $uprincipal = $CloudMailboxObj.UserPrincipalName
    
                    # This should be populated by the time we get to this check and this is an Azure AD Object
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Mailbox created {0}" -f $uprincipal) -EmitWarning
    
                    # Retreive the booking information
                    $restricted = $jsonMailbox.restrictionType
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("UPN {0} has restricted:[{1}]" -f $UserPrincipalName, $restricted) -Verbose:$VerbosePreference
    
                    # Update the processing details
                    if (($RestrictedDelegates | Measure-Object).Count -gt 0)
                    {
                        Set-EPAExchangeMailboxBooking -CmdLetLogFile $CmdLetLogFile -UserPrincipalName $UserPrincipalName `
                            -BookingRestriction $restricted `
                            -RestrictedDelegates $RestrictedDelegates -Verbose:$VerbosePreference
                    }
                } else
                {
                    # Variables
                    $SamAccountName = $CloudMailboxObj.SamAccountName
                    $exchangeGuid = $CloudMailboxObj.ExchangeGuid       # Unique => Get-Mailbox -Identity $_
                    $uprincipal = $CloudMailboxObj.UserPrincipalName
                }
                    
                # Check Exchange and Update JSON properties do not matches
                $mailboxproperties = @{ }
                $mailboxpropchange = $false
    
                if ($null -eq $CloudMailboxObj.CustomAttribute8 -or $CloudMailboxObj.CustomAttribute8 -eq "" -or $CloudMailboxObj.CustomAttribute8 -ne $UserPrincipalName)
                {
                    # Update Exchange resource with the custom attribute setting
                    $mailboxproperties.add('CustomAttribute8', $UserPrincipalName)
                    $mailboxpropchange = $true
                }
    
                if ($null -eq $CloudMailboxObj.CustomAttribute9 -or $CloudMailboxObj.CustomAttribute9 -eq "" -or $CloudMailboxObj.CustomAttribute9 -ne $SamAccountName)
                {
                    # Update the Exchange Attribute
                    $mailboxproperties.add('CustomAttribute9', $SamAccountName)
                    $mailboxpropchange = $true
                }
    
                if ($null -eq $CloudMailboxObj.ResourceCapacity -or $CloudMailboxObj.ResourceCapacity -eq "" -or $CloudMailboxObj.ResourceCapacity -ne $jsonMailbox.resourceCapacity)
                {
                    # Update Exchange resource with resource capacity setting
                    $mailboxproperties.add('ResourceCapacity', $jsonMailbox.resourceCapacity)
                    $mailboxpropchange = $true
                }

                # Update Mailbox properties
                if ($mailboxpropchange -eq $true)
                {
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Updating Mailbox properties for {0}" -f $UserPrincipalName)  -Verbose:$VerbosePreference
                    Set-Mailbox -Identity $UserPrincipalName @mailboxproperties
                }
    
    
                $contactInfo = Get-User -Identity $CloudMailboxObj.Alias
                $contactpropchange = $false
                $contactprops = @{
                    Company = $Company;
                }
                if ($null -eq $contactInfo.City -or $contactInfo.City -eq "" -or $contactInfo.City -ne $jsonMailbox.city)
                {
                    $contactprops.Add("City", $jsonMailbox.city) #City
                    $contactpropchange = $true
                }
                if ($null -eq $contactInfo.CountryOrRegion -or $contactInfo.CountryOrRegion -eq "" -or $contactInfo.CountryOrRegion -ne $CountryOrRegion)
                {
                    $contactprops.Add("CountryOrRegion", $CountryOrRegion) #Country
                    $contactpropchange = $true
                }
                if ($null -eq $contactInfo.Department -or $contactInfo.Department -eq "" -or $contactInfo.Department -ne $jsonMailbox.department)
                {
                    $contactprops.Add("Department", $jsonMailbox.department) #Department
                    $contactpropchange = $true
                }
                if ($null -eq $contactInfo.Office -or $contactInfo.Office -eq "" -or $contactInfo.Office -ne $jsonMailbox.office)
                {
                    $contactprops.Add("Office", $jsonMailbox.office) #location
                    $contactpropchange = $true
                }
                if ($null -eq $contactInfo.Phone -or $contactInfo.Phone -eq "" -or $contactInfo.Phone -ne $jsonMailbox.phone)
                {
                    $contactprops.Add("Phone", $jsonMailbox.phone) #Phone
                    $contactpropchange = $true
                }
                if ($null -eq $contactInfo.PostalCode -or $contactInfo.PostalCode -eq "" -or $contactInfo.PostalCode -ne $jsonMailbox.postalCode)
                {
                    $contactprops.Add("PostalCode", $jsonMailbox.postalCode) #PostOffice
                    $contactpropchange = $true
                }
                if ($null -eq $contactInfo.StateOrProvince -or $contactInfo.StateOrProvince -eq "" -or $contactInfo.StateOrProvince -ne $jsonMailbox.stateOrProvince)
                {
                    $contactprops.Add("StateOrProvince", $jsonMailbox.stateOrProvince) #State
                    $contactpropchange = $true
                }
    
                # Update contact information properties
                if ($contactpropchange -eq $true)
                {
                    Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Updating Contact Info for {0}" -f $UserPrincipalName) -Verbose:$VerbosePreference
                    Set-User -Identity $UserPrincipalName @contactprops
                }
    

                # Need to update JSON File with properties from Exchange
                if ($null -eq $jsonMailbox.samGUID)
                {
                    $jsonpropchange = $true
                    $jsonMailbox | Add-Member NoteProperty -Name "samGUID" -Value $exchangeGuid
                }
                if ($null -eq $jsonMailbox.exchangeObjectId)
                {
                    $jsonpropchange = $true
                    $jsonMailbox | Add-Member NoteProperty -Name "exchangeObjectId" -Value $CloudMailboxObj.ExchangeObjectId
                }
                if ($null -eq $jsonMailbox.guid)
                {
                    $jsonpropchange = $true
                    $jsonMailbox | Add-Member NoteProperty -Name "guid" -Value $CloudMailboxObj.Guid
                }
                if ($null -eq $jsonMailbox.samAccountName)
                {
                    $jsonpropchange = $true
                    $jsonMailbox | Add-Member NoteProperty -Name "samAccountName" -Value $SamAccountName
                } elseif ($null -ne $jsonMailbox.samAccountName -and $jsonMailbox.samAccountName -ne $SamAccountName)
                {
                    $jsonpropchange = $true
                    $jsonMailbox.samAccountName = $SamAccountName
                }
            }
    
            if ($jsonpropchange -eq $true)
            {
                Write-EPALogMessage -LogFile $CmdLetLogFile -Msg ("Now writing changes to {0}" -f $RoomResourceJSONFile) -EmitWarning
                $jsonResources | ConvertTo-Json -Depth 5 | Out-File $RoomResourceJSONFile -Force
            }
        }
    }
}

Function Write-EPALogMessage
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $true)]
        [string]$LogFile,

        [Parameter(Mandatory = $true)]
        [string]$Msg,

        [Parameter(Mandatory = $false)]
        [switch]$EmitWarning,

        [Parameter(Mandatory = $false)]
        [switch]$EmitError
    )
    PROCESS
    {


        $now = Get-Date -format "dd-MM-yyyy HH:mm:ss:ms"
        $logmsg = ("{0} - {1}" -f $now, $Msg)
        if (!(Test-Path -PathType Leaf -Path $LogFile))
        {
            "" | Out-file -FilePath $LogFile -Force
        }

        if ($EmitWarning)
        {
            Write-Warning -Message $logmsg
        }
        if ($EmitError)
        {
            Write-Error -Message $logmsg
        }
        Write-Verbose -Message $logmsg
        Add-Content $LogFile $logmsg
    }
}

function Disconnect-EPAExchangeOnline
{
    [cmdletbinding(HelpUri = "https://github.com/USEPA/SharePoint-Github-Repo/tree/master/powershellApps/posh/readme.md")]
    param(
        [Parameter(Mandatory = $false)]
        $session
    )
    begin
    {
    }
    process
    {
        if ($null -eq $session)
        {
            $session = Get-PSSession
        }

        if ($null -ne $session -and $session.State -eq "Opened")
        {
            Remove-PSSession -Session @($session)
        }
    }
    end
    {

    }
}

# export all functions from within this module
Export-ModuleMember -Function *