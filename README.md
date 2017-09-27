# Cinegy Managed SRT Tools 

This package provides a managed wrapper around the SRT DLL, from the SRT Alliance Project (see http://www.srtalliance.org/)

These tools are designed to help aid testing, and to provide a reference for how to bind against the core SRT library via a clean interface.

They are not currently for any kind of production - and are likely to get broken and changed. However, they are provided publically to help anyone else working to improve and use the SRT library.

# Auto Building

Just to make your life easier, we auto-build this using AppVeyor - here is how we are doing right now: 

[![Build status](https://ci.appveyor.com/api/projects/status/fixme/branch/master?svg=true)](https://ci.appveyor.com/project/cinegy/cinegy.srt/branch/master)

You can check out the latest compiled binary from the master or pre-master code here:

[AppVeyor Cinegy SRT Project Builder](https://ci.appveyor.com/project/cinegy/cinegy.srt/build/artifacts)

We forked the main SRT library here, https://github.com/Cinegy/srt, mainly to allow us to strap some automatic building steps to the core which we can use to then build these downstream tools.

If you want some SRT libraries pre-built and with the required PThreads and OpenSSL libraries and includes, you can see these here:

[![Build status](https://ci.appveyor.com/api/projects/status/ko7tpaaxyn4d5dnt?svg=true)](https://ci.appveyor.com/project/cinegy/srt)

[AppVeyor SRT Project Builder](https://ci.appveyor.com/project/cinegy/srt/build/artifacts)