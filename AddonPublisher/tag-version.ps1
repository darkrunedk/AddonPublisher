# Load assembly version from your compiled EXE or DLL
$assemblyPath = "$PSScriptRoot\bin\Release\net8.0-windows\AddonPublisher.exe"
$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$version = $assembly.GetName().Version.ToString()

# Format tag (e.g., v1.0.0)
$tag = "v$version"

# Check if tag already exists
$existingTags = git tag
if ($existingTags -contains $tag) {
    Write-Host "Tag $tag already exists. Skipping."
    exit
}

# Create and push tag
git tag $tag
git push origin $tag

Write-Host "Tagged current commit as $tag and pushed to GitHub."