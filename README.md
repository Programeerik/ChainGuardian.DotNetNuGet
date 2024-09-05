# Securing Nugets supply chain
In an era dominated by digital dependencies, the software supply chain plays a pivotal role in shaping the technology landscape. As consumers, we often download and integrate various packages to enhance the functionality of our applications. NuGet is a package manager for the Microsoft development platform. However, as we embrace the convenience of integrating third-party packages, it becomes imperative to address the lurking shadows of potential vulnerabilities in the software supply chain.

## Software Supply Chain
Software applications are no longer built entirely from custom code. Instead, they are made up of a variety of third-party components, including open-source libraries, frameworks, and modules. These components are often referred to as dependencies. The software supply chain is the process of managing these dependencies and their security risks.

## Prerequisites
- Visual Studio (code)
- [.Net SDK installed](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks)
- Docker desktop / rancher installed locally
- Have a [github.com](www.github.com) account
- Have this repository cloned on your local machine

## Workshop
In this workshop, you will learn how to secure your NuGet packages and mitigate potential security risks in your software supply chain.

### Knowing what is in you software
Open the file `./src/ChainGuardian/ChainGuardian.csproj` in your code editor. This file contains the project top level dependencies. These dependencies are the NuGet packages that your application relies on. To list all these packages, run the following command in the terminal:

```powershell
dotnet list package
```

These dependencies are just your top-level dependencies. Your supply chain consist of much more packages than just the packages that are defined in your project file. To get a full overview of all the packages that are used in your application, run the `dotnet list package` command with the `--include-transitive` flag:

```powershell
dotnet list package --include-transitive
```

See the entire [`dotnet list` command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-list-package) for all the other possibilities.

#### SBOM
If your software is running in production the list package command is never ran for that software. To be able to monitor and document all the dependencies that you have in your running software, you can create a **bill of material** file. This file contains a nested description of software artifact components and metadata. This information can also include licensing information, persistent references, and other auxiliary information. This file can than be used to monitor all dependencies and licenses for your software.

To get a software bill of material, there are several tools that you can use. For this workshop we will use the [dotnet CycloneDX tool](https://github.com/CycloneDX/cyclonedx-dotnet). This tool supports the CycloneDX format. There are multiple SBOM formats, but the two most populair ones are CycloneDX and SPDX. These formats contain overlapping information. However, the two formats have tradionally two different use cases within the software development lifecycle. The SPDX was primarily designed as a way to manage open-source software liceness and share information about the packages. CycloneDX allows users to create SBOMs that provide detailed information about all components like dependencies. [To see a full comparison, I would recommend this blog.](https://scribesecurity.com/blog/spdx-vs-cyclonedx-sbom-formats-compared/)

Install the CycloneDX tool:

```powershell
dotnet tool install --global CycloneDX`
```

Generate the SBOM file for the Chainguardian solution:

```powershell
dotnet-cyclonedx .\ChainGuardian.DotNetNuGet.sln
```

This will generate a file called `bom.xml` in the root of the repository. Open this file and inspect the content of it.

### Vulnerabilities
If you include software of which you don't know the origin, you are exposed to the risk of including malicious code in your software. There can be vulnerabilities in the package that could be exploited and be used as a backdoor to harm your environment.

Looking at recent history, there a examples of malicious code having a big impact on the world of software development. One of the most famous examples was the finding of a vulnerability in Log4J, an open-source logging library that is widely used by apps and services across the internet. Exploiting this vulnerability, attackers could break into systems, steal passwords and logins, extract data and infect networks.

The `dotnet list` command has an option that you can list all vulnerable packages.

Run `dotnet list package --vulnerable`, this will output all vulnerabilities that are present in the project. The output will say that there are no vulnerabilities present in the project. This is because the list command by default doesn't look at all transitive packages like you saw before.

To get all vulnerabilities, including your transitive packages, run `dotnet list package --include-transitive --vulnerable`. This result will say that there is a vulnerability in the transitive package `System.Text.Json` version that is used.

The output of these commands tell you a few different things:
- In what package (top level or transitive) the vulnerability is present
- What the impact/severity of the vulnerability is
- A link to the CVE (Common Vulnerabilities and Exposures) database where you can find more information about the vulnerability

Open one of the links from the vulnerabilities that are present in your project. This will give you more information about the vulnerability and how to mitigate it. **Do not patch this vulnerability yet**, we will do this later in the workshop.

Starting from .NET 8 (SDK 8.0.100) the `restore` command can audit([Auditing package dependencies for security vulnerabilities | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)) all your 3rd party packages. The restore command does a vulnerability check on all the packages that are being restored. If you only use a private feed, like Azure DevOps artifacts, the restore command is not able to find any vulnerabilities because azure devops by default doesn't have any CVE database or information. 

With the introduction of .NET 9, the private artifact store issue is fixed. There is a new property that you can add to the `nuget.config` file that allows you to set an audit source. By setting this property, NuGet gets its [vulnerabilityInfo resource](https://learn.microsoft.com/en-us/nuget/api/vulnerability-info) from a different source than the configured NuGet feeds.

Run the command `dotnet restore` in the root of the repository. This will restore all the packages and do a vulnerability check on all the packages that are being restored. **Check the output of the restore command** This will output a list of all the vulnerabilities that are present in the packages that are being restored as warnings in the console.

The console outputs only the direct dependencies that have vulnerabilities. In the .NET 8.0.100 SDK the `restore` command defaulted all settings that are required to do a vulnerability check:
- `NuGetAudit` is set to `true`
- `NuGetAuditMode` is set to `direct`*
- `NuGetAuditLevel` is set to `low`

*In .NET 9.0.100 SDK value of `NugetAuditMode` is changed to `all`

Add the following line to the `ChainGuardian.csproj` file:

```xml
<PropertyGroup>
    <NuGetAudit>true</NuGetAudit>
    <NugetAuditMode>direct</NugetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
</PropertyGroup>
```

See all the different options that are possible in the [Microsoft documentation](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#configuring-nuget-audit) and **make sure that the restore command will output all the vulnerabilities that are present in the packages that are being restored.**

Now you have a situation where the restore command will output all the vulnerabilities that are present in the packages that are being restored. This is a good first step in securing your software supply chain. However, this is not enough. You need to make sure that you are aware of the vulnerabilities that are present in your software and that you are able to patch them.

.NET has a build in feature that can help you with that. You can make your build fail if there are vulnerabilities present in your software. To make the actual build fail when a vulnerability is found, **add the following line to your project file**:

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Run the `dotnet restore` or `dotnet build` command and see that you are no longer able to create a succesfull build. Now update the packages so that the build succeeds.

### Restoring packages


#### Dependency confusion
