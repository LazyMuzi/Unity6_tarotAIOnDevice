using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tarot.Core
{
    /// <summary>
    /// 3장 스프레드 AI 응답에서 과거·현재·미래 구절을 분리합니다.
    /// </summary>
    public static class TarotSpreadReadingParser
    {
        private const string LegacyIntroLine = "과거·현재·미래의 흐름이 이렇게 이어져요.";
        private const string TagPast = "[과거]";
        private const string TagPresent = "[현재]";
        private const string TagFuture = "[미래]";

        private static readonly string[] AllSpreadTags = { TagPast, TagPresent, TagFuture };

        private static readonly Regex SpreadNoisePrefix = new Regex(
            @"^\s*(?:\*+\s*)?((과거|현재|미래)\s*슬롯|키워드\s*끝|슬럼|진행|슬롯)\s*[：:]\s*",
            RegexOptions.Compiled);

        /// <summary>
        /// 스프레드 한 구절에서 모델이 남긴 슬롯·메타 줄 접두만 제거합니다.
        /// </summary>
        public static string SanitizeSpreadSection(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return section;

            string[] lines = section.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var kept = new List<string>(lines.Length);

            foreach (string line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t))
                    continue;

                t = SpreadNoisePrefix.Replace(t, "").Trim();
                if (string.IsNullOrEmpty(t))
                    continue;

                kept.Add(t);
            }

            if (kept.Count == 0)
                return string.Empty;

            return string.Join(Environment.NewLine, kept).Trim();
        }

        /// <summary>
        /// 점괘 본문을 [과거]/[현재]/[미래] 태그로 나눕니다. 태그 순서가 뒤섞여 있어도 각 태그 뒤~다음 태그 앞까지 잘라 씁니다.
        /// </summary>
        public static string[] SplitSections(string readingBody)
        {
            if (string.IsNullOrWhiteSpace(readingBody))
                return new[] { string.Empty, string.Empty, string.Empty };

            string body = NormalizeSpreadBody(readingBody.Trim());

            if (body.StartsWith(LegacyIntroLine, StringComparison.Ordinal))
            {
                body = body.Substring(LegacyIntroLine.Length).TrimStart();
                if (body.StartsWith("\n\n", StringComparison.Ordinal))
                    body = body.Substring(2).TrimStart();
            }

            string past = ExtractTaggedSection(body, TagPast);
            string present = ExtractTaggedSection(body, TagPresent);
            string future = ExtractTaggedSection(body, TagFuture);

            if (!string.IsNullOrEmpty(past) || !string.IsNullOrEmpty(present) || !string.IsNullOrEmpty(future))
                return new[] { past, present, future };

            return SplitSectionsByParagraphFallback(body);
        }

        private static string NormalizeSpreadBody(string body)
        {
            if (string.IsNullOrEmpty(body))
                return body;
            return body
                .Replace("［", "[").Replace("］", "]")
                .Replace("【", "[").Replace("】", "]");
        }

        /// <summary>
        /// <paramref name="tag"/> 직후부터, 그 다음에 등장하는 다른 스프레드 태그 직전까지를 잘라냅니다.
        /// </summary>
        private static string ExtractTaggedSection(string body, string tag)
        {
            int i = body.IndexOf(tag, StringComparison.Ordinal);
            if (i < 0)
                return string.Empty;

            int contentStart = i + tag.Length;
            int end = body.Length;
            foreach (string boundary in AllSpreadTags)
            {
                if (boundary == tag)
                    continue;
                int j = body.IndexOf(boundary, contentStart, StringComparison.Ordinal);
                if (j >= 0 && j < end)
                    end = j;
            }

            return SanitizeSpreadSection(body.Substring(contentStart, end - contentStart).Trim());
        }

        private static string[] SplitSectionsByParagraphFallback(string body)
        {
            string[] parts = body.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                return new[]
                {
                    SanitizeSpreadSection(parts[0].Trim()),
                    SanitizeSpreadSection(parts[1].Trim()),
                    SanitizeSpreadSection(parts[2].Trim())
                };
            }

            if (parts.Length == 2)
            {
                return new[]
                {
                    SanitizeSpreadSection(parts[0].Trim()),
                    SanitizeSpreadSection(parts[1].Trim()),
                    string.Empty
                };
            }

            if (parts.Length == 1)
                return new[] { SanitizeSpreadSection(parts[0].Trim()), string.Empty, string.Empty };

            return new[] { SanitizeSpreadSection(body), string.Empty, string.Empty };
        }
    }
}
