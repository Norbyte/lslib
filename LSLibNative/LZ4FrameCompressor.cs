using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LSLib.Native
{
public class LZ4FrameCompressor {
	private const uint LZ4F_VERSION = 100;

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_createCompressionContext(UIntPtr* cctxPtr, uint version);

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_freeCompressionContext(UIntPtr cctx);

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_createDecompressionContext(UIntPtr* dctx, uint version);

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_freeDecompressionContext(UIntPtr dctx);

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static extern uint LZ4F_isError(UIntPtr code);

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static extern IntPtr LZ4F_getErrorName(UIntPtr code);

	[StructLayout(LayoutKind.Explicit, Size=16, CharSet=CharSet.Ansi)]
	private struct LZ4F_decompressOptions_t {
		[FieldOffset(0)]public uint stableDst;
		[FieldOffset(4)]public uint skipChecksums;
		[FieldOffset(8)]public uint reserved1;
		[FieldOffset(12)]public uint reserved0;
	}

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_decompress(UIntPtr dctx, byte* dstBuffer, ulong* dstSizePtr, byte* srcBuffer, ulong* srcSiePtr, [MarshalAs(UnmanagedType.LPStruct)]LZ4F_decompressOptions_t* dOptPtr);

	private const uint LZ4F_default = 0;
	private const uint LZ4F_max64KB = 4;
	private const uint LZ4F_max256KB = 5;
	private const uint LZ4F_max1MB = 6;
	private const uint LZ4F_max4MB = 7;

	private const uint LZ4F_blockLinked = 0;
	private const uint LZ4F_blockIndependent = 1;

	private const uint LZ4F_noContentChecksum = 0;
	private const uint LZ4F_contentChecksumEnabled = 1;

	private const uint LZ4F_frame = 0;
	private const uint LZ4F_skippableFrame = 1;



	[StructLayout(LayoutKind.Explicit, Size=32, CharSet=CharSet.Ansi)]
	private struct LZ4F_frameInfo_t {
		[FieldOffset(0)]public uint blockSizeID;
		[FieldOffset(4)]public uint blockMode;
		[FieldOffset(8)]public uint contentChecksumFlag;
		[FieldOffset(12)]public uint frameType;
		[FieldOffset(16)]public ulong contentSize;
		[FieldOffset(24)]public uint dictID;
		[FieldOffset(28)]public uint blockChecksumFlag;
	}

	[StructLayout(LayoutKind.Explicit, Size=56, CharSet=CharSet.Ansi)]
	private struct LZ4F_preferences_t {
		[FieldOffset(0)]public LZ4F_frameInfo_t frameInfo;
		[FieldOffset(32)]public int compressionLevel;
		[FieldOffset(36)]public uint autoFlush;
		[FieldOffset(40)]public uint favorDecSpeed;
		[FieldOffset(44)]public uint reserved0;
		[FieldOffset(48)]public uint reserved1;
		[FieldOffset(52)]public uint reserved2;
	}

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_compressBegin(UIntPtr cctx, void* dstBuffer, ulong dstCapacity, LZ4F_preferences_t* prefsPtr);

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_compressBound(ulong srcSize, LZ4F_preferences_t* prefsPtr);

	[StructLayout(LayoutKind.Explicit, Size=16, CharSet=CharSet.Ansi)]
	private struct LZ4F_compressOptions_t {
		[FieldOffset(0)]public uint stableSrc;
		[FieldOffset(4)]public uint reserved0;
		[FieldOffset(8)]public uint reserved1;
		[FieldOffset(12)]public uint reserved2;
	}

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_compressUpdate(UIntPtr cctx, void* dstBuffer, ulong dstCapacity, void* srcBuffer, ulong srcSize, LZ4F_compressOptions_t* cOptPtr);

	[DllImport("liblz4", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr LZ4F_compressEnd(UIntPtr cctx, void* dstBuffer, ulong dstCapacity, LZ4F_compressOptions_t* cOptPtr);


	public static byte[] Compress(byte[] input) {
		/* TODO: Not tested */
		UIntPtr cctx;
		UIntPtr error;
		unsafe {
			 error = LZ4F_createCompressionContext(&cctx, LZ4F_VERSION);
		}
		if (LZ4F_isError(error) != 0) {
			throw new InvalidDataException("Failed to create LZ4 compression context");
		}
		var output = new byte[0x10000];
		ulong inputOffset = 0, outputOffset = 0;

		LZ4F_preferences_t preferences;
		preferences.frameInfo.blockSizeID = LZ4F_max64KB;
		preferences.frameInfo.blockMode = LZ4F_blockLinked;
		preferences.frameInfo.contentChecksumFlag = LZ4F_noContentChecksum;
		preferences.frameInfo.frameType = LZ4F_frame;
		// We _do_ know the content size here, but specify "unknown" size for LS compatibility.
		preferences.frameInfo.contentSize = 0;
		preferences.compressionLevel = 9;
		preferences.autoFlush = 1;

		LZ4F_compressOptions_t options;
		options.stableSrc = 1;

		UIntPtr headerSize;
		unsafe {
			headerSize = LZ4F_compressBegin(cctx,
					Marshal.UnsafeAddrOfPinnedArrayElement(output, 0).ToPointer(),
					(ulong)output.Length, &preferences);
		}
		if (LZ4F_isError(headerSize) != 0) {
			throw new InvalidDataException("Could not write LZ4 frame headers: " + LZ4F_getErrorName(headerSize));
		}

		outputOffset += (ulong)headerSize;

		if (input.Length > 0) {
			// Process input in 0x10000 byte chunks
			while (inputOffset < (ulong)input.Length) {
				ulong chunkSize = (ulong)input.Length - inputOffset;

				if (chunkSize > 0x10000) chunkSize = 0x10000;

				// Keep the required number of bytes free in the output array
				UIntPtr requiredSize;
				unsafe {
					requiredSize = LZ4F_compressBound(chunkSize, &preferences);
				}
				ulong outputFree = (ulong)output.Length - outputOffset;
				if (outputFree < (ulong)requiredSize) {
					Array.Resize(ref output, output.Length + (int)((ulong)requiredSize - outputFree));
					outputFree = (ulong)output.Length - outputOffset;
				}

				// Process the next input chunk
				UIntPtr bytesWritten;
				unsafe {
					bytesWritten = LZ4F_compressUpdate(cctx,
							Marshal.UnsafeAddrOfPinnedArrayElement(output, (int)outputOffset).ToPointer(),
							outputFree,
							Marshal.UnsafeAddrOfPinnedArrayElement(input, (int)inputOffset).ToPointer(),
							chunkSize, &options);
				}
				if (LZ4F_isError(bytesWritten) != 0) {
					throw new InvalidDataException("LZ4 compression failed: " + LZ4F_getErrorName(bytesWritten));
				}

				inputOffset += chunkSize;
				outputOffset += (ulong)bytesWritten;
			}
		}

		// LZ4F_compressEnd needs at most 8 free bytes.
		ulong outputFreeEnd = (ulong)output.Length - outputOffset;
		if (outputFreeEnd < 8)
		{
			Array.Resize(ref output, output.Length + (8 - (int)outputFreeEnd));
			outputFreeEnd = (ulong)output.Length - outputOffset;
		}

		UIntPtr bytesWrittenEnd;
		unsafe {
			bytesWrittenEnd = LZ4F_compressEnd(cctx,
					Marshal.UnsafeAddrOfPinnedArrayElement(output, (int)outputOffset).ToPointer(),
					outputFreeEnd, &options);
		}
		if (LZ4F_isError(bytesWrittenEnd) != 0) {
			throw new InvalidDataException("Failed to finish LZ4 compression: " + LZ4F_getErrorName(bytesWrittenEnd));
		}

		outputOffset += (ulong)bytesWrittenEnd;

		unsafe {
			 error = LZ4F_freeCompressionContext(cctx);
		}

		Array.Resize(ref output, (int)outputOffset);

		return output;
	}
	public static byte[] Decompress(byte[] input) {
		UIntPtr dctx;
		UIntPtr error;
		unsafe {
			 error = LZ4F_createDecompressionContext(&dctx, LZ4F_VERSION);
		}
		if (LZ4F_isError(error) != 0) {
			throw new InvalidDataException("Failed to create LZ4 decompression context");
		}

		byte[] output = {};
		long inputOffset = 0, outputOffset = 0;

		while (inputOffset < input.Length) {
			ulong outputFree = (ulong)(output.Length - outputOffset);

			// Always keep ~0x10000 bytes free in the decompression output array.
			if (outputFree < 0x10000) {
				Array.Resize(ref output, (int)(output.Length + (int)(0x10000ul - outputFree)));
				outputFree = (ulong)(output.Length - outputOffset);
			}

			ulong inputAvailable = (ulong)(input.Length - inputOffset);

			UIntPtr result;
			unsafe {
				result = LZ4F_decompress(dctx,
						(byte*)Marshal.UnsafeAddrOfPinnedArrayElement(output, (int)outputOffset).ToPointer(), &outputFree,
						(byte*)Marshal.UnsafeAddrOfPinnedArrayElement(input, (int)inputOffset).ToPointer(), &inputAvailable,
						null);
			}
			if (LZ4F_isError(result) != 0) {
				throw new InvalidDataException("LZ4 decompression failed: " + Marshal.PtrToStringAnsi(LZ4F_getErrorName(result)));
			}

			inputOffset += (long)inputAvailable;
			outputOffset += (long)outputFree;

			if (inputAvailable == 0) {
				throw new InvalidDataException("LZ4 error: Not all input data was processed (input might be truncated or corrupted?)");
			}
		}

		unsafe {
			LZ4F_freeDecompressionContext(dctx);
		}

		return output;
	}
}
}
