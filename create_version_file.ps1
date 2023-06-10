# PowerShell Cinegy Build Script
# COPYRIGHT Cinegy 2020-2022
param([string]$BuildCounter=0,[string]$SourceRevisionValue="FFFFFF",[string]$OverrideMinorVersion="")

$majorVer = 2
$minorVer = 0

#minor version may be overridden (e.g. on integration builds)
if($OverrideMinorVersion)
{
    $minorVer = $OverrideMinorVersion
}

#calculte a UInt16 from the commit hash to use as 4th version flag
$shortRev = $SourceRevisionValue.Substring(0,4)
$sourceAsDecimal = [System.Convert]::ToUInt16($shortRev, 16) -1

$softwareVersion = "$majorVer.$minorVer.$BuildCounter.$sourceAsDecimal"

#set global variable to version number
$Env:SoftwareVersion = $softwareVersion

#write out values to env file to pass between stages
Add-Content -Path version.env -Value "SoftwareVersion=$softwareVersion"
Add-Content -Path version.env -Value "shortVersion=$majorVer.$minorVer"

