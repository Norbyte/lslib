namespace LSLib.LS.Enums;

public enum LSFVersion
{
    /// <summary>
    /// Initial version of the LSF format
    /// </summary>
    VerInitial = 0x01,

    /// <summary>
    /// LSF version that added chunked compression for substreams
    /// </summary>
    VerChunkedCompress = 0x02,

    /// <summary>
    /// LSF version that extended the node descriptors
    /// </summary>
    VerExtendedNodes = 0x03,

    /// <summary>
    /// BG3 version, no changes found so far apart from version numbering
    /// </summary>
    VerBG3 = 0x04,

    /// <summary>
    /// BG3 version with updated header metadata
    /// </summary>
    VerBG3ExtendedHeader = 0x05,

    /// <summary>
    /// BG3 version with unknown additions
    /// </summary>
    VerBG3AdditionalBlob = 0x06,

    /// <summary>
    /// BG3 Patch 3 version with unknown additions
    /// </summary>
    VerBG3Patch3 = 0x07,

    /// <summary>
    /// Latest input version supported by this library
    /// </summary>
    MaxReadVersion = 0x07,

    /// <summary>
    /// Latest output version supported by this library
    /// </summary>
    MaxWriteVersion = 0x06
}

public enum LSXVersion
{
    /// <summary>
    /// Version used in D:OS 2 (DE)
    /// </summary>
    V3 = 3,
    /// <summary>
    /// Version used in BG3
    /// Replaces type IDs with type names
    /// </summary>
    V4 = 4
}