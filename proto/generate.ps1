$runtime = "windows_x64"
$suffix = ".exe"

if ($env:OS -ne "Windows_NT")
{
  $runtime = "linux_x64"
  $suffix = ""
}

$protoc = Join-Path $env:USERPROFILE ".nuget\packages\google.protobuf.tools\3.5.1\tools\$runtime\protoc$suffix"
$protocInclude = Join-Path $env:USERPROFILE ".nuget\packages\google.protobuf.tools\3.5.1\tools\"
$grpc = Join-Path $env:USERPROFILE ".nuget\packages\grpc.tools\1.9.0-pre2\tools\$runtime\grpc_csharp_plugin$suffix"

$files = Get-ChildItem -Filter *.proto -Recurse . | Resolve-Path -Relative

$google_deps = "Mgoogle/protobuf/timestamp.proto=github.com/golang/protobuf/ptypes/timestamp,Mgoogle/protobuf/any.proto=github.com/golang/protobuf/ptypes/any"

foreach ($file in $files)
{
  & $protoc --plugin=protoc-gen-grpc=$($grpc) --grpc_out=..\src\Helm\Hapi --csharp_out=..\src\Helm\Hapi  -I""$($protocInclude)"" -I. $file
}