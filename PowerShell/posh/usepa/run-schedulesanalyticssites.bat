@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
CD EPA.SharePoint.SysConsole
.\epa-console.exe getEPAAnalyticO365Sites --enumerate-subsites -l scheduleanalyticssites.txt