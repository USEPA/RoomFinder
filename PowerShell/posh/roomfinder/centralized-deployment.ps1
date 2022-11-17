<#

  Deploy Add-In through Centralized Deployment

#>

$creds = Get-Credential -UserName "sharepointadmin@usepa.onmicrosoft.com"
Connect-OrganizationAddInService -Credential $creds
$items = Get-ChildItem -Path .\Microsoft.Services.OutlookRoomFinder-cin-poc.xml
New-OrganizationAddIn -ManifestPath $items.FullName -Locale 'en-US' -Members 'leonard.shawn@epa.gov', 'stitt.brian@epa.gov', 'osborn.amy@epa.gov', 'sharepointadmin@usepa.onmicrosoft.com'