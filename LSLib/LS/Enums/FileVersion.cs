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
        /// Latest version supported by this library
        /// </summary>
        CurrentVersion = 0x03
    }
}