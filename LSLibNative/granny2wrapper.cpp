#pragma once

#include <Windows.h>
#include "granny2wrapper.h"

namespace LSLib {
	namespace Native {
		typedef bool(* __stdcall GrannyDecompressDataProc)(int Format, bool FileIsByteReversed, int CompressedBytesSize, void *CompressedBytes, int Stop0, int Stop1, int Stop2, void *DecompressedBytes);

		array<byte> ^ Granny2Compressor::Decompress(Int32 format, array<byte> ^ compressed, Int32 decompressedSize, Int32 stop0, Int32 stop1, Int32 stop2)
		{
			pin_ptr<byte> inputPin(&compressed[compressed->GetLowerBound(0)]);
			byte * input = inputPin;

			array<byte> ^ decompressed = gcnew array<byte>(decompressedSize);
			pin_ptr<byte> decompPtr(&decompressed[decompressed->GetLowerBound(0)]);
			byte * decomp = decompPtr;

			// Load Granny2 library
			HMODULE hGranny = LoadLibraryA("granny2.dll");
			if (!hGranny)
			{
				throw gcnew System::IO::InvalidDataException("Granny2.dll is required for Oodle0/Oodle1 compressed GR2 files.");
			}

			auto decompressProc = (GrannyDecompressDataProc)GetProcAddress(hGranny, "_GrannyDecompressData@32");
			if (!decompressProc)
			{
				throw gcnew System::IO::InvalidDataException("GrannyDecompressData export not found in Granny2.dll.");
			}

			bool ok = decompressProc(format, false, compressed->Length, input, stop0, stop1, stop2, decomp);
			if (!ok)
			{
				throw gcnew System::IO::InvalidDataException("Failed to decompress Oodle compressed section.");
			}

			FreeModule(hGranny);
			return decompressed;
		}
	}
}
