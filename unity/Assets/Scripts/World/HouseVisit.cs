namespace ColonQuest.World
{
    /// <summary>
    /// Which house of the town the player walked into. The houses for sale or rent share the interior of the mother's
    /// house; while one of them is not the player's, its things only give a thought and the mother is not there.
    /// </summary>
    public static class HouseVisit
    {
        public const string NotMineThought = "Esta casa no es mía. Si la alquilo o la compro, puedo usarla.";

        /// <summary>The listing of the house the player is in, or empty for the mother's house.</summary>
        public static string ListingId { get; private set; } = string.Empty;

        public static bool InListing => !string.IsNullOrEmpty(ListingId);

        public static void Enter(string listingId)
        {
            ListingId = listingId ?? string.Empty;
        }
    }
}
