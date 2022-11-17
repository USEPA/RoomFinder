### EPA PowerShell Visual Studio Solution
# Examples
The following folder contains running code that indexes the environment, performs storage, enhances analytics, and administers various components of the USEPA tenant.
This code was written by Microsoft Consulting Services and improved via Premier Services.
This code exists within the repository to collaborate with other employees and contractors with respect to SharePoint, Exchange, and other Office 365 services.

## Agreement Scope
Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.
THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object code form of the Sample Code, provided that.
You agree:
(i) to not use Our name, logo, or trademarks to market Your software product in which the Sample Code is embedded;
(ii) to include a valid copyright notice on Your software product in which the Sample Code is embedded; and
(iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits, including attorneys’ fees, that arise or result from the use or distribution of the Sample Code
If your release clearly meets all of the above criteria, you may publish it but you must include the following header in each source file:
// Copyright © Microsoft Corporation. All Rights Reserved.

### Usage
This code is released under the terms of the GNU General Public License (GNU GPLv3)
You may not use this code in a non-open source code base.
You may not incorporate this code into code that you will resale.
You may not incorporate this code into your code base without providing reference materials to the origin.
You will not hold liable any author of this code as this code is sample.

### Prerquisites
Visual Studio 2019 - Include these Workloads: ASP.NET and web development, Azure Development, Data storage and processing, .NET Core cross-platform development
Microsoft .NET Core SDK 3.1

### Solution
Upon initially opening the EPA.PowerShell.sln, missing packages will automatically be downloaded and restored. 
The solution consists of four projects:
    EPA.Office365 - This project contains the Entities, Mappings, and Helper classes to support the O365 Reporting solution. NOTE: The db_analytics solution provides the SQL schema.
    EPA.Office365.API - This project support Azure connected services. 
    EPA.SharePoint.PowerShell - This project remains for legacy purposes.
    EPA.SharePoint.SysConsole - This projects contains the commands, models, and helper classes to support the "runtime" of user requests, i.e. reportusage. 

### Console Scripts (Batch Files)
The scripts were originally built using PowerShell (which is why the solution name contains 'PowerShell'). The scripts have since been converted to console applications. These applications can be run
from a command line (or PowerShell). 

### Debugging
NOTE: For Report Usage, ensure the db_analytics database is available. Open the EPA.SharePoint.SysConsole AppSettings.JSON file. Ensure the keys are pointing to the correct values. Ensure the User Secrets file is populated.

1. Set EPA.SharePoint.SysConsole as a Startup Project
2. To run a specific command, for example, Report Usage for Teams, go to the Project properties for EPA.SharePoint.Console
3. Go to the Debug tab
4. Within Application Arguments enter: ```epa-console.exe reportusage -r MSTeams -p D90 -l teams-count-log.txt -v```
NOTE: This will retrieve the Teams Counts Reporting for the last 90 days, and will export verbose information to a log file.
5. Press F5 (or select Play button in the ribbon) to Debug the solution. 

### Staging
The Console Scripts and corresponding assemblies reside on the ASP Staging 1 server.
- Script and Assembly Location: D:\public\data\asp4sharepoint\PowerBI\SysConsole
- Log File Location: D:\data\epa-logs
The Console Scripts are triggered via the Windows Task Scheduler
- Location: Task Scheduler Library->Microsoft->EPA

### Reference 
.NET Core 3.1: https://dotnet.microsoft.com/download/dotnet-core/3.1
Microsoft Graph API Reference: https://docs.microsoft.com/en-us/graph/api/overview?view=graph-rest-1.0
Working with Office 365 usage reports in Microsoft Graph: https://docs.microsoft.com/en-us/graph/api/resources/report?view=graph-rest-1.0
Graph Explorer (Testing and Troubleshooting API Calls): https://developer.microsoft.com/en-us/graph/graph-explorer