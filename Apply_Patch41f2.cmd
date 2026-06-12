@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Apply_Patch41f2_SchemaUpgradeWarnings_ASCII.ps1"
endlocal
