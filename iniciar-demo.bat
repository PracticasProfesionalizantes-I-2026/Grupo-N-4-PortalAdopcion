@echo off
cd /d "%~dp0"
dotnet --version
if errorlevel 1 (
 echo Instalar primero el SDK .NET 10.
 pause
 exit /b 1
)
echo Cuando el servidor arranque, abrir http://localhost:5104
dotnet run --project API --launch-profile Demo
pause
