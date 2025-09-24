LSLib
=====

This package provides utilities for manipulating Divinity Original Sin 1, Enhanced Edition, Original Sin 2 and Baldur's Gate 3 EA files:

 - Extracting/creating PAK packages
 - Extracting/creating LSV savegame packages
 - Converting LSB, LSF, LSX, LSJ resource files
 - Importing and exporting meshes and animations (conversion from/to GR2 format)
 - Editing story (OSI) databases


Installation
============
For regular users, download the latest release, unzip it, and run ConverterApp.exe.

1. On this page, look for Releases and click the one that says Latest.

   <img width="500" height="1365" alt="Image" src="https://github.com/user-attachments/assets/7e2f6d25-7cfc-4277-bdce-e280d2eb0df6" />

2. Click ExportTool.zip to download it.
   
   <img width="500" height="1007" alt="Image" src="https://github.com/user-attachments/assets/ff079263-25c6-486d-aa44-d810797c5bb8" />

3. In your downloads folder, right click the zip, and extract it to a new folder.
   
   <img width="250" height="239" alt="Image" src="https://github.com/user-attachments/assets/02afdeb2-64a9-4ec3-b5d8-f09213e56ff0" />

4. Open the new folder, open Packed, and double click ConverterApp.exe to open it.
   
   <img width="250" height="696" alt="Image" src="https://github.com/user-attachments/assets/4c03d31e-f58e-4757-a312-0c73c667efb1" />


Build From Source
=================
Once you've downloaded and extracted the latest source code, here's how to build the solution using visual studio.

1. Get the following **dependencies**:
 - Download GPLex 1.2.2 [from here](https://s3.eu-central-1.amazonaws.com/nb-stor/dos-legacy/ExportTool/gplex-distro-1_2_2.zip) and extract it to the `External\gplex\` directory
 - Download GPPG 1.5.2 [from here](https://s3.eu-central-1.amazonaws.com/nb-stor/dos-legacy/ExportTool/gppg-distro-1_5_2.zip) and extract it to the `External\gppg\` directory
 - Protocol Buffers 3.6.1 compiler [from here](https://github.com/protocolbuffers/protobuf/releases/download/v3.6.1/protoc-3.6.1-win32.zip) and extract it to the `External\protoc\` directory

2. You'll need visual studio, i.e. [Visual Studio Community 2022](https://visualstudio.microsoft.com/downloads/)

3. Your visual studio needs .NET and C++. To get it, in the visual studio installer click Modify, tick the boxes for **.NET desktop development** and **Desktop development with C++**, then install.
   
   <img width="500" height="1321" alt="Image" src="https://github.com/user-attachments/assets/b3ffd96a-4040-4dc4-8814-17341f14332b" />

4. Launch your visual studio. File > Open > Project/Solution, and find your LSTools.sln.

5. Switch from Debug to Release, then do Build > Build Solution.
   
   <img width="500" height="961" alt="Image" src="https://github.com/user-attachments/assets/34f4874c-0ea3-4108-a42e-e2be1b11d9f7" />

6. The last step is to get granny2. Releases now include it, but the latest source code doesn't, so to make the latest source code work you can either grab granny2.dll from the latest release package, or from an older source version. Copy granny2.dll into these three folders: `Release\Packed\`, `ConverterApp\`, and `ConverterApp\bin\Release\net8.0-windows\`
   
   <img width="500" height="1846" alt="Image" src="https://github.com/user-attachments/assets/0053e17e-d6ad-4804-a40f-d49a6701391d" />

7. The built executables will end up in `Release\Packed\`.
    
    <img width="600" height="1396" alt="Image" src="https://github.com/user-attachments/assets/ab4298a1-d606-48cd-ba4d-4272957572b0" />
