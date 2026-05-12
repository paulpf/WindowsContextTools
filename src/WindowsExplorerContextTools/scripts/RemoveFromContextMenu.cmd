@echo off
setlocal

echo Removing MyContextTools context menu entries...

reg delete "HKCU\Software\Classes\Directory\shell\MyContextTools" /f 2>nul
reg delete "HKCU\Software\Classes\Folder\shell\MyContextTools" /f 2>nul
reg delete "HKCU\Software\Classes\Directory\Background\shell\MyContextTools" /f 2>nul
reg delete "HKCU\Software\Classes\*\shell\MyContextTools" /f 2>nul

echo Context menu entries removed.
endlocal
exit /b 0
