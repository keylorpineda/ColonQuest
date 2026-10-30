namespace ColonQuest.Game
{
    /// <summary>
    /// The version of the alpha as the player sees it on the title and loading screens. The number after the second dot
    /// follows the Windows build that is being tested, so a build and a bug report always name the same thing.
    /// </summary>
    public static class BuildInfo
    {
        public const int Build = 15;

        public const string Stage = "Alpha";

        public const string Credit = "Design by DoWhile Team";

        public static string Version => "0.1." + Build;

        /// <summary>Like "Alpha 0.1.15". The stage word is kept in English in both languages, as the credit line is.</summary>
        public static string Label => Stage + " " + Version;
    }
}
