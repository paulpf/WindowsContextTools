@echo off
setlocal

REM Define the path to the executable in the bin\Debug\net8.0-windows folder
set EXE_PATH=%~dp0..\bin\Debug\net8.0-windows\WindowsExplorerContextTools.exe

REM Resolve to absolute path (removes relative segments like ..)
for %%I in ("%EXE_PATH%") do set EXE_PATH=%%~fI

REM Verify the executable exists
if not exist "%EXE_PATH%" (
    echo ERROR: Executable not found at %EXE_PATH%
    echo Please build the solution first.
    exit /b 1
)

REM Escape backslashes for registry
set "REG_EXE_PATH=%EXE_PATH:\=\\%"

echo Adding context menu entries for MyContextTools...
echo Executable: %EXE_PATH%
echo.

REM Context menu for folders (local directories)
reg add "HKCU\Software\Classes\Directory\shell\MyContextTools" /ve /d "MyContextTools" /f
reg add "HKCU\Software\Classes\Directory\shell\MyContextTools" /v "Icon" /d "\"%EXE_PATH%\",0" /f
reg add "HKCU\Software\Classes\Directory\shell\MyContextTools\command" /ve /d "\"%EXE_PATH%\" \"%%1\"" /f

REM Context menu for folders (includes network shares and UNC paths)
reg add "HKCU\Software\Classes\Folder\shell\MyContextTools" /ve /d "MyContextTools" /f
reg add "HKCU\Software\Classes\Folder\shell\MyContextTools" /v "Icon" /d "\"%EXE_PATH%\",0" /f
reg add "HKCU\Software\Classes\Folder\shell\MyContextTools\command" /ve /d "\"%EXE_PATH%\" \"%%1\"" /f

REM Context menu for folder background (right-click inside a folder)
reg add "HKCU\Software\Classes\Directory\Background\shell\MyContextTools" /ve /d "MyContextTools" /f
reg add "HKCU\Software\Classes\Directory\Background\shell\MyContextTools" /v "Icon" /d "\"%EXE_PATH%\",0" /f
reg add "HKCU\Software\Classes\Directory\Background\shell\MyContextTools\command" /ve /d "\"%EXE_PATH%\" \"%%V\"" /f

REM Context menu for all file types
reg add "HKCU\Software\Classes\*\shell\MyContextTools" /ve /d "MyContextTools" /f
reg add "HKCU\Software\Classes\*\shell\MyContextTools" /v "Icon" /d "\"%EXE_PATH%\",0" /f
reg add "HKCU\Software\Classes\*\shell\MyContextTools\command" /ve /d "\"%EXE_PATH%\" \"%%1\"" /f

echo.
echo Context menu entries added successfully.
echo The tool is now available via right-click on files and folders (including network shares).
endlocal
exit /b 0
