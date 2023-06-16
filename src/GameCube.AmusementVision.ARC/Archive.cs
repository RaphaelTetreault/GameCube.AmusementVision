using GameCube.DiskImage;
using Manifold.IO;
using System;

namespace GameCube.AmusementVision.ARC
{
    public class Archive :
        IFileType,
        IBinaryFileType,
        IBinarySerializable
    {
        // CONSTANTS
        private const int FileAlignment = 32;
        private const byte PaddingCC = 0xCC;
        private const byte PaddingSize = 16;
        public const uint Magic = 0x55AA382D; // "Uª8-"
        public const Endianness endianness = Endianness.BigEndian;
        public const string Extension = ".arc";

        // MEMBERS
        private uint magic;
        private Pointer fileSystemPtr;
        private int fileSystemSize;
        private Pointer dataPointer;
        private FileSystem fileSystem = new FileSystem();

        // PROPERTIES
        public string FileExtension => Extension;
        public string FileName { get; set; } = string.Empty;
        public Endianness Endianness => endianness;
        public FileSystem FileSystem => fileSystem;

        public void Deserialize(EndianBinaryReader reader)
        {
            reader.Read(ref magic);
            Assert.IsTrue(magic == Magic, $"Magic value {magic:x8} does not match expected value {Magic:x8}!");
            reader.Read(ref fileSystemPtr);
            reader.Read(ref fileSystemSize);
            reader.Read(ref dataPointer);
            bool isCorrectPadding = reader.ReadPadding(PaddingCC, PaddingSize);
            Assert.IsTrue(isCorrectPadding, $"Spacer value does not match expected value {PaddingCC:x2}!");
            reader.Read(ref fileSystem);
        }

        public void Serialize(EndianBinaryWriter writer)
        {
            // Enforce file alignment in file system
            fileSystem.FileAlignment = FileAlignment;

            // Write structure
            writer.Write(Magic);
            var ptrsAddress = writer.GetPositionAsPointer();
            writer.Write(fileSystemPtr);
            writer.Write(fileSystemSize);
            writer.Write(dataPointer);
            writer.WritePadding(PaddingCC, PaddingSize);
            writer.Write(fileSystem);

            // Assign pointer, will write later
            fileSystemPtr = fileSystem.AddressRange.startAddress;
            fileSystemSize = fileSystemSize = fileSystem.AddressRange.Size;
            writer.AlignTo(FileAlignment); // Realign with file alignment
            dataPointer = writer.GetPositionAsPointer();

            // Write proper pointers
            writer.JumpToAddress(ptrsAddress);
            writer.Write(fileSystemPtr);
            writer.Write(fileSystemSize);
            writer.Write(dataPointer);
        }
    }
}
