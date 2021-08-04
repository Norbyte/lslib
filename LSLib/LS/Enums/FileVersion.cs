namespace LSLib.LS.Enums
{
    public enum FileVersion
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
        VerExtendedHeader = 0x05,

        /// <summary>
        /// Latest version supported by this library
        /// </summary>
        MaxVersion = 0x05
    }
}