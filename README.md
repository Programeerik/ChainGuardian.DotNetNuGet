# Securing NuGets supply chain
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
dotnet tool install --global CycloneDX
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

Starting from .NET 8 (SDK 8.0.100) the `restore` command can audit([Auditing package dependencies for security vulnerabilities | Microsoft Learn](https://learn.microsoft.com/en-us/NuGet/concepts/auditing-packages)) all your 3rd party packages. The restore command does a vulnerability check on all the packages that are being restored. If you only use a private feed, like Azure DevOps artifacts, the restore command is not able to find any vulnerabilities because azure devops by default doesn't have any CVE database or information. 

With the introduction of .NET 9, the private artifact store issue is fixed. There is a new property that you can add to the `NuGet.config` file that allows you to set an audit source. By setting this property, NuGet gets its [vulnerabilityInfo resource](https://learn.microsoft.com/en-us/NuGet/api/vulnerability-info) from a different source than the configured NuGet feeds.

Run the command `dotnet restore` in the root of the repository. This will restore all the packages and do a vulnerability check on all the packages that are being restored. **Check the output of the restore command.** This will output a list of all the vulnerabilities that are present in the packages that are being restored as warnings in the console.

The console outputs only the direct dependencies that have vulnerabilities. In the .NET 8.0.100 SDK the `restore` command defaulted all settings that are required to do a vulnerability check:
- `NuGetAudit` is set to `true`
- `NuGetAuditMode` is set to `direct`
- `NuGetAuditLevel` is set to `low`

*In .NET 9.0.100 SDK value of `NuGetAuditMode` is changed to `all`

Add the following line to the `ChainGuardian.csproj` file:

```xml
<PropertyGroup>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>direct</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
</PropertyGroup>
```

See all the different options that are possible in the [Microsoft documentation](https://learn.microsoft.com/en-us/NuGet/concepts/auditing-packages#configuring-NuGet-audit) and **make sure that the restore command will output all the vulnerabilities that are present in the packages that are being restored.**

Now you have a situation where the restore command will output all the vulnerabilities that are present in the packages that are being restored. This is a good first step in securing your software supply chain. However, this is not enough. You need to make sure that you are aware of the vulnerabilities that are present in your software and that you are able to patch them.

.NET has a build in feature that can help you with that. You can make your build fail if there are vulnerabilities present in your software. To make the actual build fail when a vulnerability is found, **add the following line to your project file**:

```xml
<WarningsAsErrors>NU1901,NU1903,NU1903,NU1904</WarningsAsErrors>
```

Run the `dotnet restore` or `dotnet build` command and see that you are no longer able to create a successful build. **Now update the packages so that the build succeeds.**

### Restoring packages
Great now that we have tackled the **known** vulnerabilities in our software, let's have a look at the **hidden dangers** of the software supply chain. Before we dive into the dangers, it is good to understand how the NuGet package restore works.

Using third party packages requires you to restore them via NuGet. All project dependencies that are listed in either a project file or a packages.config file are restored.

Package restore first installs all the direct dependencies. These are the dependencies that are directly referenced between the <PackageReference> tags or <package> tags. After the direct dependencies all transitive packages are installed. By default, NuGet will first scan the local global packages or HTTP cache folders. If the required package isn't in the local folders, NuGet tries to download it.

What makes this behavior dangerous is that **NuGet ignores the order of package sources configured**. It first looks in the local NuGet cache, it the package is not present, it fires a request to each package source configured. It uses the package source that responds the quickest. This means that **by default, you don't have any control from what source you are restoring your packages**. If a restore fails, NuGet doesn't indicate the failure until after it checks all sources. NuGet reports a failure ony for the last source in the list. The error implies that the package wasn't present on any of the resources.

![Nuget Supply Chain Security](/assets/images/nuget-restore.drawio.png)

Package resolution for transitive dependencies follows a set of rules. To understand certain risks and how to mitigate them, it is important to understand how Nuget resolves transitive dependencies. Before your read further, I recommend you to read the [official documentation](https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution) on how Nuget resolves transative dependencies.

Now we are going to have a look at two different types of supply chain attacks that can happen when your packages are being restored.

#### Dependency confusion
Imagine that you work at a company that uses both a public and a private Nuget feed to retrieve the packages that are used in your software projects. You have a package that is called MyCompany.Common. You have a build pipeline that builds the package and pushes it to the private Nuget feed. Via some way of research / reverse engineering, a hacker found the name of your private image used internally (MyCompany.Common). The hacker then creates a package with the same name, version and adds some malware/backdoor to the source code and uploads it to the public Nuget feed.

Because of the way a nuget restore works (it uses the first source that responds the quickest), the malicious package from the public Nuget feed can be installed instead of the package from the private Nuget feed. This is called dependency confusion. The hacker is able to inject malicious code into your software without you knowing it. This is a big risk for your software supply chain.

For example, you are using the package MyCompany.Common with version `1.0.0` and you reference it in your project file like this <PackageReference Include="MyCompany.Common" Version="1.0.*" />. You have a connection to your own private feed and the public Nuget feed. The hacker uploads a package with the name MyCompany.Common with version 1.0.1 to the public Nuget feed. When you run a restore, NuGet will restore the package of the hacker because of the resolution rules. You can **unintentionally** introduce a new way for your software to be vulnerable for a supply chain attack. It is good to understand how [Nuget semantic versioning works](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning?tabs=semver20sort#references-in-project-files-packagereference) and what the risks are of the way you reference your packages in your project file.

##### Mitigations
**NOTE**: before you perform the next exercise make sure that you run the command `nuget locals all -clear`. This will clear you local cached packages, so that you can see the effect of the mitigations that you are going to do. If you don't run this command, NuGet will use the cached packages and you will not see the effect of the mitigations.

By default, having both a public and a private feed is a risk. I would recommend to **only use a private feed**. This way you (your company) has control over the packages that are available to your software projects. You can configure Nuget to only use the private feed. Configuring NuGet to use a certain package source is done via the [`nuget.config` file](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file).

**Exercise**: Create a `nuget.config` file in the root of the repository and configure NuGet to use the public NuGet feed.

If for some reason you use / require a public feed next to your private feed, there are a few things that you can do to mitigate the risk of dependency confusion. The first thing that you can (if you publish a package yourself) is [claim the prefix](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation) of your packages. This means that you claim the prefix of your packages on the public Nuget feed. This way, the public Nuget feed will not allow anyone to upload a package with the same prefix as your packages. This will prevent the hacker from uploading a package with the same name as your package. In the example of `MyCompany.Common`, you would claim the prefix `MyCompany`.

We are **not going** to do this in this workshop, but it is good to know that this is a possibility.

The second thing that you can do is use the `nuget.config` file to configure the package sources that you want to use. You can add a `packageSourceMapping` element to the `nuget.config` file. This element allows you to map a package source to a specific feed. This way you can configure NuGet to only use the private feed for the package `MyCompany.Common`.

**Exercise**: Add the package source mapping to the `nuget.config` file and configure NuGet to pull the required packages from the configured feed. 

A third thing that you can do is to **configure trusted signers**. This way you can configure NuGet to only accept packages that are signed by a trusted signer. This way you can make sure that the packages that are being restored are coming from a trusted source. This is a good way to mitigate the risk of dependency confusion.

**Exercise**: Configure trusted signers in the `nuget.config` file.

To protect your software from the serious risks of dependency confusion, it's essential to take proactive measures. Start by configuring Nuget to only use a private feed, ensuring control over the packages integrated into your projects. If you must use a public feed, mitigate risks by claiming your package prefix, setting up package source mapping, and configuring trusted signers. Taking these steps will significantly reduce the likelihood of malicious code infiltrating your software supply chain. Act now by reviewing and updating your Nuget configurations to safeguard your projects against these potential vulnerabilities.

#### Typosquatting
A different way to attack your software supply chain with the focus on your dependencies is by using a typosquatting attack. Typosquatting is a form of cybersquatting that relies on mistakes such as typographical errors made by users when inputting a website address into a web browser. Should a user accidentally enter an incorrect website address, they may be led to an alternative website that could contain malware, phishing scams, or other malicious content.

In .NET we have the command dotnet add package <package-name>. This command will add a package to your project. The package name that you provide is used to download the package from the Nuget feed. If you make a typo in the package name, Nuget will try to download the package with the typo in the name. This is where the risk of typosquatting comes in. A hacker can upload a package with a name that is very similar to a popular package. If you make a typo in the package name, Nuget will download the package from the hacker instead of the package from the original author. Looking at the MyCompany.Common example, the hacker can upload a package with the name MyCompany.Commonn to the public Nuget feed. If you make a typo in the package name, Nuget will download the package from the hacker.

##### Mitigations
Some of the mitigations that you did for the dependency confusion attack can also be used for the typosquatting attack.
- Use a private feed, this way you have control over the packages that are available to your software projects.
- Configure trusted signers. This way you can configure Nuget to only accept packages that are signed by a certain trusted signer.

By implementing these proactive measures, you can significantly reduce the risk of typosquatting attacks on your software supply chain. Safeguard your projects against potential vulnerabilities by reviewing and updating your Nuget configurations to protect your software from malicious code infiltration.

### Infiltrated build system
Another example that shocked the world is the [SolarWinds hack](https://www.techtarget.com/whatis/feature/SolarWinds-hack-explained-Everything-you-need-to-know). In this hack, the attackers inserted a backdoor into the SolarWinds Orion software. This backdoor was then distributed to all customers of SolarWinds. This backdoor was then used to infiltrate the networks of the customers of SolarWinds.

In the SolarWinds case, attackers got access to the build system. They inserted malware into the build environment that was used to build the SolarWinds Orion software. This malware was then used to insert a backdoor into the SolarWinds Orion software. This backdoor was then distributed to all customers of SolarWinds.

All the other attacks that are described in this workshop are about your dependencies and how you can protect yourself from them. A supply chain attack on your build environment is a different kind of attack. You're not in control of the packages that are cached locally on your build server. If a hacker gets access to your build server, they can add malicious code to your local cache. If Nuget decides to use the local cache of the build server to restore the packages, the malicious code will be used in your software.

### Mitigation
To protect your software from the serious risks of an infiltrated build system, it's essential to take proactive measures. Start by securing your build environment. Make sure that only authorized personnel have access to the build environment. Make sure that the build environment is up-to-date with the latest security patches. Make sure that the build environment is monitored for any suspicious activity.

From a .NET perspective, there are mainly two things that you can do to make that the build result on your local machine is the same as the build result on the build server.
The first thing that I would recommend is the use of a *package lock file*. With `PackageReference`, NuGet always tries to produce the same closure of package dependencies if the package reference list has not changed. However, there are a few scenarios where it may not be able to do so. Cases where the package graph may change include:
- **Package deletion**, nuget.org doesn't support package deletion, but not all package sources have this constraint.
- **Floating versions**, where the version range is not pinned to a specific version.
- **Package content mismatch**, the same version of a package may have different content on different package sources.

Starting from `Nuget.exe` 4.9 and .NET SDK 2.1.500, you can use a lock file. 
You must enable the lock file by adding the following line to your project file:
```XML
<PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

**Excersice**: Add the lock file to the project file and run the `dotnet restore` command. Check the output of the command and see that a lock file is generated. Run the `dotnet restore` command again but with the `--locked-mode` setting and see that the lock file is used to restore the packages.

This will generate a lock file with all your resolved dependencies in it. You must commit/check-in this lock file to your source control so that it is always available for `restore`.NuGet does a quick check to see if there were any changes in the package dependencies as mentioned in the project file (or dependent projects’ files) and if there were no changes, it just restores the packages mentioned in the lock file without re-evaluating the full package graph. NuGet detects a change in the defined dependencies as enumerated in the project file(s), it re-evaluates the package graph and updates the lock file to reflect the new package graph applicable for the project. This is the default mode and is useful when you are in a development environment actively modifying your package and project dependencies.

Reading this, I hear you think. If the dependency graph is re-evaluated when there are changes in the project file, how does this protect me from an infiltrated build system? Well, by default it doesn't. But you can tell the dotnet restore command to run in a `locked-mode`. You can run the `dotnet restore` command with the `--locked-mode` flag. This will make sure that the lock file is used to restore the packages and that the lock file is not updated. This way you can make sure that the packages that are restored are the same as the packages that are restored on your local machine.

Beside the `--locked-mode` flag you can also use the following configuration in your project file:
```XML
<PropertyGroup>
   <!-- assuming you have the MSBuild property (ContinuousIntegrationBuild) set to true -->
   <RestoreLockedMode Condition=<"'$(ContinuousIntegrationBuild)' == 'true'">true</RestoreLockedMode>
</PropertyGroup>
```

The second thing that you can do is setup your .NET build to be a [reproducible build / determenistic](https://reproducible-builds.org/). A reproducible build is a process where you compile the same source code with the same tools and settings, and it always produces the exact same output (like a binary or executable file). In simpler terms, it's like following a recipe to bake a cake: if you use the same ingredients and steps every time, you'll end up with the same cake each time. The key idea is that anyone who has the source code can rebuild the software and get the same result as the original, ensuring nothing has been altered or tampered with along the way. This helps verify that the software you have is trustworthy and hasn't been modified maliciously.

Reproducible builds are enabled by default in .NET SDK projects, but there is an extra property, `ContinuousIntegrationBuild` (also used in last example), that you can set to `true`. This forces the build server to normalize stored file paths.

To set the [`ContinuousIntegrationBuild`](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#continuousintegrationbuild) property to true, you can use the following setting in your project file:
```XML
<PropertyGroup Condition="'$(GITHUB_ACTIONS)' == 'true'">
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>
```

Exercise: Add the `ContinuousIntegrationBuild` property to the project file. If . Check the output of the command and see that the property is set to true.

To make your build even more reliable, [there a even more project properties](https://github.com/dotnet/reproducible-builds/blob/main/Documentation/Reproducible-MSBuild/Techniques/toc.md) that you can set. These properties are:

- `AssemblySearchPaths` - Restrict the reference assembly search path to avoid machine-specific dependencies.
```XML
<PropertyGroup>
   <AssemblySearchPaths>

   </AssemblySearchPaths>
</PropertyGroup>>
```

- `NetCoreTargetingPackRoot` - Disable msbuild's lookup of dotnetcore through default install locations. Instead, resolve from nuget feed.
```XML
<PropertyGroup>
   <NetCoreTargetingPackRoot>[UNDEFINED]</NetCoreTargetingPackRoot>
</PropertyGroup>
```

- `DisableImplicitNuGetFallbackFolder` and `DisableImplicitLibraryPacksFolder` - Disable the extra implicit nuget package caches provided by dotnetsdk.
```XML
<PropertyGroup>
   <DisableImplicitNuGetFallbackFolder>true</DisableImplicitNuGetFallbackFolder>
   <DisableImplicitLibraryPacksFolder>true</DisableImplicitLibraryPacksFolder>
</PropertyGroup>
```

All these settings are good to understand and now what they do. To make this easier for you, Microsoft created a [NuGet package 'ReproducibleBuilds'](https://github.com/dotnet/reproducible-builds/tree/main) that does all these settings for you. 

## Conclusion
In conclusion, securing your NuGet supply chain is essential to safeguarding your software projects from the growing threats of supply chain attacks. The increasing reliance on third-party packages introduces vulnerabilities that can be exploited by malicious actors, as seen in high-profile incidents like the Log4J vulnerability and SolarWinds attack. As a consumer, it's crucial to implement robust security practices, including thorough dependency management, package restoration controls, and proactive measures against dependency confusion and typosquatting.

By understanding your software’s dependency graph and automating the generation of Software Bills of Materials (SBOMs), you can gain visibility into potential risks and ensure that vulnerabilities are swiftly addressed. Additionally, leveraging tools like CycloneDX and implementing safety measures like package locking and source mapping can provide further protection.

Ultimately, staying vigilant and adopting a security-first approach in managing your NuGet packages will help you build resilient software systems. Regularly review your configurations, update your security practices, and monitor your supply chain to stay ahead of emerging threats. With these strategies in place, you can mitigate risks and maintain the integrity of your software projects.
