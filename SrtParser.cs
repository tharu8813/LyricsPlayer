using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LyricsPlayer {
	public static class SrtParser {
		private static readonly Regex TimeRegex = new Regex(
			@"(?<h>\d{{2}}):(?<m>\d{{2}}):(?<s>\d{{2}}),(?<ms>\d{{3}})",
			RegexOptions.Compiled);

		public static List<LyricLine> Parse(string text) {
			var result = new List<LyricLine>();
			var blocks = Regex.Split(text.Trim(), @"\r?\n\r?\n");
			Debug.WriteLine($"[SrtParser] Found {blocks.Length} blocks.");

			foreach(var block in blocks) {
				var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
				if(lines.Length < 2) continue;

				// 타임라인 줄 (예: 00:00:05,000 --> 00:00:07,000)
				// SRT 파일 형식에 따라 타임라인이 첫 번째 줄에 올 수도 있으므로 확인
				int timeLineIndex = lines.Length > 1 && lines[1].Contains(" --> ") ? 1 : 0;
				if(lines.Length <= timeLineIndex) {
					Debug.WriteLine($"[SrtParser] Skipping block (not enough lines):\n{block}");
					continue;
				}

				var timeMatch = Regex.Match(lines[timeLineIndex], @"(\d+):(\d+):(\d+),(\d+) --> (\d+):(\d+):(\d+),(\d+)");
				if(!timeMatch.Success) {
					Debug.WriteLine($"[SrtParser] Time match failed for line: {lines[timeLineIndex]}");
					continue;
				}

				var start = new TimeSpan(0,
					int.Parse(timeMatch.Groups[1].Value),
					int.Parse(timeMatch.Groups[2].Value),
					int.Parse(timeMatch.Groups[3].Value),
					int.Parse(timeMatch.Groups[4].Value));

				var end = new TimeSpan(0,
					int.Parse(timeMatch.Groups[5].Value),
					int.Parse(timeMatch.Groups[6].Value),
					int.Parse(timeMatch.Groups[7].Value),
					int.Parse(timeMatch.Groups[8].Value));

				// 텍스트 줄
				string lyric = string.Join("\n", lines.Skip(timeLineIndex + 1)).Trim();
				Debug.WriteLine($"[SrtParser] Parsed: Start={start}, End={end}, Lyric='{lyric}'");

				result.Add(new LyricLine(start, lyric, end));
			}

			return result.OrderBy(x => x.StartTime).ToList();
		}


		private static TimeSpan ParseTime(Match m) {
			return new TimeSpan(0,
				int.Parse(m.Groups["h"].Value),
				int.Parse(m.Groups["m"].Value),
				int.Parse(m.Groups["s"].Value),
				int.Parse(m.Groups["ms"].Value));
		}
	}
}