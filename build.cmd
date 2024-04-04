dotnet restore
dotnet build -c Debug --no-restore
dotnet test  -c Debug --no-build --settings .runsettings
dotnet tool restore
dotnet reportgenerator -reports:./artifacts/cob/*/coverage.cobertura.xml -targetdir:artifacts/html -reporttypes:HtmlInline
dotnet reportgenerator -reports:./artifacts/cob/*/coverage.cobertura.xml -targetdir:artifacts -reporttypes:"Cobertura;HtmlSummary;TextSummary"
dotnet pack -c Release --output ./artifacts/nuget