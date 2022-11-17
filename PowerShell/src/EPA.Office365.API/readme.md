## Fireup webhook testing

https://localhost:44313/api/webhooks/incoming/usepa/?code=e0f0d18218fbcb031fa17f9fbc638a8be56be3db

code: should match what is set in the secrets.json file

```javascript
  "WebHooks": {
    "usepa": {
      "SecretKey": {
        "default": "e0f0d18218fbcb031fa17f9fbc638a8be56be3db"
      }
    }
  }
```

should be a POST with sample JSON
```javascript
{
  "schemaId": "AzureMonitorMetricAlert",
  "data": {
    "version": "2.0",
    "properties": null,
    "status": "Deactivated",
    "context": {
      "timestamp": "2019-06-02T21:16:23.9411269Z",
      "id": "/subscriptions/------/resourceGroups/azuresubname/providers/microsoft.insights/metricAlerts/capacity%20reached%20threshold",
      "name": "capacity reached threshold",
      "description": "",
      "conditionType": "SingleResourceMultipleMetricCriteria",
      "severity": "1",
      "condition": {
        "windowSize": "PT1H",
        "allOf": [
          {
            "metricName": "UsedCapacity",
            "metricNamespace": "Microsoft.Storage/storageAccounts",
            "operator": "GreaterThan",
            "threshold": "4800000000",
            "timeAggregation": "Average",
            "dimensions": [
              {
                "name": "AccountResourceId",
                "value": "/subscriptions/------/resourceGroups/azuresubname/providers/Microsoft.Storage/storageAccounts/storageAccountName"
              }
            ],
            "metricValue": 0.0
          }
        ]
      },
      "subscriptionId": "------",
      "resourceGroupName": "azuresubname",
      "resourceName": "storageAccountName",
      "resourceType": "Microsoft.Storage/storageAccounts",
      "resourceId": "/subscriptions/------/resourceGroups/azuresubname/providers/Microsoft.Storage/storageAccounts/storageAccountName",
      "portalLink": "https://portal.azure.com/#resource/subscriptions/------/resourceGroups/azuresubname/providers/Microsoft.Storage/storageAccounts/storageAccountName"
    }
  }
}
```

If you want to test with a full tunnel or https look at ngrok as an offering
ngrok http 44313 -host-header=localhost

will generate a tunnelable https route to your locally running resource
https://{guid}.ngrok.io