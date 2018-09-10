#pragma once

#include <msclr/marshal_cppstd.h>

using namespace System;
using namespace System::Collections::Generic;

namespace LSLib {
	namespace Native {
		public ref class Granny2Compressor abstract sealed
		{
		public:
			static array<byte> ^ Decompress(Int32 format, array<byte> ^ compressed, Int32 decompressedSize, Int32 stop0, Int32 stop1, Int32 stop2);
			static array<byte> ^ Decompress4(array<byte> ^ compressed, Int32 decompressedSize);
		};
	}
}
