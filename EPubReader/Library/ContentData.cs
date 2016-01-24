using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System;
using Ionic.Zip;

namespace eBdb.EpubReader {
	public class ContentData {
		private readonly ZipEntry _ZipEntry;

        #region Extra Data

        private readonly Hashtable _LinksMapping = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
        private static Regex _RefsRegex = new Regex(@"(?<prefix><\w+[^>]*?href\s*=\s*(""|'))(?<href>[^""']*)(?<suffix>(""|')[^>]*>)", Utils.REO_ci);
        private static Regex _ExternalLinksRegex = new Regex(@"^\s*(http(s)?://|mailto:|ftp(s)?://)", Utils.REO_ci);

        private readonly Epub _parentEpub;

        #endregion Extra Data

        public string FileName { get; private set; }
		public string Content {
			get {
				using (MemoryStream memoryStream = new MemoryStream()) {
					_ZipEntry.Extract(memoryStream);
					memoryStream.Position = 0;

					using (StreamReader reader = new StreamReader(memoryStream)) return reader.ReadToEnd();
				}
			}
		}

		public ContentData(string fileName, ZipEntry zipEntry, Epub parentEpub) {
			FileName = fileName;
			_ZipEntry = zipEntry;
            _parentEpub = parentEpub;
		}

		public string GetContentAsPlainText() {
			Match m = Regex.Match(Content, @"<body[^>]*>.+</body>", Utils.REO_csi);
			return m.Success ? Utils.ClearText(m.Value) : "";
		}

        public string GetContentAsHTMLText()
        {
            //Match m = Regex.Match(Content, @"<body[^>]*>.+</body>", Utils.REO_csi);
            //return m.Success ? m.Value : "";
            return GetContentAsHtml();
        }

        #region HTML Content Retrival

        private string GetContentAsHtml(string cssFileName = null)
        {
            StringBuilder body = new StringBuilder();

            //Note: run through all content items and collect collection of replacement links (solves problem with client id value replacement)
            {
                CollectReplacementLinks(_LinksMapping, GetTrimmedFileName(this.FileName, false), this.Content);
            }

            {
                Match m = Regex.Match(this.Content, @"<body[^>]*>(?<body>.+)</body>", Utils.REO_csi);
                if (m.Success)
                {
                    //Note: add link to top of page so they can be found by the table of contents
                    //then update links within body

                    // May have a problem here!
                    string _CurrentFileName = GetTrimmedFileName(this.FileName, false);
                    string fullContentHtml = NormalizeRefs("<a id=\"" + _CurrentFileName.Replace('.', '_') + "\"/>" + m.Groups["body"].Value);

                    //embed base64 images & append
                    fullContentHtml = EmbedImages(fullContentHtml);
                    body.Append(fullContentHtml);
                    _CurrentFileName = null;
                }
            }

            string headPart = "";
            Match match = Regex.Match(this.Content, @"<head[^>]*>(?<head>.+?)</head>", Utils.REO_csi);
            if (match.Success) headPart = Regex.Replace(match.Groups["head"].Value, @"<title[^>]*>.+?</title>", "", Utils.REO_csi);

            if (cssFileName != null)
            {
                // Load the specific CSS file.
                string cssLink = "<link href=\"" + cssFileName.Replace("\\", "/") + "\" rel=\"stylesheet\" type=\"text/css\" />";
                // Add it to the header.
                headPart += cssLink;
            }


            if (!Regex.IsMatch(headPart, @"<meta\s+[^>]*?http-equiv\s*=\s*(""|')Content-Type(""|')", Utils.REO_csi))
                headPart += "<meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" />";


            headPart = EmbedCssData(headPart);

            string bodyTag = "<body>";
            match = Regex.Match(this.Content, @"<body[^>]*>", Utils.REO_ci);
            if (match.Success) bodyTag = match.Value;

            return string.Format(_HtmlTemplate, "", headPart.Trim(), bodyTag, body);
        }


        private const string _HtmlTemplate = @"<!DOCTYPE html
			  PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"">
			<html>
			   <head>
				  <title>{0}</title>
				  {1}
			   </head>
			   {2}
				  {3}
			   </body>
			</html>";

        private static string GetNormalizedSrc(string originalSrc)
        {
            string trimmedFileName = GetTrimmedFileName(originalSrc, false);
            return trimmedFileName != null ? "#" + trimmedFileName.Replace('.', '_').Replace('#', '_') : null;
        }

        private string CssEvaluator(Match match)
        {
            var extendedData = _parentEpub.ExtendedData[GetTrimmedFileName(match.Groups["href"].Value, true)] as ExtendedData;
            return extendedData != null
                       ? string.Format("<style type=\"text/css\">{0}</style>", extendedData.Content) : match.Value;
        }

        private string EmbedCssData(string head)
        {
            return Regex.Replace(head, @"<link\s+[^>]*?(href\s*=\s*(""|')(?<href>[^""']+)(""|')[^>]*?|type\s*=\s*(""|')text/css(""|')[^>]*?){2}[^>]*?/>", CssEvaluator, Utils.REO_ci);
        }

        

        private string EmbedImages(string html)
        {
            return Regex.Replace(html, @"(?<prefix><\w+[^>]*?src\s*=\s*(""|'))(?<src>[^""']+)(?<suffix>(""|')[^>]*>)", SrcEvaluator, Utils.REO_ci);
        }

        private string SrcEvaluator(Match match)
        {
            var extendedData = _parentEpub.ExtendedData[GetTrimmedFileName(match.Groups["src"].Value, true)] as ExtendedData;
            return extendedData != null
                       ? match.Groups["prefix"].Value + "data:" + extendedData.MimeType + ";base64," + extendedData.Content +
                         match.Groups["suffix"].Value : match.Value;
        }

        private static void CollectReplacementLinks(Hashtable linksMapping, string fileName, string text)
        {
            MatchCollection matches = _RefsRegex.Matches(text);
            foreach (Match match in matches)
            {
                if (!_ExternalLinksRegex.IsMatch(match.Groups["href"].Value))
                {
                    string targetFileName = (GetTrimmedFileName(match.Groups["href"].Value, true) ?? GetTrimmedFileName(fileName, true)) + GetAnchorValue(match.Groups["href"].Value);
                    linksMapping[targetFileName] = GetNormalizedSrc(match.Groups["href"].Value);
                }
            }
        }

        private string NormalizeRefs(string text)
        {
            if (text == null) return null;
            text = _RefsRegex.Replace(text, RefsEvaluator);
            text = Regex.Replace(text, @"(?<prefix>\bid\s*=\s*(""|'))(?<id>[^""']+)", IdsEvaluator, Utils.REO_ci);

            return text;
        }

        private static string RefsEvaluator(Match match)
        {
            return !_ExternalLinksRegex.IsMatch(match.Groups["href"].Value)
                       ? match.Groups["prefix"].Value + GetNormalizedSrc(match.Groups["href"].Value) + match.Groups["suffix"].Value
                       : match.Value.Insert(match.Value.Length - 2, "target=\"_blank\"");
        }

        private static string GetAnchorValue(string fileName)
        {
            var match = Regex.Match(fileName, @"\#(?<anchor>.+)", Utils.REO_c);
            return match.Success ? "#" + match.Groups["anchor"].Value : "";
        }

        private string IdsEvaluator(Match match)
        {
            string originalFileName = GetTrimmedFileName(this.FileName, true) + "#" + match.Groups["id"].Value;
            return _LinksMapping.Contains(originalFileName) ? match.Groups["prefix"].Value + ((string)_LinksMapping[originalFileName]).Replace("#", "") : match.Value;
        }

        private static string GetTrimmedFileName(string fileName, bool removeAnchor)
        {
            Match m = Regex.Match(fileName, @"/?(?<fileName>[^/]+)$", Utils.REO_c);
            if (m.Success)
            {
                if (removeAnchor)
                {
                    string fileNameWithoutAnchor = Regex.Replace(m.Groups["fileName"].Value, @"\#.*", "", Utils.REO_c);
                    return fileNameWithoutAnchor.Trim() != string.Empty ? fileNameWithoutAnchor : null;
                }
                return m.Groups["fileName"].Value;
            }
            return null;
        }

        #endregion HTML Content Retrival

    }
}
