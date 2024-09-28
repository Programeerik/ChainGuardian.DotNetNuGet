In an era dominated by digital dependencies, the software supply chain plays a pivotal role in shaping the technology landscape. As consumers, we often download and integrate various packages to enhance the functionality of our applications. NuGet is a package manager for the Microsoft development platform. However, as we embrace the convenience of integrating third-party packages, it becomes imperative to address the lurking shadows of potential vulnerabilities in the software supply chain.

## Software Supply Chain
Software applications are no longer built entirely from custom code. Instead, they are made up of a variety of third-party components, including open-source libraries, frameworks, and modules. These components are often referred to as dependencies. The software supply chain is the process of managing these dependencies and their security risks.

This repo contains best practices and a workshop that gives you an overview and introduction of different types of supply chain attacks and how you can mitigate them.

There are two ways to get started:
- Follow the workshop and start at [01. knowing-your-dependencies.md](./workshop/00-introduction.md)
- Read my blogs about this subject:
   - [Securing Nuget's supply chain](https://www.blognet.tech/)

## Usefull links
- [.NET reproducible builds techniques](https://github.com/dotnet/reproducible-builds/tree/main/Documentation/Reproducible-MSBuild/Techniques)
- [Customize msbuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build?view=vs-2022)
