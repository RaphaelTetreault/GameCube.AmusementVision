using System.Collections.Generic;

namespace GameCube.AmusementVision.LZ
{
    internal class LzssDecoder
    {
        public byte[] Decode(byte[] input)
        {
            List<byte> output = new List<byte>();
            byte[] ringBuf = new byte[LzssParameters.N];
            int inputPos = 0, ringBufPos = LzssParameters.N - LzssParameters.F;

            ushort flags = 0;

            // Clear ringBuf with a character that will appear often
            for (int i = 0; i < LzssParameters.N - LzssParameters.F; i++)
                ringBuf[i] = LzssParameters.BUFF_INIT;

            while (inputPos < input.Length)
            {
                // Use 16 bits cleverly to count to 8.
                // (After 8 shifts, the high bits will be cleared).
                if ((flags & 0xFF00) == 0)
                    flags = (ushort)(input[inputPos++] | 0x8000);

                if ((flags & 1) == 1)
                {
                    // Copy data literally from input
                    byte c = input[inputPos++];
                    output.Add(c);
                    ringBuf[ringBufPos++ % LzssParameters.N] = c;
                }
                else
                {
                    // Copy data from the ring buffer (previous data).
                    int index = ((input[inputPos + 1] & 0xF0) << 4) | input[inputPos];
                    int count = (input[inputPos + 1] & 0x0F) + LzssParameters.THRESHOLD;
                    inputPos += 2;

                    for (int i = 0; i < count; i++)
                    {
                        byte c = ringBuf[(index + i) % LzssParameters.N];
                        output.Add(c);
                        ringBuf[ringBufPos++ % LzssParameters.N] = c;
                    }
                }

                // Advance flags & count bits
                flags >>= 1;
            }

            return output.ToArray();
        }
    }
}
