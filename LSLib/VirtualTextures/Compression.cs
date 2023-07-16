using System;

namespace LSLib.VirtualTextures
{
    public static class FastLZ
    {
        static void fastlz_memmove(byte[] dest, uint destoff, byte[] src, uint srcoff, uint count)
        {
            do
            {
                dest[destoff++] = src[srcoff++];
            } while (--count > 0);
        }

        public static int fastlz1_decompress(byte[] input, int length, byte[] output)
        {
            uint ip = 0;
            uint op = 0;
            uint ip_limit = (uint)input.Length;
            uint ip_bound = (uint)input.Length - 2;
            uint op_limit = (uint)output.Length;
            UInt32 ctrl = (uint)input[ip++] & 31;

            while (true)
            {
                if (ctrl >= 32)
                {
                    UInt32 len = (ctrl >> 5) - 1;
                    UInt32 ofs = (ctrl & 31) << 8;
                    int reff = (int)op - (int)ofs - 1;
                    if (len == 7 - 1)
                    {
                        if (ip > ip_bound) return 0;
                        len += input[ip++];
                    }
                    reff -= input[ip++];
                    len += 3;
                    if (op + len > op_limit)
                    {
                        throw new Exception("Ran out of output buffer space");
                    }

                    if (reff < 0)
                    {
                        throw new Exception("Bad reference in stream");
                    }

                    fastlz_memmove(output, op, output, (uint)reff, len);
                    op += len;
                }
                else
                {
                    ctrl++;
                    if (op + ctrl > op_limit)
                    {
                        throw new Exception("Ran out of output buffer space");
                    }
                    if (ip + ctrl > ip_limit)
                    {
                        throw new Exception("Ran out of input buffer");
                    }

                    fastlz_memmove(output, op, input, ip, ctrl);
                    ip += ctrl;
                    op += ctrl;
                }

                if (ip > ip_bound) break;
                ctrl = input[ip++];
            }

            return (int)op;
        }
    }
}
