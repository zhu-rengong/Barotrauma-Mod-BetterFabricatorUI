param (
    [string]$modDeployDir
)

$deployPathTupleList = @(
    @((Join-Path -Path $PSScriptRoot -ChildPath "Assets\Content"), (Join-Path -Path $modDeployDir -ChildPath "Content")),
    @((Join-Path -Path $PSScriptRoot -ChildPath "Assets\Lua"), (Join-Path -Path $modDeployDir -ChildPath "Lua")),
    @((Join-Path -Path $PSScriptRoot -ChildPath "SharedProject\SharedSource"), (Join-Path -Path $modDeployDir -ChildPath "CSharp\Shared")),
    @((Join-Path -Path $PSScriptRoot -ChildPath "ClientProject\ClientSource"), (Join-Path -Path $modDeployDir -ChildPath "CSharp\Client")),
    @((Join-Path -Path $PSScriptRoot -ChildPath "ServerProject\ServerSource"), (Join-Path -Path $modDeployDir -ChildPath "CSharp\Server"))
)

# Function to get the SHA256 hash and size of a file
function Get-FileInfo {
    param (
        [string]$filePath
    )
    $fileInfo = New-Object PSObject
    $fileInfo | Add-Member -MemberType NoteProperty -Name 'Hash' -Value (Get-FileHash -Path $filePath -Algorithm SHA256).Hash
    $fileInfo | Add-Member -MemberType NoteProperty -Name 'Size' -Value (Get-Item $filePath).Length
    $fileInfo | Add-Member -MemberType NoteProperty -Name 'DirectoryName' -Value (Get-Item $filePath).DirectoryName
    return $fileInfo
}

# Function to ensure the target directory exists
function Ensure-DirectoryExists {
    param (
        [string]$directoryPath
    )
    if (-not (Test-Path -Path $directoryPath -PathType Container)) {
        New-Item -Path $directoryPath -ItemType Directory -Force | Out-Null
    }
}

# Function to sync files and output changes
function Sync-FilesWithLogging {
    param (
        [string]$source,
        [string]$target
    )

    $separatorLine = '-' * $HOST.UI.RawUI.WindowSize.Width
    Write-Host $separatorLine
    Write-Host "Start syncing files......"
    Write-Host "Source: ""$source"""
    Write-Host "Destination: ""$source"""
    Write-Host ""

    # Create dictionaries to store file info for quick lookup
    $sourceFiles = @{}
    $targetFiles = @{}

    # Get file info for all files in the source directory
    Get-ChildItem -Path $source -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($source.Length + 1)
        $sourceFiles[$relativePath] = Get-FileInfo -filePath $_.FullName
    }

    # Ensure the target directory exists
    Ensure-DirectoryExists -directoryPath $target

    # Get file info for all files in the target directory
    Get-ChildItem -Path $target -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($target.Length + 1)
        $targetFiles[$relativePath] = Get-FileInfo -filePath $_.FullName
    }

    # Initialize lists for logging and counters
    $addedFiles = @()
    $updatedFiles = @()
    $removedFiles = @()
    $addedCount = 0
    $updatedCount = 0
    $removedCount = 0

    # Process source files
    foreach ($key in $sourceFiles.Keys) {
        if (-not $targetFiles.ContainsKey($key)) {
            # File is in source but not in target, add it
            $addedFiles += $key
            $targetFilePath = (Join-Path -Path $target -ChildPath $key)
            Ensure-DirectoryExists -directoryPath ($targetFilePath | Split-Path)
            Copy-Item -Path (Join-Path -Path $source -ChildPath $key) -Destination $targetFilePath -Force -ErrorAction Stop
            $addedCount++
        }
        elseif ($sourceFiles[$key].Hash -ne $targetFiles[$key].Hash) {
            # File is in both source and target but hashes differ, update it
            $updatedFiles += $key
            Copy-Item -Path (Join-Path -Path $source -ChildPath $key) -Destination (Join-Path -Path $target -ChildPath $key) -Force -ErrorAction Stop
            $updatedCount++
        }
    }

    # Process target files that are not in source
    foreach ($key in $targetFiles.Keys) {
        if (-not $sourceFiles.ContainsKey($key)) {
            # File is in target but not in source, remove it
            $removedFiles += $key
            Remove-Item -Path (Join-Path -Path $target -ChildPath $key) -Force -ErrorAction Stop
            $removedCount++
        }
    }

    # Output logs
    Write-Host "Added files ($addedCount):" -ForegroundColor Green
    $addedFiles | ForEach-Object {
        $fileSize = $sourceFiles[$_].Size
        $formattedSize = ($fileSize).ToString("N0") + "bytes" # Format size
        Write-Host "  $(Join-Path -Path $target -ChildPath $_) - Size: $formattedSize" -ForegroundColor DarkGreen
    }
    Write-Host ""

    Write-Host "Updated files ($updatedCount):" -ForegroundColor Blue
    $updatedFiles | ForEach-Object {
        $oldSize = $targetFiles[$_].Size
        $newSize = $sourceFiles[$_].Size
        $sizeChange = ($newSize - $oldSize).ToString("N0") + "bytes" # Format size change
        $formattedOldSize = ($oldSize).ToString("N0") + "bytes" # Format old size
        $formattedNewSize = ($newSize).ToString("N0") + "bytes" # Format new size
        Write-Host "  $(Join-Path -Path $target -ChildPath $_) - Old Size: $formattedOldSize, New Size: $formattedNewSize, Change: $sizeChange" -ForegroundColor DarkBlue
    }
    Write-Host ""

    Write-Host "Removed files ($removedCount):" -ForegroundColor Red
    $removedFiles | ForEach-Object {
        $fileSize = $targetFiles[$_].Size
        $formattedSize = ($fileSize).ToString("N0") + "bytes" # Format size
        Write-Host "  $(Join-Path -Path $target -ChildPath $_) - Size: $formattedSize" -ForegroundColor DarkRed
    }
    Write-Host ""

    $sourceDirectories = @{}
    $targetDirectories = @{}

    Get-ChildItem -Path $source -Recurse -Directory | ForEach-Object {
        $relativePath = $_.FullName.Substring($source.Length + 1)
        $sourceDirectories[$relativePath] = $_
    }

    Get-ChildItem -Path $target -Recurse -Directory | ForEach-Object {
        $relativePath = $_.FullName.Substring($target.Length + 1)
        $targetDirectories[$relativePath] = $_
    }

    foreach ($key in $targetDirectories.Keys) {
        if (-not $sourceDirectories.ContainsKey($key)) {
            $targetDirectory = $targetDirectories[$key].FullName
            if (Test-Path -Path $targetDirectory) {
                Remove-Item -Path $targetDirectory -Recurse -ErrorAction Stop
            }
        }
    }
}

for ($i = 0; $i -lt $deployPathTupleList.Count; $i++) {
    $source = $deployPathTupleList[$i][0]
    $target = $deployPathTupleList[$i][1]
    Sync-FilesWithLogging -source $source -target $target
}