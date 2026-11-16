$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$cache = Join-Path $project 'Temp/StandCoach'
$runtime = Join-Path $project 'StandCoachRuntime'
New-Item -ItemType Directory -Force $cache, "$runtime/llama", "$runtime/speech", "$runtime/models" | Out-Null

function Get-ReleaseFile($url, $name, $hash) {
    $destination = Join-Path $cache $name
    if (!(Test-Path -LiteralPath $destination) -or (Get-FileHash -LiteralPath $destination).Hash -ne $hash) {
        & curl.exe --fail --location --retry 2 --silent --show-error $url -o $destination
        if ($LASTEXITCODE -ne 0 -or (Get-FileHash -LiteralPath $destination).Hash -ne $hash) {
            throw "Unable to verify the download for $name."
        }
    }
    return $destination
}

$llama = Get-ReleaseFile 'https://github.com/ggml-org/llama.cpp/releases/download/b9000/llama-b9000-bin-win-cpu-x64.zip' 'llama.zip' '8294E287933D3212AA93A32E1CEB800722BEDE14D9692D405679D4BA77CF05DB'
$languageModel = Get-ReleaseFile 'https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF/resolve/main/qwen2.5-1.5b-instruct-q4_k_m.gguf?download=true' 'model-1.5.gguf' '6A1A2EB6D15622BF3C96857206351BA97E1AF16C30D7A74EE38970E434E9407E'
$sherpa = Get-ReleaseFile 'https://github.com/k2-fsa/sherpa-onnx/releases/download/v1.13.5/sherpa-onnx-v1.13.5-win-x64-shared-MT-Release.tar.bz2' 'sherpa.tar.bz2' '70B7193E40CE5C393A218C6383069A64CF6479962AE87CDC19373B8B57E3F285'
$voice = Get-ReleaseFile 'https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/kokoro-int8-multi-lang-v1_0.tar.bz2' 'kokoro.tar.bz2' '72C347230107EF8A31098EC4D468A605244BEEF5F1ED00DB97EFA13518800653'

New-Item -ItemType Directory -Force "$cache/llama" | Out-Null
& tar.exe -xf $llama -C "$cache/llama"
if ($LASTEXITCODE -ne 0) { throw 'Could not extract llama.cpp.' }
& tar.exe -xf $sherpa -C $cache
if ($LASTEXITCODE -ne 0) { throw 'Could not extract sherpa-onnx.' }
& tar.exe -xf $voice -C $cache
if ($LASTEXITCODE -ne 0) { throw 'Could not extract Kokoro.' }

Copy-Item "$cache/llama/*.dll", "$cache/llama/llama-server.exe" -Destination "$runtime/llama" -Force
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $crt = & $vswhere -latest -products '*' -find 'VC\Redist\MSVC\**\x64\Microsoft.VC*.CRT\msvcp140.dll' |
        Where-Object { $_ -notmatch '\\onecore\\' } | Select-Object -First 1
    if ($crt) {
        Copy-Item -Path (Join-Path (Split-Path -Parent $crt) '*.dll') -Destination "$runtime/llama" -Force
    }
}
if (!(Test-Path -LiteralPath "$runtime/llama/msvcp140.dll")) {
    Write-Warning 'App-local Visual C++ libraries were unavailable. Target PCs will need the Microsoft Visual C++ x64 Redistributable.'
}
$sherpaLib = Join-Path $cache 'sherpa-onnx-v1.13.5-win-x64-shared-MT-Release/lib'
Copy-Item "$sherpaLib/sherpa-onnx-c-api.dll", "$sherpaLib/onnxruntime.dll", "$sherpaLib/onnxruntime_providers_shared.dll" -Destination "$runtime/speech" -Force
Copy-Item -LiteralPath $languageModel -Destination "$runtime/models/coach.gguf" -Force
$voiceFiles = Join-Path $cache 'kokoro-int8-multi-lang-v1_0'
Copy-Item "$voiceFiles/model.int8.onnx", "$voiceFiles/voices.bin", "$voiceFiles/tokens.txt", "$voiceFiles/LICENSE" -Destination "$runtime/speech" -Force
Copy-Item -LiteralPath "$voiceFiles/espeak-ng-data" -Destination "$runtime/speech" -Recurse -Force

$licenses = @{
    'llama/LICENSE' = 'https://raw.githubusercontent.com/ggml-org/llama.cpp/b9000/LICENSE'
    'speech/SHERPA-LICENSE' = 'https://raw.githubusercontent.com/k2-fsa/sherpa-onnx/v1.13.5/LICENSE'
    'models/LICENSE' = 'https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF/raw/main/LICENSE'
}
foreach ($license in $licenses.GetEnumerator()) {
    & curl.exe --fail --location --retry 2 --silent --show-error $license.Value -o (Join-Path $runtime $license.Key)
    if ($LASTEXITCODE -ne 0) { throw "Could not download $($license.Key)." }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'StandCoach-ThirdParty.txt') -Destination "$runtime/ThirdParty.txt" -Force
Write-Host "Stand Coach files are ready in $runtime. Unity includes them automatically in Windows x64 builds."
