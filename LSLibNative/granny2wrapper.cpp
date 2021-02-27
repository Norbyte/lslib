#pragma once

#include <Windows.h>
#include "granny2wrapper.h"

namespace LSLib {
	namespace Native {
		typedef bool(__stdcall * GrannyDecompressDataProc)(int Format, bool FileIsByteReversed, int CompressedBytesSize, void *CompressedBytes, int Stop0, int Stop1, int Stop2, void *DecompressedBytes);
		typedef void *(__stdcall * GrannyBeginFileDecompressionProc)(int Format, bool FileIsByteReversed, int DecompressedBytesSize, void *DecompressedBytes, int WorkMemSize, void *WorkMemBuffer);
		typedef bool(__stdcall * GrannyDecompressIncrementalProc)(void *State, int CompressedBytesSize, void *CompressedBytes);
		typedef bool(__stdcall * GrannyEndFileDecompressionProc)(void *State);

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
				throw gcnew System::IO::InvalidDataException("Granny2.dll is required for compressed GR2 files.");
			}

			auto decompressProc = (GrannyDecompressDataProc)GetProcAddress(hGranny, "GrannyDecompressData");
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

		array<byte> ^ Granny2Compressor::Decompress4(array<byte> ^ compressed, Int32 decompressedSize)
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
				throw gcnew System::IO::InvalidDataException("Granny2.dll is required for compressed GR2 files.");
			}

			auto beginDecompressProc = (GrannyBeginFileDecompressionProc)GetProcAddress(hGranny, "GrannyBeginFileDecompression");
			if (!beginDecompressProc)
			{
				throw gcnew System::IO::InvalidDataException("GrannyBeginFileDecompression export not found in Granny2.dll.");
			}

			auto decompressProc = (GrannyDecompressIncrementalProc)GetProcAddress(hGranny, "GrannyDecompressIncremental");
			if (!decompressProc)
			{
				throw gcnew System::IO::InvalidDataException("GrannyDecompressIncremental export not found in Granny2.dll.");
			}

			auto endDecompressProc = (GrannyEndFileDecompressionProc)GetProcAddress(hGranny, "GrannyEndFileDecompression");
			if (!endDecompressProc)
			{
				throw gcnew System::IO::InvalidDataException("GrannyEndFileDecompression export not found in Granny2.dll.");
			}

			void * workMem = malloc(0x4000);
			void * state = beginDecompressProc(4, false, decompressedSize, decomp, 0x4000, workMem);
			int pos = 0;
			while (pos < compressed->Length)
			{
				int chunkSize = min(compressed->Length - pos, 0x2000);
				bool incrementOk = decompressProc(state, chunkSize, input + pos);
				if (!incrementOk)
				{
					throw gcnew System::IO::InvalidDataException("Failed to decompress GR2 section increment.");
				}

				pos += chunkSize;
			}

			bool ok = endDecompressProc(state);
			if (!ok)
			{
				throw gcnew System::IO::InvalidDataException("Failed to finish GR2 section decompression.");
			}

			free(workMem);

			FreeModule(hGranny);
			return decompressed;
		}
	}
}
