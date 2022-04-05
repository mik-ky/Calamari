 function IsDockerAvailable() {
    $command = $(Get-Command 'docker' -ErrorAction SilentlyContinue)
    if ($command -eq $null) {
        Write-Host 'docker command not available'
        return $false
    }

    try {
        & docker ps 1>$null 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host 'Docker was unable to connect to a docker service'
            return $false
        }
    }
    catch {
        Write-Host 'Unable to connect to docker service'
        return $false
    }

    return $LASTEXITCODE -eq 0
} 
 
 function GetCachedWorkerToolsImageDigests {
     $digests = docker image ls --format '{{.Digest}}' octopusdeploy/worker-tools
     return $digests
 }
 
 function GetSelectedWorkerToolsImageDigests {
     $separatorPosition = $IMAGE.LastIndexOf(':')
     $tag = if ($separatorPosition -gt 0) { $IMAGE.Substring($separatorPosition + 1) } else { 'latest' }
     $uri = "https://hub.docker.com/v2/repositories/octopusdeploy/worker-tools/tags/$tag"
     
     try {
         $imageInformation = (Invoke-WebRequest -URI $uri).Content | ConvertFrom-Json
         $digests = $imageInformation.Images | ForEach-Object {$_.Digest}
     } catch {
         $digests = @()
     }
     
     return $digests
 }
 
 function VerifyWorkerToolsImageBeingCached {
     $cachedImageDigests = GetCachedWorkerToolsImageDigests
     $selectedImageDigests = GetSelectedWorkerToolsImageDigests
     
     $cached = $selectedImageDigests | Where-Object { $cachedImageDigests -contains $_ } 
     
     if ($cached.Length -eq 0) {
         Write-Warning "The worker tool image '$IMAGE' is not cached, pulling may take a while"
     }
 }
 
 
$IMAGE=$OctopusParameters["Image"]
$dockerUsername=$OctopusParameters["DockerUsername"]
$dockerPassword=$OctopusParameters["DockerPassword"]
$feedUri=$OctopusParameters["FeedUri"]

if($(IsDockerAvailable) -eq $false) {
  Write-Error "You will need docker installed and running to pull docker images"
  exit 1;
}

Write-Verbose $(docker -v)

if (-not ([string]::IsNullOrEmpty($dockerUsername))) {
    # docker 17.07 throws a warning to stderr if you use the --password param
    $dockerVersion = & docker version --format '{{.Client.Version}}'
    $parsedVersion = [Version]($dockerVersion -split '-')[0]
    $dockerNeedsPasswordViaStdIn = (($parsedVersion.Major -gt 17) -or (($parsedVersion.Major -eq 17) -and ($parsedVersion.Minor -gt 6)))
    if ($dockerNeedsPasswordViaStdIn) {
        echo $dockerPassword | cmd /c "docker login --username $dockerUsername --password-stdin $feedUri 2>&1"
    } else {
        cmd /c "docker login --username $dockerUsername --password $dockerPassword $feedUri 2>&1"
    }
    
    if(!$?)
    {
        Write-Error "Login Failed"
        exit 1
    }
}

 if ($IMAGE.StartsWith("octopusdeploy/worker-tools", "InvariantCultureIgnoreCase")) {
     VerifyWorkerToolsImageBeingCached
 }
 
Write-Verbose "docker pull $IMAGE"
docker pull $IMAGE
