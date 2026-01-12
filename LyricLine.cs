namespace LyricsPlayer {
	public sealed class LyricLine : IComparable<LyricLine> {
		public TimeSpan StartTime { get; }
		public TimeSpan? EndTime { get; }   // SRT 전용 (LRC는 null)
		public string Text { get; }

		public LyricLine(TimeSpan startTime, string text, TimeSpan? endTime = null) {
			StartTime = startTime;
			EndTime = endTime;
			Text = text ?? string.Empty;
		}

		public int CompareTo(LyricLine other) {
			if(other == null) return 1;
			return StartTime.CompareTo(other.StartTime);
		}

		public override string ToString() {
			if(EndTime.HasValue)
				return $"[{StartTime} - {EndTime}] {Text}";
			return $"[{StartTime}] {Text}";
		}
	}
}
