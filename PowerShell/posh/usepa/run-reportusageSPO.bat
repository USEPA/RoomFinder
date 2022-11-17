@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
CD EPA.SharePoint.SysConsole
.\epa-console.exe reportusage -r SharePoint -p D90 -l reportusage-spo-period.txt
.\epa-console.exe reportusage -r SharePoint --details -l reportusage-spo-details.txt