# Building the Windows Event Log Messages tool

Building WELM is fairly straightforward.

1. Install Visual Studio 2015 Update 3 or later.
1. Open the **welm.sln** file with Visual Studio.
3. If this is the first time building the project, install the following [nuget](https://www.nuget.org/) packages from the [Package Manager Console](https://docs.microsoft.com/en-us/nuget/tools/package-manager-console) in Visual Studio using these [commands](https://docs.microsoft.com/en-us/nuget/tools/powershell-reference):
    1. [docopt.net](https://www.nuget.org/packages/docopt.net/): **Get-Project WelmConsole | Install-Package docopt.net** (You may need to delete the T4 files and main.usage.txt file or you will get an exception when compiling: **An exception was thrown while trying to compile the transformation code**)
    1. [ilmerge](https://www.nuget.org/packages/ilmerge/): **Get-Project WelmConsole | Install-Package ilmerge**
    1. [CSVHelper](https://www.nuget.org/packages/CsvHelper/): **Get-Project WelmLibrary | Install-Package CsvHelper -Version 2.16.3** (This is the last version that supports .Net 4.0)
    1. [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/): **Get-Project WelmLibrary | Install-Package Newtonsoft.Json**
    1. [NLog](https://www.nuget.org/packages/NLog/): **Get-Project WelmConsole,WelmLibrary | Install-Package NLog**
    1. [NLog.Schema](https://www.nuget.org/packages/NLog.Schema/): **Get-Project WelmConsole,WelmLibrary | Install-Package NLog.Schema**
1. If this is not the first time building the project, then update the existing nuget packages using the **Update-Packages** command in the Package Manager Console in Visual Studio.
2. Build a **Release** or **Debug** configuration. A **dist** folder will be created inside the **welm** folder. When building a Release configuration and the ILmerge nuget package is installed, then the release folder will contain a single binary (welm.exe) along with welm.bat, NLog.config, and Install-Features.ps1. Copy these files to a system that you want to [run WELM on](./Running%20WELM.md).
