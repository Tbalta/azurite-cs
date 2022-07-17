dotnet publish .\src\
copy src\bin\Debug\netcoreapp3.1\publish\Azurite.dll %USERPROFILE%\.azurite /y
copy src\bin\Debug\netcoreapp3.1\publish\Azurite.exe %USERPROFILE%\.azurite /y
copy src\bin\Debug\netcoreapp3.1\publish\Azurite.pdb %USERPROFILE%\.azurite /y