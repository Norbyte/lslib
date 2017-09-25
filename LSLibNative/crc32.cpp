#pragma once

#include "crc32.h"

namespace LSLib {
	namespace Native {
		static uint32_t Crc32Lookup[8][0x100];
		static bool Crc32LookupTableInitialized = false;

		void InitCrc32LookupTable()
		{
			if (Crc32LookupTableInitialized) return;

			for (unsigned int i = 0; i <= 0xFF; i++)
			{
				uint32_t crc = i;
				for (unsigned int j = 0; j < 8; j++)
					crc = (crc >> 1) ^ (-int(crc & 1) & 0xedb88320u);
				Crc32Lookup[0][i] = crc;
			}

			for (unsigned int i = 0; i <= 0xFF; i++)
			{
				Crc32Lookup[1][i] = (Crc32Lookup[0][i] >> 8) ^ Crc32Lookup[0][Crc32Lookup[0][i] & 0xFF];
				Crc32Lookup[2][i] = (Crc32Lookup[1][i] >> 8) ^ Crc32Lookup[0][Crc32Lookup[1][i] & 0xFF];
				Crc32Lookup[3][i] = (Crc32Lookup[2][i] >> 8) ^ Crc32Lookup[0][Crc32Lookup[2][i] & 0xFF];
				Crc32Lookup[4][i] = (Crc32Lookup[3][i] >> 8) ^ Crc32Lookup[0][Crc32Lookup[3][i] & 0xFF];
				Crc32Lookup[5][i] = (Crc32Lookup[4][i] >> 8) ^ Crc32Lookup[0][Crc32Lookup[4][i] & 0xFF];
				Crc32Lookup[6][i] = (Crc32Lookup[5][i] >> 8) ^ Crc32Lookup[0][Crc32Lookup[5][i] & 0xFF];
				Crc32Lookup[7][i] = (Crc32Lookup[6][i] >> 8) ^ Crc32Lookup[0][Crc32Lookup[6][i] & 0xFF];
			}

			Crc32LookupTableInitialized = true;
		}

		uint32_t Crc32::Compute(array<byte> ^ input, uint32_t previousCrc32)
		{
			if (input->Length == 0)
			{
				return previousCrc32;
			}

			pin_ptr<byte> inputPin(&input[input->GetLowerBound(0)]);

			uint32_t * current = (uint32_t *)inputPin;
			int length = input->Length;
			uint32_t crc = ~previousCrc32;

			InitCrc32LookupTable();

			// process eight bytes at once
			while (length >= 8)
			{
				uint32_t one = *current++ ^ crc;
				uint32_t two = *current++;
				crc = Crc32Lookup[7][one & 0xFF] ^
					Crc32Lookup[6][(one >> 8) & 0xFF] ^
					Crc32Lookup[5][(one >> 16) & 0xFF] ^
					Crc32Lookup[4][one >> 24] ^
					Crc32Lookup[3][two & 0xFF] ^
					Crc32Lookup[2][(two >> 8) & 0xFF] ^
					Crc32Lookup[1][(two >> 16) & 0xFF] ^
					Crc32Lookup[0][two >> 24];
				length -= 8;
			}
			byte * currentChar = (byte *)current;
			// remaining 1 to 7 bytes
			while (length--)
				crc = (crc >> 8) ^ Crc32Lookup[0][(crc & 0xFF) ^ *currentChar++];

			return ~crc;
		}
	}
}
