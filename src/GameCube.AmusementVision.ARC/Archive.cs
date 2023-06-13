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
        private const int Alignment = 32;
        private const ulong _16C = 0xCCCCCCCC_CCCCCCCC;
        private readonly UInt128 Spacer32C = new UInt128(_16C, _16C);
        public const uint Magic = 0x55AA382D; // "Uª8-"
        public const Endianness endianness = Endianness.BigEndian;
        public const string Extension = ".arc";

        // MEMBERS
        private uint magic;
        private Pointer fileSystemPtr;
        private int fileSystemSize;
        private Pointer dataPointer;
        private UInt128 spacer32C;
        private FileSystem fileSystem = new FileSystem();
        private FileSystemFile[] files = Array.Empty<FileSystemFile>();

        // PROPERTIES
        public string FileExtension => Extension;
        public string FileName { get; set; } = string.Empty;
        public FileSystemFile[] Files { get => files; set => files = value; }
        public Endianness Endianness => endianness;

        public void Deserialize(EndianBinaryReader reader)
        {
            reader.Read(ref magic);
            Assert.IsTrue(magic == Magic, $"Magic value {magic:x8} does not match expected value {Magic:x8}!");
            reader.Read(ref fileSystemPtr);
            reader.Read(ref fileSystemSize);
            reader.Read(ref dataPointer);
            reader.Read(ref spacer32C);
            Assert.IsTrue(spacer32C == Spacer32C, $"Spacer value {spacer32C:x32} does not match expected value {Spacer32C:x32}!");
            reader.Read(ref fileSystem);
            reader.AlignTo(Alignment);
            files = FileSystemFile.ReadFiles(reader, fileSystem);
        }

        public void Serialize(EndianBinaryWriter writer)
        {
            PrepareFileSystemEntries();

            // Write structure
            writer.Write(Magic);
            var ptrsAddress = writer.GetPositionAsPointer();
            writer.Write(fileSystemPtr);
            writer.Write(fileSystemSize);
            writer.Write(dataPointer);
            writer.Write(_16C);
            writer.Write(_16C);
            writer.Write(fileSystem);
            fileSystemPtr = fileSystem.AddressRange.startAddress;
            fileSystemSize = fileSystemSize = fileSystem.AddressRange.Size;
            writer.AlignTo(Alignment);
            dataPointer = writer.GetPositionAsPointer();
            fileSystem.FilesEntries = FileSystemFileEntry.WriteFiles(writer, files);

            // Write Pointers properly
            writer.JumpToAddress(ptrsAddress);
            writer.Write(fileSystemPtr);
            writer.Write(fileSystemSize);
            writer.Write(dataPointer);
            // Write file system properly (pointers and sizes)
            writer.JumpToAddress(fileSystem.AddressRange.startAddress);
            writer.Write(fileSystem); //TODO: just entries, not names...?
        }

        public void PrepareFileSystemEntries()
        {
            // Prepare FileSystem. Data will be filled later.
            //fileSystem.FilesEntries = FileSystemFileEntry.GetTempFileEntries(files);
            // TODO: this is incorrect!
            // NOT ORDERED & DOES NOT INLCLUDE FOLDERS
            // THUS SIZE IS OFF, POINTERS AND OTHER DATA OFF
            // Looks like you need to build proper entries NOW, no patching FS afterwards other than ptr+size
            // ALSO, YOU NEED TO DEFINE ROOT PATH (TRIM FILE PATHS), ALSO FILE PATCH MUST INCLUDE ROOT PATH
        }

        public void AddPaths(string rootPath, params string[] paths)
        {
            files = FileSystemFile.GetFiles(rootPath, paths);
        }
    }
}
