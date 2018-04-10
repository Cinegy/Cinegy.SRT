#Script to grab a known build of SRT libraries from our AppVeyor build server
#feel free to re-point this to build our tools against a different SRT library
#will require PS5 to unzip - if it fails, you can just unzip by hand to the toolkit directory

$SRT_Package_url = "https://ci.appveyor.com/api/buildjobs/2cqoxhdar6ska5r8/artifacts/SRT-master-Release-1.0.469.zip"

$SRTPackageName =  Split-Path -Path $url -Leaf

Write-Host "Downloading ZIP with SRT package... please be patient"

#$ProgressPreference = 'SilentlyContinue'

iwr -ContentType "application/octet-stream" -Uri $SRT_Package_url -OutFile ./$SRTPackageName
Expand-Archive ./$SRTPackageName ./_toolkits/srt -force