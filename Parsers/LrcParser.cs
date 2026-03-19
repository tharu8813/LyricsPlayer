using System.Globalization;
using System.Text.RegularExpressions;

using LyricsPlayer.Models;

namespace LyricsPlayer.Parsers {
	/// <summary>
	/// 표준 LRC(멀티 타임스탬프 포함) 파서
	/// </summary>
	public static class LrcParser {
		// [mm:ss.xx] 혹은 [hh:mm:ss.xx] 지원
		static readonly Regex LineRegex = new Regex(@"^((?:\[(?:\d{1,2}:)?\d{1,2}:\d{1,2}(?:\.\d{1,3})?\])+)(.*)$", RegexOptions.Compiled);
		static readonly Regex TimestampRegex = new Regex(@"\[((?:\d{1,2}:)?\d{1,2}:\d{1,2}(?:\.\d{1,3})?)\]", RegexOptions.Compiled);

		public static List<LyricLine> Parse(string text) {
			var result = new List<LyricLine>();
			if(string.IsNullOrWhiteSpace(text)) return result;

			foreach(var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
				var match = LineRegex.Match(line.Trim());
				if(!match.Success) continue;

				string timestampsPart = match.Groups[1].Value;
				string lyric = match.Groups[2].Value.Trim();

				var tsMatches = TimestampRegex.Matches(timestampsPart);
				foreach(Match tsMatch in tsMatches) {
					var timestamp = ParseTimestamp(tsMatch.Groups[1].Value);
					if(timestamp.HasValue) {
						result.Add(new LyricLine(timestamp.Value, lyric));
					}
				}
			}

			var sorted = result.OrderBy(x => x.StartTime).ToList();
			return CollapseSameTimestamp(sorted);
		}

		private static TimeSpan? ParseTimestamp(string s) {
			var parts = s.Split(':');
			try {
				if(parts.Length == 2) {
					// mm:ss(.fff)
					if(int.TryParse(parts[0], out int mm) && double.TryParse(parts[1], CultureInfo.InvariantCulture, out double ss)) {
						return TimeSpan.FromMinutes(mm) + TimeSpan.FromSeconds(ss);
					}
				} else if(parts.Length == 3) {
					// hh:mm:ss(.fff)
					if(int.TryParse(parts[0], out int hh) && int.TryParse(parts[1], out int mm) && double.TryParse(parts[2], CultureInfo.InvariantCulture, out double ss)) {
						return new TimeSpan(hh, mm, 0) + TimeSpan.FromSeconds(ss);
					}
				}
			} catch {
				// 파싱 실패 시 무시
			}
			return null;
		}

		private static List<LyricLine> CollapseSameTimestamp(List<LyricLine> src) {
			if(src.Count < 2) return src;

			var res = new List<LyricLine>();
			foreach(var cur in src) {
				if(res.Count > 0 && res[^1].StartTime == cur.StartTime) {
					// 같은 타임스탬프면 텍스트를 합침
					var prev = res[^1];
					string newText = string.IsNullOrWhiteSpace(prev.Text) ? cur.Text : (string.IsNullOrWhiteSpace(cur.Text) ? prev.Text : $"{prev.Text} / {cur.Text}");
					res[^1] = new LyricLine(cur.StartTime, newText);
				} else {
					res.Add(cur);
				}
			}
			return res;
		}
	}
}
