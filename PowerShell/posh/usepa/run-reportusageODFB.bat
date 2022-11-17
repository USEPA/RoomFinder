@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
CD EPA.SharePoint.SysConsole
.\epa-console.exe reportusage -r OneDrive -p D90 -l reportusage-odfb-period.txt
.\epa-console.exe reportusage -r OneDrive --details -l reportusage-odfb-details.txt