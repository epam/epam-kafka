dotnet restore
dotnet build -c Debug --no-restore
dotnet test  -c Debug --no-build --verbosity normal --settings .runsettings 
dotnet tool restore
dotnet reportgenerator -reports:./artifacts/cob/*/coverage.cobertura.xml -targetdir:artifacts/html -reporttypes:HtmlInline
dotnet reportgenerator -reports:./artifacts/cob/*/coverage.cobertura.xml -targetdir:artifacts -reporttypes:"Cobertura;HtmlSummary"
dotnet pack -c Release -p:VersionPrefix=0-rc-local --output ./artifacts/nuget