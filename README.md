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

To build the tools you'll need to get the following dependencies:

 - Download GPLex 1.2.2 [from here](https://s3.eu-central-1.amazonaws.com/nb-stor/dos/ExportTool/gplex-distro-1_2_2.zip) and extract it to the `External\gplex\` directory
 - Download GPPG 1.5.2 [from here](https://s3.eu-central-1.amazonaws.com/nb-stor/dos/ExportTool/gppg-distro-1_5_2.zip) and extract it to the `External\gppg\` directory
 - Protocol Buffers 3.6.1 compiler [from here](https://github.com/protocolbuffers/protobuf/releases/download/v3.6.1/protoc-3.6.1-win32.zip) and extract it to the `External\protoc\` directory
