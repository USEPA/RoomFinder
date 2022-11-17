
Connect-MsolService

$apps = Get-MsolServicePrincipal -All | Where-Object { $_.DisplayName -like '*pnp*' }
$apps | Select-Object { $_.ServicePrincipalNames }
$appPrincipal = Get-MsolServicePrincipal -ServicePrincipalName "cb6fd5c8-0412-4740-9a0f-15909d611443"
Remove-MsolServicePrincipal -ObjectId $appPrincipal.ObjectId



Connect-PnPOnline –url "https://usepa.sharepoint.com/sites/dev" -Credentials "spepaonline"
Get-PnPContext
Get-PnPApp
Get-PnPAppInstance




$clientId = "e3107717-7ed8-4fcb-86dc-afae817aac81" # Site Provisioning
$clientId = "2733f9e1-44ff-4bac-972a-5250f0f70c07" # Governance and Features - Provider Hosted Add-In
$clientId = "ade159e6-d94e-4227-8774-044ee5dd8e41" # EZ Forms
Connect-MsolService
$principals = Get-MsolServicePrincipal -all | where-object { $_.AppPrincipalId -eq $clientId }
$principals | foreach-object {
    Get-MsolServicePrincipalCredential -AppPrincipalId $_.appprincipalid -ReturnKeyValues $false
} | Format-Table KeyId, StartDate, EndDate, Type, Usage



$bytes = New-Object Byte[] 32
$rand = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rand.GetBytes($bytes)
$rand.Dispose()
$newClientSecret = [System.Convert]::ToBase64String($bytes)

$dtStart = ([System.DateTime]::Now).AddDays(-1)
$dtEnd = $dtStart.AddYears(1)
New-MsolServicePrincipalCredential -AppPrincipalId $clientId -Type Symmetric -Usage Sign -Value $newClientSecret -StartDate $dtStart  -EndDate $dtEnd
New-MsolServicePrincipalCredential -AppPrincipalId $clientId -Type Symmetric -Usage Verify -Value $newClientSecret   -StartDate $dtStart  -EndDate $dtEnd
New-MsolServicePrincipalCredential -AppPrincipalId $clientId -Type Password -Usage Verify -Value $newClientSecret   -StartDate $dtStart  -EndDate $dtEnd
$newClientSecret