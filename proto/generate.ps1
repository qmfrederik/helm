$files = Get-ChildItem -Filter *.proto -Recurse . | Resolve-Path -Relative

foreach ($file in $files)
{
  ..\bin\protoc.exe --csharp_out=..\src\Helm\Hapi $file
}