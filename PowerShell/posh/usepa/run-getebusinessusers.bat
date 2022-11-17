@ECHO OFF
REM On staging, within Task Scheduler, use the Start in (optional): D:\public\data\asp4sharepoint\PowerBI\SysConsole
REM Connect with ADAL Tokens
REM Run all Graph User Reporting
CD EPA.SharePoint.SysConsole
.\epa-console.exe getebusinessusers -l "analytics-ebusiness.txt"
