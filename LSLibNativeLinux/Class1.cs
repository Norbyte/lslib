using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LSLib.Native
{
public class LZ4FrameCompressor {
	public static byte[] Compress(byte[] compressed) {
		throw new NotSupportedException("LZ4 compression is currently unsupported");
		return compressed;
	}
	public static byte[] Decompress(byte[] compressed) {
		throw new NotSupportedException("LZ4 decompression is currently unsupported");
		return compressed;
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
