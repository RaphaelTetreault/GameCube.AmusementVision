using GameCube.GFZ;
using Manifold.IO;
using System;
using System.IO;

namespace GameCube.AmusementVision.LZ;

/// <summary>
///     Static class which wraps LZ pack/unpack functionality.
/// </summary>
public static class Lz
{
    public static void Unpack(Stream inputStream, Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        ArgumentNullException.ThrowIfNull(outputStream);

        EndianBinaryReader reader = new(inputStream, Endianness.LittleEndian);

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

        byte[] uncompressedData = LzssDecoder.Decode(compressedData);
        if (uncompressedData.Length != uncompressedSize)
        {
            throw new InvalidLzFileException("Invalid .lz file, outputSize does not match actual output size.");
        }

        // Write uncompressed data to output stream
        outputStream.Write(uncompressedData, 0, uncompressedData.Length);
    }

    public static LzHeaderType AvGameToLzHeaderType(AvGame avGame)
    {
        return avGame switch
        {
            AvGame.FZeroAX or
            AvGame.SuperMonkeyBall or
            AvGame.SuperMonkeyBallDX => LzHeaderType.FileSizePlus8,

            AvGame.FZeroGX => LzHeaderType.FileSize,

            _ => throw new NotImplementedException($"Unhandled case {avGame}."),
        };
    }

    public static LzHeaderType GfzGameCodeToLzHeaderType(GameCode gameCode)
        => GfzGameCodeFieldsToLzHeaderType((GameCodeFlags)gameCode);

    public static LzHeaderType GfzGameCodeFieldsToLzHeaderType(GameCodeFlags gameCodeFields)
    {
        if (gameCodeFields.HasFlag(GameCodeFlags.GX))
            return LzHeaderType.FileSize;
        else if (gameCodeFields.HasFlag(GameCodeFlags.AX))
            return LzHeaderType.FileSizePlus8;
        else
            throw new NotImplementedException($"Unhandled case {gameCodeFields}.");
    }

    public static void Pack(Stream inputStream, Stream outputStream, GameCode gameCode)
        => Pack(inputStream, outputStream, GfzGameCodeToLzHeaderType(gameCode));

    public static void Pack(Stream inputStream, Stream outputStream, LzHeaderType headerType)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (headerType == LzHeaderType.Undefined)
            throw new ArgumentException($"Undefined {nameof(LzHeaderType)}.");

        // Read the input data and compress with LZSS
        byte[] uncompressedData = GetAllBytes(inputStream);

        LzssEncoder encoder = new();
        byte[] compressedData = encoder.Encode(uncompressedData);

        // Write file header and data
        int headerSizeField = compressedData.Length;
        if (headerType == LzHeaderType.FileSizePlus8)
            headerSizeField += 8;

        EndianBinaryWriter outputBinaryWriter = new(outputStream, Endianness.LittleEndian);
        outputBinaryWriter.Write(headerSizeField);
        outputBinaryWriter.Write(uncompressedData.Length);
        outputBinaryWriter.Write(compressedData);
    }

    private static byte[] GetAllBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    ///     Compresses file at <paramref name="filePath"/> using Amusement Vision LZ compression.
    /// </summary>
    /// <param name="filePath">The file to compress.</param>
    /// <param name="lzHeaderType">Which LZ format to use for this game.</param>
    /// <returns>
    ///     A memory stream with the compressed file contents.
    /// </returns>
    public static MemoryStream Compress(string filePath, LzHeaderType lzHeaderType)
    {
        var compressedFile = new MemoryStream();
        using (var inputFile = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Pack(inputFile, compressedFile, lzHeaderType);
            compressedFile.Flush();
        }
        return compressedFile;
    }

    /// <summary>
    ///     Decompresses file at <paramref name="filePath"/> using Amusement Vision LZ decompression.
    /// </summary>
    /// <param name="filePath">The file to decompress.</param>
    /// <returns>
    ///     A memory stream with the decompressed file contents.
    /// </returns>
    public static MemoryStream Decompress(string filePath)
    {
        var decompressedFile = new MemoryStream();
        using (var inputFile = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Unpack(inputFile, decompressedFile);
            decompressedFile.Flush();
            decompressedFile.Position = 0;
        }
        return decompressedFile;
    }
}
