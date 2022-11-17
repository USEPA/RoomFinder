@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
CD EPA.SharePoint.SysConsole
.\epa-console.exe MoveEPAEZFormsRequests --migrate-subfolder true --migrate-deleted -l "ezforms-move.txt"