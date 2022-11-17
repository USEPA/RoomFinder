
[cmdletbinding()]
param(
    [ValidateSet("dev", "test", "prod")]
    [Parameter(Mandatory = $true)] 
    [String]$customer,
    
    [ValidateSet("usgovvirginia")]
    [Parameter(Mandatory = $false)] 
    [String]$location = "usgovvirginia",
    
    [Parameter(Mandatory = $false, HelpMessage = "Guid from the Azure Subscription")] 
    [String]$azureTenantId
)
BEGIN {
    $rgName = "armcontoso-epa" 
}
PROCESS {
    $group = Get-AzResourceGroup -Name $rgName
    if ($null -eq $group) {
        New-AzResourceGroup -Name $rgName -Location $location -Verbose:$VerbosePreference
    }


    $templateParams = @{
        customer = "epa-dev"
    }

    $test = Test-AzResourceGroupDeployment -ResourceGroupName $rgName `
        -TemplateFile .\arm\roomfinder.template.json -TemplateParameterFile .\arm\roomfinder.parameters.json @templateParams `
        -Verbose:$VerbosePreference
    if ($null -eq $test -or $test.Count -gt 0) {
        Write-Host ("Failed test with details {0} with {1}" -f $test.Details, $test.Message)
        Resolve-AzError -Last 
    }
    else {
        if ($Evaluate -ine $true) {
            Write-Verbose ("Starting deployment for shared environment")
            New-AzResourceGroupDeployment -Name ("deploy-{0}" -f $rgName) -Mode Incremental -ResourceGroupName $rgName `
                -TemplateFile .\arm\roomfinder.template.json -TemplateParameterFile .\arm\roomfinder.parameters.json @templateParams `
                -Verbose:$VerbosePreference
        }
    }
}
