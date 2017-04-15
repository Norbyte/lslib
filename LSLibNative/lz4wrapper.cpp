#pragma once

#include "lz4wrapper.h"

namespace LSLib {
	namespace Native {
		array<byte> ^ LZ4FrameCompressor::Compress(array<byte> ^ input)
		{
			// Initialize LZ4 compression
			LZ4F_compressionContext_t cctx;
			auto error = LZ4F_createCompressionContext(&cctx, LZ4F_VERSION);
			if (LZ4F_isError(error))
			{
				throw gcnew System::IO::InvalidDataException("Failed to create LZ4 compression context");
			}

			std::vector<byte> output(0x10000);
			size_t inputOffset = 0, outputOffset = 0;

			LZ4F_preferences_t preferences;
			preferences.frameInfo.blockSizeID = max64KB;
			preferences.frameInfo.blockMode = blockLinked;
			preferences.frameInfo.contentChecksumFlag = noContentChecksum;
			preferences.frameInfo.frameType = LZ4F_frame;
			// We _do_ know the content size here, but specify "unknown" size for LS compatibility.
			preferences.frameInfo.contentSize = 0;
			preferences.compressionLevel = 9;
			preferences.autoFlush = 1;

			LZ4F_compressOptions_t options;
			options.stableSrc = 1;

			auto headerSize = LZ4F_compressBegin(cctx, output.data(), output.size(), &preferences);
			if (LZ4F_isError(headerSize))
			{
				auto errmsg = std::string("Could not write LZ4 frame headers: ") + LZ4F_getErrorName(headerSize);
				throw gcnew System::IO::InvalidDataException(msclr::interop::marshal_as<System::String ^>(errmsg));
			}

			outputOffset += headerSize;

			if (input->Length)
			{
				pin_ptr<byte> inputPin(&input[input->GetLowerBound(0)]);
				byte * inputPtr = inputPin;

				// Process input in 0x10000 byte chunks
				while (inputOffset < input->Length)
				{
					size_t chunkSize = input->Length - inputOffset;
					if (chunkSize > 0x10000) chunkSize = 0x10000;

					// Keep the required number of bytes free in the output array
					size_t requiredSize = LZ4F_compressBound(chunkSize, &preferences);
					size_t outputFree = output.size() - outputOffset;
					if (outputFree < requiredSize)
					{
						output.resize(output.size() + (requiredSize - outputFree));
						outputFree = output.size() - outputOffset;
					}

					// Process the next input chunk
					auto bytesWritten = LZ4F_compressUpdate(cctx, output.data() + outputOffset, outputFree, inputPtr + inputOffset, chunkSize, &options);
					if (LZ4F_isError(bytesWritten))
					{
						auto errmsg = std::string("LZ4 compression failed: ") + LZ4F_getErrorName(bytesWritten);
						throw gcnew System::IO::InvalidDataException(msclr::interop::marshal_as<System::String ^>(errmsg));
					}

					inputOffset += chunkSize;
					outputOffset += bytesWritten;
				}
			}

			// LZ4F_compressEnd needs at most 8 free bytes.
			size_t outputFree = output.size() - outputOffset;
			if (outputFree < 8)
			{
				output.resize(output.size() + (8 - outputFree));
				outputFree = output.size() - outputOffset;
			}

			auto bytesWritten = LZ4F_compressEnd(cctx, output.data() + outputOffset, outputFree, &options);
			if (LZ4F_isError(bytesWritten))
			{
				auto errmsg = std::string("Failed to finish LZ4 compression: ") + LZ4F_getErrorName(bytesWritten);
				throw gcnew System::IO::InvalidDataException(msclr::interop::marshal_as<System::String ^>(errmsg));
			}

			outputOffset += bytesWritten;

			// Copy the output to a managed array
			LZ4F_freeCompressionContext(cctx);
			array<byte> ^ compressed = gcnew array<byte>(outputOffset);
			pin_ptr<byte> compPtr(&compressed[compressed->GetLowerBound(0)]);
			byte * comp = compPtr;
			memcpy(comp, output.data(), outputOffset);
			return compressed;
		}

		array<byte> ^ LZ4FrameCompressor::Decompress(array<byte> ^ compressed)
		{
			pin_ptr<byte> inputPin(&compressed[compressed->GetLowerBound(0)]);
			byte * input = inputPin;

			// Initialize LZ4 decompression
			LZ4F_decompressionContext_t dctx;
			auto error = LZ4F_createDecompressionContext(&dctx, LZ4F_VERSION);
			if (LZ4F_isError(error))
			{
				throw gcnew System::IO::InvalidDataException("Failed to create LZ4 decompression context");
			}

			std::vector<byte> output;
			size_t inputOffset = 0, outputOffset = 0;
			while (inputOffset < compressed->Length)
			{
				size_t outputFree = output.size() - outputOffset;

				// Always keep ~0x10000 bytes free in the decompression output array.
				if (outputFree < 0x10000)
				{
					output.resize(output.size() + (0x10000 - outputFree));
					outputFree = output.size() - outputOffset;
				}

				size_t inputAvailable = compressed->Length - inputOffset;
				// Process the next LZ4 frame
				// outputFree contains the number of bytes written, inputAvailable contains the number of bytes processed
				auto result = LZ4F_decompress(dctx, output.data() + outputOffset, &outputFree, input + inputOffset, &inputAvailable, nullptr);
				if (LZ4F_isError(result))
				{
					auto errmsg = std::string("LZ4 decompression failed: ") + LZ4F_getErrorName(result);
					throw gcnew System::IO::InvalidDataException(msclr::interop::marshal_as<System::String ^>(errmsg));
				}

				inputOffset += inputAvailable;
				outputOffset += outputFree;

				if (inputAvailable == 0)
				{
					// No bytes were processed but the whole input stream was passed to LZ4F_decompress; possible corruption?
					throw gcnew System::IO::InvalidDataException("LZ4 error: Not all input data was processed (input might be truncated or corrupted?)");
				}
			}
			
			// Copy the output to a managed array
			LZ4F_freeDecompressionContext(dctx);
			array<byte> ^ decompressed = gcnew array<byte>(outputOffset);
			if (outputOffset > 0)
			{
				pin_ptr<byte> decompPtr(&decompressed[decompressed->GetLowerBound(0)]);
				byte * decomp = decompPtr;
				memcpy(decomp, output.data(), outputOffset);
			}

			return decompressed;
		}
	}
}
