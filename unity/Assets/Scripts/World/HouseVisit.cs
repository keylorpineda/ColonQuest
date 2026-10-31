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

        /// <summary>The neighbors' houses that keep a dog; the others have none.</summary>
        private static readonly string[] DogHouses = { "Casa_Vecina_A", "Casa_Almendros_4" };

        /// <summary>
        /// True when the neighbor's house the player came in by has a dog. The key is the spot in front of its door, the
        /// remembered return of the door (for example "Town_From_Casa_Vecina_A").
        /// </summary>
        public static bool NeighborHasDog(string doorReturn)
        {
            if (string.IsNullOrEmpty(doorReturn))
            {
                return false;
            }

            foreach (var house in DogHouses)
            {
                if (doorReturn.EndsWith(house))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
