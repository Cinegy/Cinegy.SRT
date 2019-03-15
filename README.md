# Cinegy Managed SRT Tools 

This package provides a managed wrapper around the SRT DLL, from the SRT Alliance Project (see http://www.srtalliance.org/)

These tools are designed to help aid testing, and to provide a reference for how to bind against the core SRT library via a clean interface.

They are not currently for any kind of production - and are likely to get broken and changed. However, they are provided publically to help anyone else working to improve and use the SRT library.

All the dependent DLLs for SRT are blended into each EXE, using Costura, so you should be able to drag / drop the resulting tool to any machine with .NET 451 and the VS 2013 / VS 2015 x64 runtimes (pick the VC runtime depending on how this pack was built - by default we use VC2015).

# Auto Building

Just to make your life easier, we auto-build this using AppVeyor - here is how we are doing right now: 

Cinegy SRT Tools Build Status:

[![Build status](https://ci.appveyor.com/api/projects/status/e74bhgj9ywocnwr4?svg=true)](https://ci.appveyor.com/project/cinegy/cinegy-srt)

You can check out the latest compiled binary from the master or pre-master code here:

[AppVeyor Cinegy SRT Project Builder](https://ci.appveyor.com/project/cinegy/cinegy-srt/build/artifacts)

We forked the main SRT library here, https://github.com/Cinegy/srt, mainly to allow us to strap some automatic building steps to the core which we can use to then build these downstream tools. Grab binaries from this fork from this location.

