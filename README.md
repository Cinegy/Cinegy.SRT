# Cinegy Managed SRT Tools 

These tools are built to demonstrate how to use SrtSharp - the SRT C# wrapper library produced to make managed code interop with dotnet easier.

The tools are not designed for production use, but for demonstration of concepts and to provide an easy way to get started playing with SRT for people with a background in C#.

There is a Dockerfile that also provides a demonstration of how a container version of the project can be built and execute on 64-bit Linux too.

# Auto Building

Just to make your life easier, we auto-build this using AppVeyor - here is how we are doing right now: 

Cinegy SRT Tools Build Status:

[![Build status](https://ci.appveyor.com/api/projects/status/e74bhgj9ywocnwr4?svg=true)](https://ci.appveyor.com/project/cinegy/cinegy-srt)

You can check out the latest compiled binary from the master or pre-master code here:

[AppVeyor Cinegy SRT Project Builder](https://ci.appveyor.com/project/cinegy/cinegy-srt/build/artifacts)

This does disappear after a period of time

We forked the main SRT library here, https://github.com/Cinegy/srt, mainly to allow us to strap some automatic building steps to the core which we can use to then build these downstream tools. Grab binaries from this fork from this location.

