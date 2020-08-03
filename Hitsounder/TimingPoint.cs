namespace Hitsounder
{
    /// <summary>
    /// A simplified internal representation of an osu! timing point.
    /// </summary>
    internal struct TimingPoint
    {
        /// <summary>
        /// The offset from the start of the beatmap in milliseconds.
        /// </summary>
        public long Position { get; set; }

        /// <summary>
        /// The sample type of the hitsound.
        /// </summary>
        public short SampleType { get; set; }

        /// <summary>
        /// The sample index of the hitsound.
        /// </summary>
        public short SampleIndex { get; set; }
    }
}
