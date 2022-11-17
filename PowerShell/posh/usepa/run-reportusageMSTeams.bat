@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
CD EPA.SharePoint.SysConsole
.\epa-console.exe reportusage -r MSTeams -p D90 -l reportusage-msteams-period.txt
.\epa-console.exe reportusage -r MSTeams --details -l reportusage-msteams-details.txt