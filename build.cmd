dotnet publish .\src\
copy src\bin\Debug\netcoreapp6.0\publish\Azurite.dll %USERPROFILE%\.azurite /y
copy src\bin\Debug\netcoreapp6.0\publish\Azurite.exe %USERPROFILE%\.azurite /y
copy src\bin\Debug\netcoreapp6.0\publish\Azurite.pdb %USERPROFILE%\.azurite /y
copy src\bin\Debug\netcoreapp6.0\publish\Azurite.deps.json %USERPROFILE%\.azurite /y
copy src\bin\Debug\netcoreapp6.0\publish\Azurite.runtimeconfig.json %USERPROFILE%\.azurite /y