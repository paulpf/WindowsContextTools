@echo off
setlocal

REM Define the path to the executable in the bin\Debug\net8.0-windows folder
set EXE_PATH=%~dp0..\bin\Debug\net8.0-windows\WindowsExplorerContextTools.exe

REM Define the SendTo folder path
set SENDTO_PATH=%APPDATA%\Microsoft\Windows\SendTo

REM Check if the SendTo folder exists
if not exist "%SENDTO_PATH%" (
    echo SendTo folder not found.
    exit /b 1
)

REM Create a shortcut in the SendTo folder
set SHORTCUT_PATH=%SENDTO_PATH%\MyContextTools.lnk
echo Creating shortcut at %SHORTCUT_PATH%
powershell -command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%SHORTCUT_PATH%'); $s.TargetPath = '%EXE_PATH%'; $s.Save()"

echo Shortcut created successfully.
endlocal
exit /b 0
