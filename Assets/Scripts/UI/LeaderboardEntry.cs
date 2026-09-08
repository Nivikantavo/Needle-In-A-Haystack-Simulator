using UnityEngine;

namespace Needle.UI
{
    public sealed class LeaderboardEntry
    {
        public LeaderboardEntry(
            int rank,
            string displayName,
            string scoreLabel,
            Sprite countryIcon,
            string photoUrl = null)
        {
            Rank = rank;
            DisplayName = displayName;
            ScoreLabel = scoreLabel;
            CountryIcon = countryIcon;
            PhotoUrl = photoUrl;
        }

        public int Rank { get; }
        public string DisplayName { get; }
        public string ScoreLabel { get; }
        public Sprite CountryIcon { get; }
        public string PhotoUrl { get; }
    }
}
