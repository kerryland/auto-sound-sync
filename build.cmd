dotnet publish -p:PublishSingleFile=True auxmic.ui

echo 'ass.exe and friends are in auxmic.ui\bin\Debug\net7.0-windows\win-x64\publish\'

cd auxmic.ui\bin\Debug\net7.0-windows\win-x64\publish\
zip -9 ass.zip *