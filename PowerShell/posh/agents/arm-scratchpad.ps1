$location = "usgovvirginia"
$azureTenantId = "8a09f2d7-8415-4296-92b2-80bb4666c5fc"
$azureAplicationId = Get-ChildItem -Path Env:\ARMCLIENTID
$azureAppSecret = Get-ChildItem -Path Env:\ARMCLIENTSECRET
$azurePassword = ConvertTo-SecureString $azureAppSecret.Value -AsPlainText -Force
$psCred = New-Object System.Management.Automation.PSCredential($azureAplicationId.Value, $azurePassword)
Connect-AzAccount -Environment AzureUSGovernment -Credential $psCred -ServicePrincipal -TenantId $azureTenantId | Out-Null
Select-AzSubscription -Subscription "spl-mag" | Out-Null


Get-AzADServicePrincipal -DisplayNameBeginsWith "CoolBridge"
Get-AzADServicePrincipal -DisplayNameBeginsWith "Fairfax"
Get-AzADServicePrincipal -DisplayNameBeginsWith "Microsoft Azure App Service"
Get-AzADGroup -DisplayNameStartsWith "epa-Developers"


$rgName = "armcontoso-epa" 
$group = Get-AzResourceGroup -Name $rgName
if ($null -eq $group) {
  New-AzResourceGroup -Name $rgName -Location $location -Verbose:$VerbosePreference
}

$templateParams = @{
  environmentType = "jahunte"
  webEnvironment  = "dev-jahunte"
  customer        = "epa"
}

$test = Test-AzResourceGroupDeployment -ResourceGroupName $rgName `
  -TemplateFile .\AzureTemplates\roomfinder.template.json -TemplateParameterFile .\AzureTemplates\roomfinder.parameters.json @templateParams `
  -Verbose:$VerbosePreference
if ($null -eq $test -or $test.Count -gt 0) {
  Write-Host ("Failed test with details {0} with {1}" -f $test.Details, $test.Message)
  Resolve-AzError -Last 
}
else {
  if ($Evaluate -ine $true) {
    Write-Verbose ("Starting deployment for shared environment")
    New-AzResourceGroupDeployment -Name ("deploy-{0}-{1}" -f $rgName, $templateParams.environmentType) -Mode Incremental -ResourceGroupName $rgName `
      -TemplateFile .\AzureTemplates\roomfinder.template.json -TemplateParameterFile .\AzureTemplates\roomfinder.parameters.json @templateParams `
      -Verbose:$VerbosePreference
  }
}