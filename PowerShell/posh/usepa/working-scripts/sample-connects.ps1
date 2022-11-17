

$creds = Get-EPAStoredCredential -Name "spoADALepaReporting" -Type PSCredential
$network = $creds.GetNetworkCredential()

$graphparms = @{
  appid     = $network.UserName;
  appsecret = $network.Password;
}
$msalgraphparms = @{
  MSALClientId = $network.UserName;
  Scopes       = 'Reports.Read.All';
  ResourceUri  = 'msale9d816b4-182e-41e1-9648-a8c5a82e7424://auth';
}


& { invoke-expression (".\epa-console.exe connectadalv1 --app-id {0} --app-secret {1} -l {2}" -f $graphparms.appid, $graphparms.appsecret, $logFile) }
& { invoke-expression (".\epa-console.exe connectadalv2 --app-id {0} --app-secret {1} -l {2}" -f $graphparms.appid, $graphparms.appsecret, $logFile) }
& { invoke-expression (".\epa-console.exe connectadalv2 --msal-clientId {0} --scopes {1} --resource-uri {2} -l {3}" -f $msalgraphparms.MSALClientId, $msalgraphparms.Scopes, $msalgraphparms.ResourceUri, $logFile) }


# Detach the event handler (not detaching can lead to stack overflow issues when closing PS)
[System.AppDomain]::CurrentDomain.remove_AssemblyResolve($onAssemblyResolveEventHandler)