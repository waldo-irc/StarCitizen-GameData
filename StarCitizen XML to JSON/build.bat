@ECHO OFF

echo Building Win x64
dotnet publish "StarCitizen XML to JSON.csproj" -c Release -r win-x64 --self-contained=true

WHERE wsl
IF %ERRORLEVEL% EQU 0 (
	echo Building Linux x64
	wsl dotnet publish "StarCitizen XML to JSON.csproj" -c Release -r linux-x64 --self-contained=true 
)else(
	ECHO Skipping Linux build, missing WSL
)
