using System;
using System.Collections.Generic;
using System.Globalization;

namespace Needle.UI
{
    public readonly struct TimeAttackLeaderboardRow
    {
        public TimeAttackLeaderboardRow(
            string id,
            int rank,
            string displayName,
            int scoreMilliseconds,
            string photoUrl)
        {
            Id = id ?? string.Empty;
            Rank = rank;
            DisplayName = displayName ?? string.Empty;
            ScoreMilliseconds = scoreMilliseconds;
            PhotoUrl = photoUrl;
        }

        public string Id { get; }
        public int Rank { get; }
        public string DisplayName { get; }
        public int ScoreMilliseconds { get; }
        public string PhotoUrl { get; }
    }

    public static class TimeAttackLeaderboardMapper
    {
        public const int TopCount = 10;
        public const string HiddenUserFallback = "User is hidden";

        public static int ToMilliseconds(float seconds)
        {
            if (seconds <= 0f)
                return 0;

            double milliseconds = Math.Floor(seconds * 1000d);
            if (milliseconds >= int.MaxValue)
                return int.MaxValue;

            return (int)milliseconds;
        }

        public static string FormatMilliseconds(int milliseconds)
        {
            if (milliseconds < 0)
                milliseconds = 0;

            return TimeAttackClock.Format(milliseconds / 1000f);
        }

        public static string ResolveDisplayName(string name, string hiddenFallback)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();

            return string.IsNullOrEmpty(hiddenFallback) ? HiddenUserFallback : hiddenFallback;
        }

        public static bool TryParse(
            IReadOnlyDictionary<string, string> source,
            string hiddenFallback,
            out TimeAttackLeaderboardRow row)
        {
            row = default;
            if (source == null)
                return false;

            if (!TryGet(source, "rank", out string rankText) ||
                !int.TryParse(rankText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank) ||
                rank < 1)
                return false;

            if (!TryGet(source, "score", out string scoreText) ||
                !TryParseMilliseconds(scoreText, out int milliseconds))
                return false;

            TryGet(source, "id", out string id);
            TryGet(source, "name", out string name);
            TryGet(source, "photo", out string photo);

            row = new TimeAttackLeaderboardRow(
                id,
                rank,
                ResolveDisplayName(name, hiddenFallback),
                milliseconds,
                string.IsNullOrWhiteSpace(photo) ? null : photo);
            return true;
        }

        public static List<TimeAttackLeaderboardRow> ParseEntries(
            IReadOnlyList<Dictionary<string, string>> entries,
            string hiddenFallback)
        {
            var rows = new List<TimeAttackLeaderboardRow>();
            if (entries == null)
                return rows;

            for (int i = 0; i < entries.Count; i++)
            {
                if (TryParse(entries[i], hiddenFallback, out TimeAttackLeaderboardRow row))
                    rows.Add(row);
            }

            return rows;
        }

        public static List<TimeAttackLeaderboardRow> TakeTop(
            IReadOnlyList<TimeAttackLeaderboardRow> rows,
            int topCount = TopCount)
        {
            var selected = new List<TimeAttackLeaderboardRow>();
            if (rows == null || topCount < 1)
                return selected;

            for (int i = 0; i < rows.Count; i++)
            {
                TimeAttackLeaderboardRow row = rows[i];
                if (row.Rank < 1 || row.Rank > topCount)
                    continue;

                selected.Add(row);
            }

            selected.Sort(CompareByRank);
            return selected;
        }

        public static bool TryFindPlayer(
            IReadOnlyList<TimeAttackLeaderboardRow> rows,
            string playerId,
            out TimeAttackLeaderboardRow row)
        {
            row = default;
            if (rows == null || string.IsNullOrEmpty(playerId))
                return false;

            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].Id, playerId, StringComparison.Ordinal))
                {
                    row = rows[i];
                    return true;
                }
            }

            return false;
        }

        public static LeaderboardEntry ToEntry(TimeAttackLeaderboardRow row)
        {
            return new LeaderboardEntry(
                row.Rank,
                row.DisplayName,
                FormatMilliseconds(row.ScoreMilliseconds),
                null,
                row.PhotoUrl);
        }

        public static List<LeaderboardEntry> ToEntries(IReadOnlyList<TimeAttackLeaderboardRow> rows)
        {
            var entries = new List<LeaderboardEntry>();
            if (rows == null)
                return entries;

            for (int i = 0; i < rows.Count; i++)
                entries.Add(ToEntry(rows[i]));

            return entries;
        }

        static int CompareByRank(TimeAttackLeaderboardRow a, TimeAttackLeaderboardRow b)
        {
            return a.Rank.CompareTo(b.Rank);
        }

        static bool TryParseMilliseconds(string text, out int milliseconds)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out milliseconds) &&
                milliseconds >= 0)
                return true;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
                parsed >= 0d)
            {
                milliseconds = (int)Math.Floor(parsed);
                return true;
            }

            milliseconds = 0;
            return false;
        }

        static bool TryGet(IReadOnlyDictionary<string, string> source, string key, out string value)
        {
            if (source.TryGetValue(key, out value))
                return true;

            value = null;
            return false;
        }
    }
}
