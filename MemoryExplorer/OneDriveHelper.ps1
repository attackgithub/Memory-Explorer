[CmdletBinding()]
param(
	[string]$location,
	[string]$oneDriveFolder
)
Write-Host "OneDrive Helper - $location : $oneDriveFolder"
$businessLogic = Join-Path $location 'BusinessLogic.cs'
if(Test-Path $businessLogic)
{
	$destination = Join-Path $oneDriveFolder 'BusinessLogic.cs'
	Copy-Item -Path $businessLogic -Destination $destination -Force -Confirm:$false
}

$dataModel = Join-Path $location 'DataModel.cs'
if(Test-Path $dataModel)
{
	$destination = Join-Path $oneDriveFolder 'DataModel.cs'
	Copy-Item -Path $dataModel -Destination $destination -Force -Confirm:$false
}