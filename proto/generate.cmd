protoc --csharp_out=..\src\Helm\Hapi hapi\chart\*.proto
protoc --csharp_out=..\src\Helm\Hapi hapi\release\*.proto
protoc --csharp_out=..\src\Helm\Hapi hapi\rudder\*.proto
protoc --csharp_out=..\src\Helm\Hapi hapi\services\*.proto
protoc --csharp_out=..\src\Helm\Hapi hapi\version\*.proto