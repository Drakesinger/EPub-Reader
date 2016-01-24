using eBdb.EpubReader;
using AngleSharp;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using mshtml;

namespace EPubReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Tools

        #region Regular Expressions
        
        private static Regex _RefsRegex = new Regex(@"(?<prefix><\w+[^>]*?href\s*=\s*(""|'))(?<href>[^""']*)(?<suffix>(""|')[^>]*>)", Utils.REO_ci);
        private static Regex _ExternalLinksRegex = new Regex(@"^\s*(http(s)?://|mailto:|ftp(s)?://)", Utils.REO_ci);
        private System.Collections.Hashtable _LinksMapping = new System.Collections.Hashtable(StringComparer.InvariantCultureIgnoreCase);
        
        #endregion Regular Expressions

        #region Paths
        
        private string debugPath = "\\bin\\Debug\\";
        private string releasePath = "\\bin\\Debug\\";
        private string baseDirPath = AppDomain.CurrentDomain.BaseDirectory;
        
        #endregion Paths

        #region Objects
        /// <summary>
        /// Document Event that tells if the browser has finished loading the document.
        /// Source: <see cref="http://stackoverflow.com/questions/31411328/custom-context-menu-for-wpf-webbrowser-control/34602392#34602392"/>.
        /// </summary>
        private HTMLDocumentEvents2_Event _docEvent;

        /// <summary>
        /// The HTML Parser.
        /// </summary>
        AngleSharp.Parser.Html.HtmlParser htmlParser = new AngleSharp.Parser.Html.HtmlParser();

        #endregion Objects

        #region States
        
        /// <summary>
        /// Boolean telling whether or not night mode is on.
        /// </summary>
        private bool nightModeEnabled = false;
        
        #endregion States

        #endregion Tools

        #region Attributes

        /// <summary>
        /// The ePub file.
        /// </summary>
        private Epub _EPub;

        private string EPubContent;

        //private Image bookCover;

        private string ePubFileName;

        private Uri _WelcomeScreenURI = new Uri("http://msdn.com");
        private List<NavPoint> TableOfContents;

        #endregion Attributes

        #region Public Functions

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();

            BookDocBrowser.Navigate("http://www.google.com");
            BookDocBrowser.LoadCompleted += delegate
            {
                if (_docEvent != null)
                {
                    _docEvent.oncontextmenu -= _docEvent_oncontextmenu;
                }

                if (BookDocBrowser.Document != null)
                {
                    _docEvent = (HTMLDocumentEvents2_Event)BookDocBrowser.Document;
                    _docEvent.oncontextmenu += _docEvent_oncontextmenu;
                }
            };

            
        }


        bool _docEvent_oncontextmenu(IHTMLEventObj pEvtObj)
        {
            WebBrowserShowContextMenu();
            return false;
        }

        private void WebBrowserShowContextMenu()
        {
            ContextMenu cm = FindResource("CustomContextMenu") as ContextMenu;
            if (cm == null) return;
            cm.PlacementTarget = BookDocBrowser;
            cm.IsOpen = true;
        }

        #endregion Initialization

        #endregion Public Functions

        #region Private Functions

        #region Events

        private void MenuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open a file chooser.
                Microsoft.Win32.OpenFileDialog _OpenFileDialog = new Microsoft.Win32.OpenFileDialog();
                _OpenFileDialog.DefaultExt = "epub";
                _OpenFileDialog.Filter = "EPub (*.epub)|*.epub|All Files (*.*)|*.*";
                Nullable<bool> _Results = _OpenFileDialog.ShowDialog();

                // Once a file has been chosen, open it as an ePub.
                if (_Results == true)
                {
                    ePubFileName = _OpenFileDialog.FileName;

                    // Load the file as an Epub object.
                    _EPub = new Epub(_OpenFileDialog.FileName);

                    // Process ePub information.

                    // Load the ePub content in HTML format.
                    EPubContent = _EPub.GetContentAsHtml();

                    // Get the table of contents.
                    TableOfContents = _EPub.TOC;

                    buildNavigation();

                    // Find a way to get the height of the whole book.
                    double ScreenHeight = BookDocBrowser.ActualHeight;


                    // Show the ePub in the web browser.
                    BookDocBrowser.NavigateToString(EPubContent);

                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Change the visibility of the BookDocBrowser.
            BookDocBrowser.Visibility = System.Windows.Visibility.Visible;
        }

        private void AddBookmark_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("When i figure out how to get the position, and the enclosing paragraph/div.");
            parseAndAdd();
        }

        private void NightMode_Click(object sender, RoutedEventArgs e)
        {
            if (EPubContent != null && EPubContent.Length > 0)
            {
                // Get the night mode CSS.
                string nightmodeCssFileName = "nightmode.css";
                string path = Path.Combine(baseDirPath.Remove(baseDirPath.Length - debugPath.Length), @"Styles\", nightmodeCssFileName);
                string cssLink = "<link href=\"" + path.Replace("\\", "/") + "\" rel=\"stylesheet\" type=\"text/css\" />";
                
                if (!nightModeEnabled)
                {
                    Match headermatch = Regex.Match(EPubContent, @"<head[^>]*>(?<head>.+?)</head>", Utils.REO_csi);
                    if (headermatch.Success)
                    {
                        string stylePattern = @"<style[^>]*>(?<style>.+?)</style>";
                        Match stylematch = Regex.Match(EPubContent, stylePattern, Utils.REO_csi);

                        if (stylematch.Success)
                        {
                            string styles = stylematch.Value;

                            // Add the css file link to the header.
                            styles += cssLink;

                            EPubContent = EPubContent.Replace(stylematch.Value, styles);

                            nightModeEnabled = true;

                            var nightModeMenuItem = FindName("NightMode") as MenuItem;
                            nightModeMenuItem.Header = "_Day Mode";
                        }
                    }
                }
                else
                {
                    EPubContent = EPubContent.Replace(cssLink, "");
                    nightModeEnabled = false;

                    var nightModeMenuItem = FindName("NightMode") as MenuItem;
                    nightModeMenuItem.Header = "_Night Mode";
                }
                // 2nd way.
                // Load the ePub content in HTML format and append the link to the night mode CSS file..
                //EPubContent = _EPub.GetContentAsHtml(path);

                // Show the ePub in the web browser.
                BookDocBrowser.NavigateToString(EPubContent);
                //BookDocBrowser.Refresh(true);
            }
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sorry, doesn't work yet. At least not how it should.");
            var ms = sender as MenuItem;
            if (ms != null)
            {
                // Could down-cast.

                string header = ms.Header.ToString();
                string source = ms.Tag as String;
                NavPoint result = TableOfContents.Find(delegate(NavPoint nav)
                {
                    return nav.Title == header;
                });

                if (result != null)
                {
                    // Found it.
                    string htmlContent = result.ContentData.GetContentAsHTMLText();
                    // Sadly this doesn't do it...
                    foreach (var child in result.Children)
                    {
                        htmlContent += result.ContentData.GetContentAsHTMLText();
                    }
                    BookNavDocBrowser.NavigateToString(htmlContent);
                    changeBetweenBrowsers(true);
                }
                
            }
        }

        /// <summary>
        /// Hides a browser and shows another one.
        /// </summary>
        /// <param name="navigated">Identifier of a change required after navigaton to a navpoint.</param>
        private void changeBetweenBrowsers(bool navigated)
        {
            if (navigated)
            {
                BookDocBrowser.Visibility = System.Windows.Visibility.Collapsed;
                BookNavDocBrowser.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                BookNavDocBrowser.Visibility = System.Windows.Visibility.Collapsed;
                BookDocBrowser.Visibility = System.Windows.Visibility.Visible;
            }
           
        }

        private void goBack_Click(object sender, RoutedEventArgs e)
        {
            changeBetweenBrowsers(false);
        }

        private void MenuFileClose_Click(object sender, RoutedEventArgs e)
        {
            // Save the bookmarks.

            // Save the current position.

            // Clear navigation.
            clearNavigation();

            // Close the ePub file.
            EPubContent = "";

            // Show the welcome page.
            //BookDocBrowser.Navigate(_WelcomeScreenURI);
            BookDocBrowser.Visibility = System.Windows.Visibility.Hidden;
        }

        private void MenuFileQuit_Click(object sender, RoutedEventArgs e)
        {
            // Close then quit.
            Close();
        }
        
        #endregion Events

        #region Other Functions

        private void parseAndAdd()
        {
            //Create a (re-usable) parser front-end.
            //htmlParser = new AngleSharp.Parser.Html.HtmlParser();
            
            //DocumentViewer -> could be used for pagination?

            //Source to be parsed.
            string source = EPubContent;
            //Parse source to document.
            var document = htmlParser.Parse(source);
            var head = document.Head;
            var styles = document.StyleSheets;
            //Do something with document like the following

            //Console.WriteLine("Serializing the (original) document:");
            //Console.WriteLine(document.DocumentElement.OuterHtml);

            var p = document.CreateElement("p");
            p.TextContent = "This is another paragraph.";

            Console.WriteLine("Inserting another element in the body ...");
            document.Body.AppendChild(p);
        }

        private void buildNavigation()
        {
            foreach (NavPoint navPoint in TableOfContents)
            {
                var menuItem = new System.Windows.Controls.MenuItem();
                menuItem.Header = navPoint.Title;
                menuItem.Tag = navPoint.Source; //"Text/cover.html"
                // To be converted to: <a href="#cover_html">
                menuItem.Click += new System.Windows.RoutedEventHandler(this.NavItem_Click);

                var navMenu = FindName("mNavigationMenu") as MenuItem;
                if (navMenu != null)
                {
                    navMenu.Items.Add(menuItem);
                }
            }
        }

        private void clearNavigation()
        {
            var navMenu = FindName("mNavigationMenu") as MenuItem;
            if (navMenu != null)
            {
                navMenu.Items.Clear();
            }
        }

        #endregion Other Functions

        #endregion Private Functions

        #region Utility Functions

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

        private static string GetNormalizedSrc(string originalSrc)
        {
            string trimmedFileName = GetTrimmedFileName(originalSrc, false);
            return trimmedFileName != null ? "#" + trimmedFileName.Replace('.', '_').Replace('#', '_') : null;
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

        private string IdsEvaluator(Match match)
        {
            string originalFileName = GetTrimmedFileName(ePubFileName, true) + "#" + match.Groups["id"].Value;
            return _LinksMapping.Contains(originalFileName) ? match.Groups["prefix"].Value + ((string)_LinksMapping[originalFileName]).Replace("#", "") : match.Value;
        }

        private static void CollectReplacementLinks(System.Collections.Hashtable linksMapping, string fileName, string text)
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

        private static string GetAnchorValue(string fileName)
        {
            var match = Regex.Match(fileName, @"\#(?<anchor>.+)", Utils.REO_c);
            return match.Success ? "#" + match.Groups["anchor"].Value : "";
        }

        #endregion Utility Functions

        
    }
}