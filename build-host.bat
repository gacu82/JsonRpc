dotnet restore &&^
dotnet build src\JsonRpc.Host\project.json --configuration Release &&^
dotnet test test\JsonRpc.Tests\project.json