[CmdletBinding()]
param(
	[string]$version,
	[string]$targetPath,
	[string]$zipPath
)
Write-Host 'POWERSHELL BUILD HELPER'
Write-Host 'Temp Directory: ' $env:TEMP
if($version.EndsWith('.0'))
{
	$version = $version.Substring(0, $version.Length-2)
}
if(Test-Path $env:TEMP)
{
	$projectDirectory = $zipPath
	$zipPath = Join-Path $zipPath 'Binaries'
	$exeFolder = (Get-Item $targetPath).DirectoryName
	Write-Host 'EXE FOLDER: ' $exeFolder
	$tempFolder = Join-Path $env:TEMP ('MemoryExplorer_' + $version)
	Write-Host 'Target Folder: ' $tempFolder
	if(Test-Path $tempFolder)
	{
		Remove-Item $tempFolder -Force -Recurse
	}
	New-Item $tempFolder -ItemType Directory | Out-Null
	if(Test-Path $tempFolder)
	{
		$mainFolder = Join-Path $tempFolder ('MemoryExplorer_' + $version)
		New-Item $mainFolder -ItemType Directory | Out-Null
		if(Test-Path $mainFolder)
		{
			Write-Host 'Created Temporary Folder: ' $mainFolder
			Copy-Item $targetPath $mainFolder
			Copy-Item (Join-Path $exeFolder '*.dll') $mainFolder
			Copy-Item (Join-path (Join-Path $projectDirectory 'Resources') '*.*')  $mainFolder
			$destination = Join-Path $zipPath ('MemoryExplorer_' + $version + '.zip')
			if(Test-Path $destination) {Remove-Item $destination -Force -Recurse}
			Add-Type -Assembly "system.io.compression.filesystem" 
			[io.compression.zipfile]::CreateFromDirectory($tempFolder, $destination)
			if(Test-Path $destination)
			{
				Write-Host 'Created Zip Archive: ' $destination
			}
			else
			{
				Write-Host 'There was a problem creating Zip Archive: ' $destination
			}
		}
		Remove-Item $tempFolder -Force -Recurse
	}
	else
	{
		Write-Host "Couldn't create temp folder: " $tempFolder
	}
}



