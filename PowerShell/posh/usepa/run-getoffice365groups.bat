@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
CD EPA.SharePoint.SysConsole
.\epa-console.exe getoffice365groups --starts-with-letter "A" --ends-with-letter "Z" --starts-with-number 1