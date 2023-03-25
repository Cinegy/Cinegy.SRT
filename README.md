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

# Cinegy.Srt.Recv console example

This application is a C# program that receives a [Secure Reliable Transport (SRT)](https://en.wikipedia.org/wiki/Secure_Reliable_Transport) stream and broadcasts it as a multicast UDP transport stream.

The program sets up a SRT socket, receives data from it, and sends the received data to a UDP client. It also logs various statistics about the SRT stream, such as the elapsed time, bandwidth, unique packets received, and lost packets.

The program uses the SrtSharp NuGet package for SRT functionality and sets up a custom log handler for SRT-related logging. It provides options for configuring the SRT and UDP parameters, such as the SRT address and port, output adapter address, multicast address, and multicast port. Additionally, it supports non-blocking mode for the SRT socket.

1. `Main`: The entry point of the application, which parses the command-line arguments and calls the Run function.
1. `Run`: Sets up the SRT and UDP clients, starts the background thread, and waits for it to complete.
1. `BackgroundThread`: A background thread that receives data from the SRT socket and sends it to the UDP client.
1. `UdpSetup`: Configures and sets up the UDP client for multicasting.
1. `SrtReceiveData`: A generator function that yields received data from the SRT socket.
1. `SrtSetup`: Initializes the SRT library, configures the logging, and sets a custom log handler.
1. `SrtTearDown`: Performs cleanup of the SRT library before the application shuts down.

# Cinegy.Srt.Send console example

This C# application is a [Secure Reliable Transport (SRT)](https://en.wikipedia.org/wiki/Secure_Reliable_Transport) sender, which receives UDP multicast data and sends it over an SRT connection. It uses the SrtSharp NuGet package to handle SRT connections and operations.

1. `Main`: The starting point of the application that handles command-line inputs and waits for the user to close the program.
1. `Run`: Sets up the necessary components, starts the main process, and waits for it to finish before closing the program.
1. `BackgroundThread`: Manages the main functionality of receiving data from a UDP source and sending it over the SRT protocol while handling interruptions and errors.
1. `UpdSetup`: Configures a UDP client to receive data, sets necessary options, and joins a multicast group for data transmission.
1. `SrtAcceptSocketInNonBlockingMode`: Connects to SRT clients without waiting, returning either a valid connection or an uninitialized one if no clients are available.
1. `SrtAcquireAcceptedSocket`: Creates a listening socket for SRT clients, connects to them, and provides connected sockets one by one.
1. `SrtSetup`: Prepares the SRT library for use, configures logging settings, and determines which areas of the library to log.
1. `SrtTearDown`: Shuts down the SRT library before the application exits.
