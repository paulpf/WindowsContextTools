# Windows Explorer context tools

- [Windows Explorer context tools](#windows-explorer-context-tools)
  - [Introduction](#introduction)
  - [CI/CD](#cicd)
  - [Available commands](#available-commands)
  - [How to add the tool to the context menu in Windows Explorer](#how-to-add-the-tool-to-the-context-menu-in-windows-explorer)
  - [How to add the tool to the SendTo menu in Windows Explorer](#how-to-add-the-tool-to-the-sendto-menu-in-windows-explorer)

## Introduction

This tool is an app that runs from the context menu of Windows Explorer and executes various commands.

After building the solution, you can run the CMD file “AddToSendTo.cmd” to add an entry to the “Send to” folder.

After this, you can start the tool from the context menu in Windows Explorer.


## CI/CD

The repository uses GitHub Actions for CI/CD:

- **CI** (`.github/workflows/ci.yml`): runs on push/pull request and executes restore, build and test on `windows-latest`.
- **CD** (`.github/workflows/cd.yml`): runs on version tags (`v*`) or manually, publishes a `win-x64` release build, uploads artifacts and creates a GitHub Release for tags.

## Available commands

- Create a list of files and folders
- Create a list of all files
- Create a list of all folders
- Create a list of all folders and subfolders
- Find the smallest solution for the project

## How to add the tool to the context menu in Windows Explorer

This method registers the tool in the Windows Registry so it appears in the right-click context menu. This works on **local drives and network shares**.

1. **Build the Solution**: Ensure that the solution is built and the executable is located in the `bin\Debug\net8.0-windows` folder.

2. **Run the CMD File**: Execute `scripts/AddToContextMenu.cmd`. This registers context menu entries under `HKEY_CURRENT_USER` (no admin rights required).

    The script registers the tool for:
    - **Folders** (right-click on a folder)
    - **Folder background** (right-click inside a folder)
    - **Files** (right-click on any file)

3. **Use the Tool**: Right-click on any file or folder (including network shares) and select `MyContextTools`.

4. **Remove**: To remove the context menu entries, run `scripts/RemoveFromContextMenu.cmd`.

## How to add the tool to the SendTo menu in Windows Explorer

To add the tool to the "SendTo" menu in Windows Explorer, follow these steps:

1. **Build the Solution**: Ensure that the solution is built and the executable is located in the `bin\Debug\net8.0-windows` folder.

2. **Run the CMD File**: Execute the `scripts/AddToSendTo.cmd` file. This script will create a shortcut in the "SendTo" folder.

    ```bat
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
    ```

3. **Verify the Shortcut**: After running the script, verify that the shortcut `MyContextTools.lnk` is created in the "SendTo" folder.

4. **Use the Tool**: You can now use the tool from the context menu in Windows Explorer by right-clicking on a file or folder, navigating to "Send to", and selecting `MyContextTools`.

This will allow you to execute various commands provided by the tool directly from the Windows Explorer context menu.
