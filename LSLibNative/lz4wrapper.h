#pragma once

#include <msclr/marshal_cppstd.h>
#pragma managed(push, off)
#include "lz4/lz4frame.h"
#pragma managed(pop)

using namespace System;
using namespace System::Collections::Generic;

namespace LSLib {
	namespace Native {
		public ref class LZ4FrameCompressor abstract sealed
		{
		public:
			static array<byte> ^ Compress(array<byte> ^ compressed);
			static array<byte> ^ Decompress(array<byte> ^ compressed);
		};
	}
}
