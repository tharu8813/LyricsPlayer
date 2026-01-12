using System.Globalization;
using System.Text.RegularExpressions;

namespace LyricsPlayer {
	/// <summary>
	/// 표준 LRC(멀티 타임스탬프 포함) 파서
	/// 예: [00:12.34]가사 / [00:12.34][00:45.67]같은 가사
	/// 태그(ID, AR, TI 등)는 무시하고 순수 가사/타임스탬프만 추출.
	/// </summary>
	public static class LrcParser {
		// [mm:ss.xx] 혹은 [mm:ss] 혹은 [hh:mm:ss.xx] 도 느슨히 지원
		static readonly Regex TagRegex = new Regex(@"\[(\d{1,2}:)?\d{1,2}:\d{1,2}(?:\.\d{1,3})?\]", RegexOptions.Compiled);

		public static List<LyricLine> Parse(string text) {
			var result = new List<LyricLine>();
			var regex = new Regex(@"\[(\d+):(\d+)(?:\.(\d+))?\](.*)");

			foreach(var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)) {
				var match = regex.Match(line);
				if(!match.Success) continue;

				int minutes = int.Parse(match.Groups[1].Value);
				int seconds = int.Parse(match.Groups[2].Value);
				int millis = match.Groups[3].Success ? int.Parse(match.Groups[3].Value.PadRight(3, '0')) : 0;
				string lyric = match.Groups[4].Value.Trim();

				var timestamp = new TimeSpan(0, 0, minutes, seconds, millis);

				// EndTime 없음
				result.Add(new LyricLine(timestamp, lyric));
			}

			return result.OrderBy(x => x.StartTime).ToList();
		}

		static TimeSpan? ParseTimestamp(string bracketed) {
			// bracketed: "[mm:ss.xx]" or "[hh:mm:ss.xx]"
			var s = bracketed.Trim('[', ']');
			var parts = s.Split(':');

			try {
				if(parts.Length == 2) {
					// mm:ss(.fff)
					int mm = int.Parse(parts[0], CultureInfo.InvariantCulture);
					double ss = double.Parse(parts[1], CultureInfo.InvariantCulture);
					return TimeSpan.FromMinutes(mm) + TimeSpan.FromSeconds(ss);
				} else if(parts.Length == 3) {
					// hh:mm:ss(.fff)
					int hh = int.Parse(parts[0], CultureInfo.InvariantCulture);
					int mm = int.Parse(parts[1], CultureInfo.InvariantCulture);
					double ss = double.Parse(parts[2], CultureInfo.InvariantCulture);
					return new TimeSpan(hh, mm, 0) + TimeSpan.FromSeconds(ss);
				}
			} catch {
				// 무시
			}
			return null;
		}

		static List<LyricLine> CollapseSameTimestamp(List<LyricLine> src) {
			if(src.Count < 2) return src;

			var res = new List<LyricLine>(src.Count);
			LyricLine prev = null;
			foreach(var cur in src) {
				if(prev != null && prev.StartTime == cur.StartTime) {
					// 같은 타임스탬프면 텍스트를 합치거나 줄바꿈.
					var joined = new LyricLine(cur.StartTime, string.Join(" / ", new[] { prev.Text, cur.Text }.Where(t => !string.IsNullOrEmpty(t))));
					res[res.Count - 1] = joined;
					prev = joined;
				} else {
					res.Add(cur);
					prev = cur;
				}
			}
			return res;
		}
	}
}
