# ChainGuardian.DotNetNuGet
- What is a software supply chain
    - everything that goes into your software, and where it comes from. the dependencies and properties of your dependencies.

- Risks as a consumer
    - malicious code is purposefully added to a package
    - typos 
    - multiple package sources
        - async call to all sources, 
        - https://medium.com/@alex.birsan/dependency-confusion-4a5d60fec610

Manage your dependencies
- Package sources
- Package source mapping
    - Github code space
        - Test if nuget config is automatic created if none is present.
        - Saw this going wrong in the github code space.
- Trusted signers
    - https://learn.microsoft.com/en-us/nuget/consume-packages/installing-signed-packages
- Lock files

Supply Chain security in the pipeline

- Vulnerability scan
- GitHub vulnerable dependencies

