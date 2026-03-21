using LyricsPlayer.Helpers;
using LyricsPlayer.Models;
using System.Text.RegularExpressions;

namespace LyricsPlayer.Parsers {
    public static class SrtParser {
        private static readonly Regex TimeRegex = new Regex(
            @"(\d+):(\d+):(\d+),(\d+) --> (\d+):(\d+):(\d+),(\d+)",
            RegexOptions.Compiled);

        public static List<LyricLine> Parse(string text) {
            MessageHelper.LogToConsole("INFO", "SRT 파싱 시작...");
            var result = new List<LyricLine>();

            if (string.IsNullOrWhiteSpace(text)) return result;

            var blocks = Regex.Split(text.Trim(), @"\r?\n\r?\n");

            foreach (var block in blocks) {
                var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                // SRT 표준: lines[0] = 번호, lines[1] = 타임라인, lines[2+] = 텍스트
                if (lines.Length < 2) continue;

                // 타임라인 줄 찾기 (번호 줄이 없는 비표준 SRT 대응)
                int timeLineIndex = lines[0].Contains(" --> ") ? 0 : 1;
                if (lines.Length <= timeLineIndex) continue;

                var timeMatch = TimeRegex.Match(lines[timeLineIndex]);
                if (!timeMatch.Success) {
                    MessageHelper.LogToConsole("WARNING", $"SRT 타임라인 파싱 실패: {lines[timeLineIndex]}");
                    continue;
                }

                if (!TryParseTimeSpan(timeMatch, 1, out var start) ||
                    !TryParseTimeSpan(timeMatch, 5, out var end)) continue;

                string lyric = string.Join("\n", lines.Skip(timeLineIndex + 1)).Trim();
                if (string.IsNullOrWhiteSpace(lyric)) continue;

                result.Add(new LyricLine(start, lyric, end));
            }

            var final = result.OrderBy(x => x.StartTime).ToList();
            MessageHelper.LogToConsole("INFO", $"SRT 파싱 완료: {final.Count} 줄 로드됨.");
            return final;
        }

        private static bool TryParseTimeSpan(Match m, int groupOffset, out TimeSpan result) {
            result = TimeSpan.Zero;
            if (int.TryParse(m.Groups[groupOffset].Value, out int h) &&
                int.TryParse(m.Groups[groupOffset + 1].Value, out int min) &&
                int.TryParse(m.Groups[groupOffset + 2].Value, out int s) &&
                int.TryParse(m.Groups[groupOffset + 3].Value, out int ms)) {
                result = new TimeSpan(0, h, min, s, ms);
                return true;
            }
            return false;
        }
    }
}