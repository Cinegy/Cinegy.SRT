# PowerShell Cinegy Build Script
# COPYRIGHT Cinegy 2020-2023
param([string]$softwareVersion)

$SoftwareVersion = $softwareVersion

Get-ChildItem -Path *.csproj -Recurse | ForEach-Object {
    $fileName = $_
    Write-Host "Processing metadata changes for file: $fileName"
	
	[xml]$projectXml = Get-Content -Path $fileName

	$nodes = $projectXml.SelectNodes("/Project/PropertyGroup/Copyright")
	foreach($node in $nodes) {
		$node.'#text' = "$([char]0xA9)$((Get-Date).year) Cinegy. All rights reserved."
	}

	$nodes = $projectXml.SelectNodes("/Project/PropertyGroup/Description")
	foreach($node in $nodes) {
		$node.'#text' = "$($node.'#text')"
	}

	$nodes = $projectXml.SelectNodes("/Project/PropertyGroup/Version")
	foreach($node in $nodes) {
		$node.'#text' = $SoftwareVersion
	}

	$nodes = $projectXml.SelectNodes("/Project/PropertyGroup/AssemblyVersion")
	foreach($node in $nodes) {
		$node.'#text' = $SoftwareVersion
	}

	$nodes = $projectXml.SelectNodes("/Project/PropertyGroup/FileVersion")
	foreach($node in $nodes) {
		$node.'#text' = $SoftwareVersion
	}

	$projectXml.Save($fileName)
}
