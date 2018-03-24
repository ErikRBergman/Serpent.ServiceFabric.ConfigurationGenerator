@echo off

del *.nupkg

for %%k in (*.csproj) DO ( 
	"c:\program files (x86)\NuGet\nuget.exe" pack %%k -tool -build -Symbols -properties Configuration=Release
	"c:\program files (x86)\NuGet\nuget.exe" pack %%k -tool -properties Configuration=Release
	)

for %%k in (*.nupkg) DO ( 
	copy %%k c:\NugetPackages
	"c:\program files (x86)\NuGet\nuget.exe" push %%k -source https://www.nuget.org -apikey 5eed0a75-e0ad-4564-97fd-bc29e6835d2f
	)

del *.nupkg

pause

