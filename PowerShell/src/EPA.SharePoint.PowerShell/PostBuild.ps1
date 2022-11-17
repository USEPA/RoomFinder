param($ProjectDir, $ConfigurationName, $TargetDir, $TargetFileName, $SolutionDir, $ProjectName)
begin 
{
	Write-Output ("Starting the post build script for {0}" -f $TargetDir)
}
process
{
	if($ConfigurationName -like "Debug*")
	{	
		$documentsFolder = [environment]::getfolderpath("mydocuments");

		Remove-Module -Name $ProjectName -ErrorAction SilentlyContinue
		$PSModuleHome = ("{0}\WindowsPowerShell\Modules\{1}" -f $documentsFolder,$ProjectName)

		# Module folder there?
		if(Test-Path $PSModuleHome)
		{
			# Yes, empty it but first unblock the files
			Remove-Item $PSModuleHome\* -Force -Recurse
		} 
		else 
		{
			# No, create it
			New-Item -Path $PSModuleHome -ItemType Directory -Force >$null # Suppress output
		}

		Write-Host "Copying files from $TargetDir to $PSModuleHome"
		Copy-Item "$TargetDir\*.dll" -Destination "$PSModuleHome" -Force
		Copy-Item "$TargetDir\*help.xml" -Destination "$PSModuleHome" -Force
		Copy-Item ("{0}.config" -f $TargetFileName) -Destination "$PSModuleHome" -Force
		Copy-Item "$TargetDir\*.psd1" -Destination  "$PSModuleHome" -Force
		Copy-Item "$TargetDir\*.ps1xml" -Destination "$PSModuleHome" -Force
		Copy-Item "$TargetDir\*.psm1" -Destination "$PSModuleHome" -Force
		Copy-Item "$TargetDir\*.resx" -Destination "$PSModuleHome" -Force
	} 
	elseif ($ConfigurationName -like "Release*")
	{
		$distDir = "$SolutionDir\dist";

		# Dist folder there? If so, empty it.
		if(Test-Path $distDir)
		{
			Remove-Item $distDir\*
		} else {
			# Create folder
			New-Item -Path "$distDir" -ItemType Directory -Force >$null # Suppress output
		}
		# Copy files to 'dist' folder
		Write-Host "Copying files from $TargetDir to $distDir"
		Copy-Item "$TargetDir\*.dll" -Destination "$distDir"
		Copy-Item "$TargetDir\*help.xml" -Destination "$distDir"
		Copy-Item ("{0}.config" -f $TargetFileName) -Destination "$PSModuleHome" -Force
		Copy-Item "$TargetDir\*.psd1" -Destination  "$distDir"
		Copy-Item "$TargetDir\*.ps1xml" -Destination "$distDir"
		Copy-Item "$TargetDir\*.psm1" -Destination "$distDir" -Force
		Copy-Item "$SolutionDir\install.ps1" -Destination "$distDir"
		ii $distDir
	}
}
end
{
	Write-Output ("Finish the post build script for {0}" -f $TargetDir)
}
	