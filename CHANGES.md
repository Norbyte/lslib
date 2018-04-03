v1.9.1 (beta)
---
- Added support for Osiris v1.12
- Change extraction path to user-set destination by default (cli)
- Package extraction UI improvements
- Change default package version when selecting game version
- Fixed crash during cli resource conversion (thanks @fireundubh)
- Fix loading of LSJ files where the translated string key handle precedes the type
- GR2: Fixed possible crash during commandline GR2 conversion
- GR2: Improved GR2 frame interpolation and trivial frame elimination to reduce animation jitter
- GR2: Fixed export of multi-track animations where bones weren't in lexical order
- GR2: Fix import of Collada files without a scene node
- GR2: Add support for version 6 Granny files
- GR2: Fix incorrect section alignments
- GR2: Write correct user-defined properties for rigid models

v1.9.0 (2018 January 24)
---
- Add CLI (thanks @fireundubh)
- Significant amount of code refactoring (thanks @fireundubh)
- Add debug info export to Osiris parser
- Add fancier error message when extracting something that is not a package
- GR2: Fix incorrect animation interpolation
- GR2: Warn when a model has multiple root bones
- GR2: Always canonicalize rotation quaternions
- GR2: Allow name mismatches when conforming GR2 files with 1 skeleton
- GR2: Disallow loading Collada files with lines in the geometry
- GR2: Fix > 64 characters long source names in Collada exports
- GR2: Improve handling of Collada files with uncommon stride configurations
- GR2: Fix handling of models without UV or normals
- GR2: Add PTT322, PNTT3322 vertex formats
- GR2: Re-enable dummy skeleton rebuild
- GR2: Allow importing a skeleton when the animation GR2 doesn't contain one

v1.8.6 (2017 September 27)
---
- Fix potential memory leaks during package compression/extraction
- Fix CRC computation of zero-length packaged files
- Fix saving of savegame story databases
- GR2: Fix null reference exception if asset unit is missing
 
v1.8.4 (2017 September 25)
---
- Add TranslatedFSString support
 
v1.8.3 (2017 September 16)
---
- Compute archive hashes when saving
- Fix tree serialization bugs when saving LSJ
- Fix bogus matrix/vector serialization in LSJ
- Prefer Zlib compression when creating savegame packages

v1.8.2 (2017 September 14)
---
- Added support for D:OS 2 attribute formats in LSF/LSB/LSJ/LSX files
- Fixed Osi serialization bugs for D:OS EE and 2

v1.8.1 (2017 July 30)
---
- Add savegame story database modification support (EXPERIMENTAL)
- Speed up package compression/decompression
- OSI: Fix crash during story saving
- OSI: Fix handling of aliased types
- OSI: Fix loading/saving of v1.10 stories
- Add JSON exporters for Mat/Vec/IVec[2..4]
- GR2: Add option for flipping V channel of UV maps. GR2 requires the V channel to be flipped (1.0 - V) compared to "standard" UV maps; this option is enabled by default
- GR2: Fix import of broken 3ds Max exports with references to nonexistent animations/skeletons
- GR2: Fix missing bone weights (library_controllers) in DAE export
- GR2: Fix "Export normals/tangents/UVs" options
- GR2: Fix crash when VertexComponentNames is empty or missing
- GR2: Use vertex data instead of VertexComponentNames (which is unreliable)
- GR2: Add support for 3ds Max 2017 DAE exports
- GR2: Fix serialization of VariantReference types
- GR2: Fix assertion failure when the last member of a struct is garbled
- GR2: Add support for vertex format PNGBTT34322

v1.8.0 (2017 April 15)
---
- Added support for story file formats up to v1.11 (D:OS2)
- Added support for saving story files (experimental)
- Added LSJ (JSON) load/save support
- Add support for D:OS2 UUID values in LSX/LSF/LSB/LSJ files
- File timestamp is now loaded correctly from LSX files
- Fix import of hexadecimal LSX integer values
- Fix (de)compression of zero-length LSF streams
- GR2: Fixed UV export for models with "UVChannel_X" or "mapX" channel names

v1.7.0 (2016 September 14)
---
- Add support for D:OS 2 (LSFv3 format)
- GR2: Interpolate missing orientation/position/scale keyframes during export
- GR2: Fix incorrect animation duration when no rotation is applied

v1.6.5 (2016 September 11)
---
- GR2: Add batch GR2/DAE conversion feature
- GR2: Add support for animation DaKeyframes32f curves
- GR2: Add PWNGBDT3433342 and PWNGBTT3433322 vertex formats
- GR2: Fix dummy skeleton build when source GR2 has no models (only skeleton)
- GR2: Fix dummy model generation when model has no mesh bindings
- GR2: Fix a bug where models were sometimes duplicated in the output
- GR2: Fix multiple issues with export of animation transforms
- GR2: Fix twitching when exporting rotation anim curves
- Remove LSF exporter leftover debug code

(Changelog not maintained for older versions)