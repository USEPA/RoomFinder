``` xml
<?xml version="1.0" encoding="utf-8"?><s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body>
<s:Fault>
<faultcode xmlns:a="http://schemas.microsoft.com/exchange/services/2006/types">a:ErrorAccessDenied</faultcode>
<faultstring xml:lang="en-US">The caller has not assigned any of the RBAC roles requested in the management role header.</faultstring>
<detail><e:ResponseCode xmlns:e="http://schemas.microsoft.com/exchange/services/2006/errors">ErrorAccessDenied</e:ResponseCode>
<e:Message xmlns:e="http://schemas.microsoft.com/exchange/services/2006/errors">The caller has not assigned any of the RBAC roles requested in the management role header.</e:Message></detail></s:Fault></s:Body>
</s:Envelope>
````

# Exchange (side load the app)
https://outlook.office.com/owa/?path=/options/manageapps


### Stack overflow (notes)

// SyncedEquipmentMailbox = -2147481594; EquipmentMailbox = 32
// Per (July 11, 2016, limit the filter to only SyncedEquipmentMailbox (do not include EquipmentMailbox)
// (&(msExchRecipientTypeDetails=-2147481594))


## Prerequisites
        https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-3.1#install-the-net-core-hosting-bundle