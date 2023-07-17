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


	public static byte[] Compress(byte[] compressed) {
		UIntPtr cctx;
		UIntPtr error;
		unsafe {
			 error = LZ4F_createCompressionContext(&cctx, LZ4F_VERSION);
		}
		unsafe {
			 error = LZ4F_freeCompressionContext(cctx);
		}
		if (LZ4F_isError(error) != 0) {
			throw new Exception("Failed to create LZ4 compression context");
		}

		return compressed;
	}
	public static byte[] Decompress(byte[] input) {
		UIntPtr dctx;
		UIntPtr error;
		unsafe {
			 error = LZ4F_createDecompressionContext(&dctx, LZ4F_VERSION);
		}
		if (LZ4F_isError(error) != 0) {
			throw new Exception("Failed to create LZ4 decompression context");
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
				throw new Exception("LZ4 decompression failed: " + Marshal.PtrToStringAnsi(LZ4F_getErrorName(result)));
			}

			inputOffset += (long)inputAvailable;
			outputOffset += (long)outputFree;

			if (inputAvailable == 0) {
				throw new Exception("LZ4 error: Not all input data was processed (input might be truncated or corrupted?)");
			}
		}

		unsafe {
			LZ4F_freeDecompressionContext(dctx);
		}

		return output;
	}
}

public class Granny2Compressor {
	public static byte[] Decompress(Int32 format, byte[] compressed, Int32 decompressedSize, Int32 stop0, Int32 stop1, Int32 stop2) {
		throw new NotSupportedException("GR2 decompression is currently unsupported");
		return compressed;
	}

	public static byte[] Decompress4(byte[] compressed, Int32 decompressedSize) {
		throw new NotSupportedException("GR2 decompression is currently unsupported");
		return compressed;
	}
}
}
