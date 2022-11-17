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

### Solution
Upon initially opening the db_analytics.sln, missing packages will automatically be downloaded and restored. 
The solution consists of a single project; the purpose to maintain the SQL Schema for the db_analytics database. This database is used in the EPA.PowerShell.sln for Report Usage (Teams, Skype for Business, OneDrive for Business, and SharePoint Online)

### Development
Within Solution Explorer, right-click on the project and select: Publish...
Ensure that you have a Target Database Connection to a DEV SQL Server
Click on Advanced Settings and update as necessary. For example, disable settings that may result in data loss.
Click on Publish

### Deployment
Within Solution Explorer, right-click on the project and select: Publish...
Ensure that you have a Target Database Connection to a DEV SQL Server
Click on Advanced Settings and update as necessary. For example, disable settings that may result in data loss.
Click on Generate Script
Run the generated SQL script on the target environment
