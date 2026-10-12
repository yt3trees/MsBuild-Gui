@echo off
rem Rebuild msbuild-gui-installer.msi
rem   1. dotnet publish (FolderProfile: self-contained single file, win-x64)
rem   2. Build the .vdproj with devenv (requires "Microsoft Visual Studio Installer Projects" extension)
rem Output: msbuild-gui-installer\Release\msbuild-gui-installer.msi

setlocal
set "INSTALLER_DIR=%~dp0"
set "ROOT_DIR=%INSTALLER_DIR%.."

rem ---- Locate devenv.com (latest Visual Studio) ----
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "DEVENV="
if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -prerelease -property productPath`) do set "DEVENV=%%~dpidevenv.com"
)
if not defined DEVENV (
    echo [ERROR] Visual Studio not found.
    exit /b 1
)
echo Using: %DEVENV%

rem ---- 1. Publish ----
rem Stale obj\Release (devenv wpftmp files etc.) breaks the devenv build after publish, so clean it first.
rem The publish output is also required for the installer's PublishItems output group (copilot.exe, Plugins, ...).
echo.
echo === dotnet publish ===
if exist "%ROOT_DIR%\msbuild-gui\obj\Release" rmdir /s /q "%ROOT_DIR%\msbuild-gui\obj\Release"
dotnet publish "%ROOT_DIR%\msbuild-gui\msbuild-gui.csproj" -p:PublishProfile=FolderProfile -c Release
if errorlevel 1 (
    echo [ERROR] dotnet publish failed.
    exit /b 1
)

rem ---- 2. Build installer ----
echo.
echo === Build installer ===
if exist "%INSTALLER_DIR%Release\msbuild-gui-installer.msi" del /q "%INSTALLER_DIR%Release\msbuild-gui-installer.msi"
"%DEVENV%" "%ROOT_DIR%\msbuild-gui.sln" /Build "Release|Any CPU" /Project "%INSTALLER_DIR%msbuild-gui-installer.vdproj"
if errorlevel 1 (
    echo [ERROR] Installer build failed.
    exit /b 1
)

if not exist "%INSTALLER_DIR%Release\msbuild-gui-installer.msi" (
    echo [ERROR] msi was not created.
    exit /b 1
)

echo.
echo Done: %INSTALLER_DIR%Release\msbuild-gui-installer.msi
endlocal
