using Manifold.IO;

namespace GameCube.AmusementVision.LZ
{
    public static class Lz
    {
        public static void Unpack(Stream inputStream, Stream outputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException("inputStream");
            if (outputStream == null)
                throw new ArgumentNullException("outputStream");

            EndianBinaryReader reader = new EndianBinaryReader(inputStream, Endianness.LittleEndian);

            // Read file header
            int headerSizeField = reader.ReadInt32();
            int uncompressedSize = reader.ReadInt32();
            int compressedSize = headerSizeField;

            // We can reason about which size the file is knowing the following:
            // (A) the size in the file's header and the files's length are equal OR
            // (B) the size in the file's header is 8 bytes less than and the files's length
            int fileLength = (int)reader.BaseStream.Length;
            // If size in file matches the size in header, we subtract 8 bytes from the header size.
            // This is because these games count the header size in the length.
            bool isMatchingExact = headerSizeField == fileLength;
            // ... and if it isn't, it should be exactly 8 bytes less.
            // If it isn't, we may be dealing with a different kind of file.
            bool isMatchingMinus8 = headerSizeField == fileLength - 8;

            // This does that precise check.
            // Condition (A): we need to subtract 8 from the size.
            if (isMatchingExact)
            {
                compressedSize -= 8;
            }
            // Condition (B): no need to change size.
            // Sanity check: if neither is true, we are not dealing with a supported file.
            else if (!isMatchingMinus8)
            {
                var errorMessage = "Invalid LZ file. File size and headerSizeField do not match known cases.";
                throw new InvalidLzFileException(errorMessage);
            }

            // Read and uncompress LZSS data
            byte[] compressedData = reader.ReadBytes(compressedSize);

            LzssDecoder decoder = new LzssDecoder();
            byte[] uncompressedData = decoder.Decode(compressedData);
            if (uncompressedData.Length != uncompressedSize)
            {
                throw new InvalidLzFileException("Invalid .lz file, outputSize does not match actual output size.");
            }

            // Write uncompressed data to output stream
            outputStream.Write(uncompressedData, 0, uncompressedData.Length);
        }

        public static void Pack(Stream inputStream, Stream outputStream, AvGame game)
        {
            if (inputStream == null)
                throw new ArgumentNullException("inputStream");
            if (outputStream == null)
                throw new ArgumentNullException("outputStream");
            if (!Enum.IsDefined(typeof(AvGame), game))
                throw new ArgumentOutOfRangeException("game");

            // Read the input data and compress with LZSS
            byte[] uncompressedData = GetAllBytes(inputStream);

            LzssEncoder encoder = new LzssEncoder();
            byte[] compressedData = encoder.Encode(uncompressedData);

            // Write file header and data
            int headerSizeField = compressedData.Length;
            switch (game)
            {
                case AvGame.SuperMonkeyBall:
                case AvGame.SuperMonkeyBallDX:
                case AvGame.FZeroAX:
                    {
                        // These games count the 8 bytes of header in the compressed size field
                        headerSizeField += 8;
                    }
                    break;

                case AvGame.FZeroGX:
                    {
                        // These games store the compressed size exactly
                    }
                    break;

                default:
                    throw new NotImplementedException($"Packing for game '{game}' is not implemented!");
            }

            EndianBinaryWriter outputBinaryWriter = new EndianBinaryWriter(outputStream, Endianness.LittleEndian);
            outputBinaryWriter.Write(headerSizeField);
            outputBinaryWriter.Write(uncompressedData.Length);
            outputBinaryWriter.Write(compressedData);
        }

        private static byte[] GetAllBytes(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

    }
}
