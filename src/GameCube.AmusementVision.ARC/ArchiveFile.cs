using Manifold.IO;

namespace GameCube.AmusementVision.ARC;

/// <summary>
///     File wrapper for <see cref="Archive"/>.
/// </summary>
public class ArchiveFile : BinaryFileWrapper<Archive>
{
    // CONSTANTS
    public const Endianness endianness = Endianness.BigEndian;
    public const string fileExtension = ".arc";

    // PROPERTIES
    public override Endianness Endianness => endianness;
    public override string FileExtension => FileExtension;
    public override string FileName { get; set; } = string.Empty;
}
