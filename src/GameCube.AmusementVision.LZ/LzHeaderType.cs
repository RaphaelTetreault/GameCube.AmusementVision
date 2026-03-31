namespace GameCube.AmusementVision.LZ;

/// <summary>
///     LZ header format. Some filers specify file size, others
///     specify file size + 8 (bytes).
/// </summary>
public enum LzHeaderType
{
    /// <summary>
    ///     Default value.
    /// </summary>
    Undefined,

    /// <summary>
    ///     Used for
    ///     <see cref="AvGame.FZeroGX"/>.
    /// </summary>
    FileSize,

    /// <summary>
    ///   Used for
    ///   <see cref="AvGame.SuperMonkeyBall"/> (1 and 2),
    ///   <see cref="AvGame.SuperMonkeyBallDX"/>, and
    ///   <see cref="AvGame.FZeroAX"/>.
    /// </summary>
    FileSizePlus8,
}
