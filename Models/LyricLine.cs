namespace LyricsPlayer.Models {
    public sealed class LyricLine : IComparable<LyricLine> {
        public TimeSpan StartTime { get; }
        public TimeSpan? EndTime { get; set; }   // LRC의 경우 다음 라인의 시작 시간으로 채워짐
        public string Text { get; }

        public LyricLine(TimeSpan startTime, string text, TimeSpan? endTime = null) {
            StartTime = startTime;
            EndTime = endTime;
            Text = text ?? string.Empty;
        }

        public string GetTimelineString() {
            string start = FormatTime(StartTime);
            string end = EndTime.HasValue ? FormatTime(EndTime.Value) : "??:??";
            return $"{start} ~ {end}";
        }

        private string FormatTime(TimeSpan ts) {
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        }

        public int CompareTo(LyricLine other) {
            if (other == null) return 1;
            return StartTime.CompareTo(other.StartTime);
        }
    }
}
