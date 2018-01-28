$protoc = Join-Path $env:USERPROFILE ".nuget\packages\google.protobuf.tools\3.5.1\tools\windows_x64\protoc.exe"
$protocInclude = Join-Path $env:USERPROFILE ".nuget\packages\google.protobuf.tools\3.5.1\tools\"
$grpc = Join-Path $env:USERPROFILE ".nuget\packages\grpc.tools\1.9.0-pre2\tools\windows_x64\grpc_csharp_plugin.exe"


$files = Get-ChildItem -Filter *.proto -Recurse . | Resolve-Path -Relative

$google_deps = "Mgoogle/protobuf/timestamp.proto=github.com/golang/protobuf/ptypes/timestamp,Mgoogle/protobuf/any.proto=github.com/golang/protobuf/ptypes/any"

foreach ($file in $files)
{
  & $protoc --plugin=protoc-gen-grpc=$($grpc) --grpc_out=..\src\Helm\Hapi --csharp_out=..\src\Helm\Hapi  -I""$($protocInclude)"" -I. $file
}