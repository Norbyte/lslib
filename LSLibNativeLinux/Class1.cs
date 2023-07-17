using System;

namespace LSLib.Native
{
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
