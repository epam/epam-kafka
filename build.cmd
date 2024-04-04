dotnet build -c Debug
dotnet test  -c Debug --no-build --verbosity normal --settings .runsettings --logger:"console;verbosity=detailed"
dotnet tool restore
dotnet reportgenerator -reports:./artifacts/cob/*/coverage.cobertura.xml -targetdir:artifacts/html -reporttypes:HtmlInline
dotnet reportgenerator -reports:./artifacts/cob/*/coverage.cobertura.xml -targetdir:artifacts -reporttypes:"Cobertura;HtmlSummary;TextSummary"
dotnet pack -c Release --output ./artifacts/nuget