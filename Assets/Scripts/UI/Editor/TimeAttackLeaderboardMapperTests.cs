using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Needle.UI.Editor.Tests
{
    public class TimeAttackLeaderboardMapperTests
    {
        [Test]
        public void ToMilliseconds_TruncatesFractionalMilliseconds()
        {
            Assert.AreEqual(1234, TimeAttackLeaderboardMapper.ToMilliseconds(1.2349f));
        }

        [Test]
        public void ToMilliseconds_Negative_IsZero()
        {
            Assert.AreEqual(0, TimeAttackLeaderboardMapper.ToMilliseconds(-1.5f));
        }

        [Test]
        public void FormatMilliseconds_MatchesClockFormat()
        {
            Assert.AreEqual("00:01.23", TimeAttackLeaderboardMapper.FormatMilliseconds(1234));
        }

        [Test]
        public void ResolveDisplayName_Empty_UsesHiddenFallback()
        {
            Assert.AreEqual("Пользователь скрыт", TimeAttackLeaderboardMapper.ResolveDisplayName("  ", "Пользователь скрыт"));
            Assert.AreEqual("Alice", TimeAttackLeaderboardMapper.ResolveDisplayName(" Alice ", "hidden"));
        }

        [Test]
        public void TryParse_MapsPlaygamaDictionary()
        {
            var source = new Dictionary<string, string>
            {
                { "id", "player-1" },
                { "name", "Bob" },
                { "score", "83456" },
                { "rank", "2" },
                { "photo", "https://example.com/bob.png" }
            };

            Assert.IsTrue(TimeAttackLeaderboardMapper.TryParse(source, "hidden", out TimeAttackLeaderboardRow row));
            Assert.AreEqual("player-1", row.Id);
            Assert.AreEqual(2, row.Rank);
            Assert.AreEqual("Bob", row.DisplayName);
            Assert.AreEqual(83456, row.ScoreMilliseconds);
            Assert.AreEqual("https://example.com/bob.png", row.PhotoUrl);
            Assert.AreEqual("01:23.45", TimeAttackLeaderboardMapper.ToEntry(row).ScoreLabel);
        }

        [Test]
        public void TryParse_MissingName_UsesHiddenFallback()
        {
            var source = new Dictionary<string, string>
            {
                { "id", "hidden-user" },
                { "name", "" },
                { "score", "1000" },
                { "rank", "1" }
            };

            Assert.IsTrue(TimeAttackLeaderboardMapper.TryParse(source, "User is hidden", out TimeAttackLeaderboardRow row));
            Assert.AreEqual("User is hidden", row.DisplayName);
        }

        [Test]
        public void TakeTop_KeepsRanksOneThroughTen_AndDropsIncludeUserOutsideTop()
        {
            var rows = new List<TimeAttackLeaderboardRow>
            {
                new TimeAttackLeaderboardRow("a", 1, "A", 1000, null),
                new TimeAttackLeaderboardRow("b", 11, "B", 2000, null),
                new TimeAttackLeaderboardRow("c", 10, "C", 1500, null),
                new TimeAttackLeaderboardRow("d", 3, "D", 1200, null)
            };

            List<TimeAttackLeaderboardRow> top = TimeAttackLeaderboardMapper.TakeTop(rows);

            Assert.AreEqual(3, top.Count);
            Assert.AreEqual(1, top[0].Rank);
            Assert.AreEqual(3, top[1].Rank);
            Assert.AreEqual(10, top[2].Rank);
        }

        [Test]
        public void TryFindPlayer_MatchesId()
        {
            var rows = new List<TimeAttackLeaderboardRow>
            {
                new TimeAttackLeaderboardRow("a", 1, "A", 1000, null),
                new TimeAttackLeaderboardRow("me", 42, "Me", 9000, "photo")
            };

            Assert.IsTrue(TimeAttackLeaderboardMapper.TryFindPlayer(rows, "me", out TimeAttackLeaderboardRow row));
            Assert.AreEqual(42, row.Rank);
            Assert.IsFalse(TimeAttackLeaderboardMapper.TryFindPlayer(rows, "missing", out _));
        }

        [Test]
        public void ToEntry_PreservesPhotoUrl()
        {
            var row = new TimeAttackLeaderboardRow("id", 4, "Name", 2500, "https://cdn/photo.png");
            LeaderboardEntry entry = TimeAttackLeaderboardMapper.ToEntry(row);
            Assert.AreEqual(4, entry.Rank);
            Assert.AreEqual("Name", entry.DisplayName);
            Assert.AreEqual("https://cdn/photo.png", entry.PhotoUrl);
            Assert.IsNull(entry.CountryIcon);
        }
    }
}
