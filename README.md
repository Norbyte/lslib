LSLib
=====

This package provides utilities for manipulating Divinity Original Sin 1, Enhanced Edition, Original Sin 2 and Baldur's Gate 3 EA files:

 - Extracting/creating PAK packages
 - Extracting/creating LSV savegame packages
 - Converting LSB, LSF, LSX, LSJ resource files
 - Importing and exporting meshes and animations (conversion from/to GR2 format)
 - Editing story (OSI) databases

Requirements
============

No manual downloads are needed: the build tools restore from NuGet with the rest of the solution.

 - GPLex and GPPG (the story and stats parser generators) come from `YaccLexTools.Gplex` / `YaccLexTools.Gppg`.
 - The Protocol Buffers compiler comes from `Grpc.Tools`, which bundles `protoc` (`StoryCompiler`, `DebuggerFrontend`).

`LSLibNative` is a C++ project, so build the solution with Visual Studio's MSBuild (C++ workload installed), not the
`dotnet` CLI.
