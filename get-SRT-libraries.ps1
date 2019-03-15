#Script to grab a known build of SRT libraries from our AppVeyor build server
#feel free to re-point this to build our tools against a different SRT library
#will require PS5 to unzip - if it fails, you can just unzip by hand to the toolkit directory

$SRT_Package_url = "https://ci.appveyor.com/api/buildjobs/7tmb6jhxtdgo84a3/artifacts/SRT-Branch_v1.3.2-Release-Winx64-2015-1.3.2.76.zip"

$SRTPackageName =  Split-Path -Path $SRT_Package_url -Leaf

Write-Host "Downloading ZIP with SRT package... please be patient"

#$ProgressPreference = 'SilentlyContinue'

iwr -ContentType "application/octet-stream" -Uri $SRT_Package_url -OutFile ./$SRTPackageName
Expand-Archive ./$SRTPackageName ./_toolkits/srt -force