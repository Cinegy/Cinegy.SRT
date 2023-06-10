@echo off

set LOCAL_NUGET_FEED_DIRECTORY=%1
if not DEFINED LOCAL_NUGET_FEED_DIRECTORY goto WrongParameters

for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"

set major=9999
set suffix=Local

rem Trim lead zeros
set /a YYt=1%YY%+1%YY%-2%YY%
set /a DDt=1%DD%+1%DD%-2%DD%
set /a Mint=1%Min%+1%Min%-2%Min%

set version=%major%.%YY%%MM%.%DD%%HH%.%Min%%Sec%-%suffix%
set trimVersion=%major%.%YYt%%MM%.%DDt%%HH%.%Mint%%Sec%-%suffix%

echo version: "%version%"
echo Trim version: "%trimVersion%"

dotnet build Cinegy.Srt.Wrapper/Cinegy.Srt.Wrapper.csproj --no-restore -p:Version=%version% -c Debug


for /R ..\ %%i IN (*%trimVersion%.nupkg) DO (
echo Copying %%i package
    xcopy "%%i" "%LOCAL_NUGET_FEED_DIRECTORY%"
)

goto end

:WrongParameters
echo Wrong parameters number are specified!
echo Usage: batch.bat LOCAL_NUGET_FEED_DIRECTORY
goto end

:end

@echo on