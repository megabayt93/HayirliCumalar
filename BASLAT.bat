@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo C# derleyicisi bulunamadi. .NET Framework 4.8 veya Visual Studio kurun.
  pause
  exit /b 1
)
for %%I in ("%CSC%") do set "WPF=%%~dpIWPF"
"%CSC%" /reference:"%WPF%\WindowsBase.dll" /reference:"%WPF%\PresentationCore.dll" /nologo /target:exe /optimize+ /codepage:65001 /out:HayirliCumalar.exe Program.cs
if errorlevel 1 (
  echo Derleme basarisiz.
  pause
  exit /b 1
)
HayirliCumalar.exe
pause
