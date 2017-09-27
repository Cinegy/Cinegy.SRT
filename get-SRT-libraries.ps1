#Script to grab a known build of SRT libraries from our AppVeyor build server
#feel free to re-point this to build our tools against a different SRT library
#will require PS5 to unzip - if it fails, you can just unzip by hand to the toolkit directory

$SRT_Package_url = "https://ci.appveyor.com/api/buildjobs/skgntva8a79bd0e8/artifacts/SRT-master-Release-1.0.26.zip"

Write-Host "Downloading ZIP with SRT package... please be patient"

#$ProgressPreference = 'SilentlyContinue'

iwr -ContentType "application/octet-stream" -Uri $SRT_Package_url -OutFile ./srtpackage.zip
Expand-Archive ./srtpackage.zip ./_toolkits/srt