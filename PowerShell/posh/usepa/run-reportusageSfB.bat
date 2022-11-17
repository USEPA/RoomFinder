@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
CD EPA.SharePoint.SysConsole
.\epa-console.exe reportusage -r Skype -p D90 -l reportusage-sfb-period.txt
.\epa-console.exe reportusage -r Skype --details -l reportusage-sfb-details.txt