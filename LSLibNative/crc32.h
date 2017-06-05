#pragma once

#include <msclr/marshal_cppstd.h>

using namespace System;
using namespace System::Collections::Generic;

namespace LSLib {
	namespace Native {
		public ref class Crc32 abstract sealed
		{
		public:
			static uint32_t Compute(array<byte> ^ input, uint32_t previousCrc32);
		};
	}
}
