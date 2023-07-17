using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LSLib.Native
{
public class Granny2Compressor {
	[DllImport("granny2", CharSet = CharSet.Ansi)]
	private static unsafe extern bool GrannyDecompressData(int format, bool fileIsByteReversed, int compressedBytesSize, void* compressedBytes, int stop0, int stop1, int stop2, void* decompressedBytes);

	[DllImport("granny2", CharSet = CharSet.Ansi)]
	private static unsafe extern UIntPtr GrannyBeginFileDecompression(int format, bool fileIsByteReversed, int decompressedBytesSize, void* decompressedBytes, int workMemSize, void* workMemBuffer);

	[DllImport("granny2", CharSet = CharSet.Ansi)]
	private static unsafe extern bool GrannyDecompressIncremental(void* state, int compressedBytesSize, void* compressedBytes);

	[DllImport("granny2", CharSet = CharSet.Ansi)]
	private static unsafe extern bool GrannyEndFileDecompression(void* state);

	public static byte[] Decompress(Int32 format, byte[] compressed, Int32 decompressedSize, Int32 stop0, Int32 stop1, Int32 stop2) {
		var decompressed = new byte[decompressedSize];

		bool ok;
		unsafe {
			ok = GrannyDecompressData(format, false, compressed.Length,
						  Marshal.UnsafeAddrOfPinnedArrayElement(compressed, 0).ToPointer(),
						  stop0, stop1, stop2,
						  Marshal.UnsafeAddrOfPinnedArrayElement(decompressed, 0).ToPointer());
		}
		if (!ok)  {
			throw new InvalidDataException("Failed to decompress Oodle compressed section");
		}
		return decompressed;
	}

	public static byte[] Decompress4(byte[] compressed, Int32 decompressedSize) {
		var decompressed = new byte[decompressedSize];

		var workMem = new byte[0x4000];
		UIntPtr state;

		unsafe {
			state = GrannyBeginFileDecompression(4, false, decompressedSize,
							     Marshal.UnsafeAddrOfPinnedArrayElement(decompressed, 0).ToPointer(),
							     workMem.Length,
							     Marshal.UnsafeAddrOfPinnedArrayElement(workMem, 0).ToPointer());
		}

		int pos = 0;
		while (pos < compressed.Length) {
			int chunkSize = Math.Min(compressed.Length - pos, 0x2000);
			bool incrementOk;
			unsafe {
				incrementOk = GrannyDecompressIncremental(state.ToPointer(), chunkSize, Marshal.UnsafeAddrOfPinnedArrayElement(compressed, pos).ToPointer());
			}
			if (!incrementOk) {
				throw new InvalidDataException("Failed to decompress GR2 section increment.");
			}

			pos += chunkSize;
		}

		bool ok;
		unsafe {
			ok = GrannyEndFileDecompression(state.ToPointer());
		}
		if (!ok) {
			throw new InvalidDataException("Failed to finish GR2 section decompression.");
		}

		return decompressed;
	}
}
}
