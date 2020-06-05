$array = @("KK_", "AI_", "HS2_")

if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$copy = $dir + "\copy\" 
Remove-Item -Force -Path ($copy) -Recurse -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Force -Path ($dir + "out\")

function CreateZip ($element)
{
    & robocopy ($dir + "\BepInEx\plugins\") ($copy + "\BepInEx\plugins\") ($element + "*.*") /R:5 /W:5
    & robocopy ($dir + "\mods\") ($copy + "\mods\") ($element + "*.*") /R:5 /W:5
    
    $ver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Get-ChildItem -Path ($copy + "\*.dll") -Recurse -Force)[0]).FileVersion.ToString()
    
    Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + $element + "QuickAccessBox_" + $ver + ".zip")

    Remove-Item -Force -Path ($copy) -Recurse
}

foreach ($element in $array) 
{
    try
    {
        CreateZip ($element)
    }
    catch 
    {
        # retry
        CreateZip ($element)
    }
}
