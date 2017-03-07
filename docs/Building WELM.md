# Building the Windows Event Log Messages tool

Building WELM is fairly straightforward.

1. Install Visual Studio 2015 Update 3 or later.
1. Open the **welm.sln** file with Visual Studio.
3. If this is the first time building the project, install the following [nuget](https://www.nuget.org/) packages from the [Package Manager Console](https://docs.microsoft.com/en-us/nuget/tools/package-manager-console) in Visual Studio using these [commands](https://docs.microsoft.com/en-us/nuget/tools/powershell-reference):
    1. [docopt.net](https://www.nuget.org/packages/docopt.net/): **Get-Project WelmConsole | Install-Package docopt.net**
    1. [ilmerge](https://www.nuget.org/packages/ilmerge/): **Get-Project WelmConsole | Install-Package ilmerge**
    1. [CSVHelper](https://www.nuget.org/packages/CsvHelper/): **Get-Project WelmLibrary | Install-Package CsvHelper**
    1. [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/): **Get-Project WelmLibrary | Install-Package Newtonsoft.Json**
    1. [NLog](https://www.nuget.org/packages/NLog/): **Get-Project WelmConsole,WelmLibrary | Install-Package NLog**
    1. [NLog.Schema](https://www.nuget.org/packages/NLog.Schema/): **Get-Project WelmConsole,WelmLibrary | Install-Package NLog.Schema**
1. If this is not the first time building the project, then update the existing nuget packages using the **Update-Packages** command in the Package Manager Console in Visual Studio.
2. Build a **Release** configuration. When building a Release configuration and the ILmerge nuget package is installed, then a **dist** folder will be created inside the **welm** folder. The dist folder will contain welm.exe, welm.bat, NLog.config, and Install-Features.ps1 in it. Copy these files to a system that you want to [run WELM on](./Running WELM.md).
