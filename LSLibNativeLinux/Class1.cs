using System;

namespace LSLib.Native
{
public class Crc32
{
	static UInt32[,] Crc32Lookup = new UInt32[8, 0x100];
	static bool Crc32LookupTableInitialized = false;

	void InitCrc32LookupTable() {
		if (Crc32LookupTableInitialized) return;


		for (uint i = 0; i <= 0xFF; i++)
		{
			UInt32 crc = i;
			for (uint j = 0; j < 8; j++)
				crc = (crc >> 1) ^ (UInt32)(-(crc & 1) & 0xedb88320u);
			Crc32Lookup[0, i] = crc;
		}

		for (uint i = 0; i <= 0xFF; i++)
		{
			Crc32Lookup[1, i] = (Crc32Lookup[0, i] >> 8) ^ Crc32Lookup[0, Crc32Lookup[0, i] & 0xFF];
			Crc32Lookup[2, i] = (Crc32Lookup[1, i] >> 8) ^ Crc32Lookup[0, Crc32Lookup[1, i] & 0xFF];
			Crc32Lookup[3, i] = (Crc32Lookup[2, i] >> 8) ^ Crc32Lookup[0, Crc32Lookup[2, i] & 0xFF];
			Crc32Lookup[4, i] = (Crc32Lookup[3, i] >> 8) ^ Crc32Lookup[0, Crc32Lookup[3, i] & 0xFF];
			Crc32Lookup[5, i] = (Crc32Lookup[4, i] >> 8) ^ Crc32Lookup[0, Crc32Lookup[4, i] & 0xFF];
			Crc32Lookup[6, i] = (Crc32Lookup[5, i] >> 8) ^ Crc32Lookup[0, Crc32Lookup[5, i] & 0xFF];
			Crc32Lookup[7, i] = (Crc32Lookup[6, i] >> 8) ^ Crc32Lookup[0, Crc32Lookup[6, i] & 0xFF];
		}

		Crc32LookupTableInitialized = true;
	}

	public static UInt32 Compute(byte[] input, UInt32 previousCrc32) {
		return 0xcafebabe;
	}
}

public class LZ4FrameCompressor {
	public static byte[] Compress(byte[] compressed) {
		return compressed;
	}
	public static byte[] Decompress(byte[] compressed) {
		return compressed;
	}
}

public class Granny2Compressor {
	public static byte[] Decompress(Int32 format, byte[] compressed, Int32 decompressedSize, Int32 stop0, Int32 stop1, Int32 stop2) {
		return compressed;
	}

	public static byte[] Decompress4(byte[] compressed, Int32 decompressedSize) {
		return compressed;
	}

}
}
