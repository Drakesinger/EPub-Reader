using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

using VersFx.Formats.Text.Epub;
using VersFx.Formats.Text.Epub.Entities;
using VersFx.Formats.Text.Epub.Schema;
using VersFx.Formats.Text.Epub.Schema.Navigation;
using VersFx.Formats.Text.Epub.Schema.Opf;

namespace EPubReader.Utilities
{
    class OldWay
    {

        private void OpenBookAndProperties(EpubBook epubBook)
        {
            // COMMON PROPERTIES

            // Book's title
            string title = epubBook.Title;

            // Book's authors (comma separated list)
            string author = epubBook.Author;

            // Book's authors (list of authors names)
            List<string> authors = epubBook.AuthorList;

            // Book's cover image (null if there are no cover)
            Image coverImage = epubBook.CoverImage;

            // CHAPTERS

            List<EpubChapter> chapters = epubBook.Chapters;

            // Enumerating chapters
            foreach (EpubChapter chapter in chapters)
            {
                // Title of chapter
                string chapterTitle = chapter.Title;

                // HTML content of current chapter
                string chapterHtmlContent = chapter.HtmlContent;

                // Nested chapters
                List<EpubChapter> subChapters = chapter.SubChapters;
            }

            // CONTENT

            // Book's content (HTML files, stlylesheets, images, fonts, etc.)
            EpubContent bookContent = epubBook.Content;


            // IMAGES

            // All images in the book (file name is the key)
            Dictionary<string, EpubByteContentFile> images = bookContent.Images;

            List<EpubByteContentFile> imagesList = new List<EpubByteContentFile>();
            foreach (string imgName in images.Keys)
            {
                EpubByteContentFile image;
                bool loadedImage = images.TryGetValue(imgName, out image);

                if (loadedImage)
                {
                    imagesList.Add(image);
                }
            }


            EpubByteContentFile firstImage = imagesList[0];

            // Content type (e.g. EpubContentType.IMAGE_JPEG, EpubContentType.IMAGE_PNG)
            EpubContentType contentType = firstImage.ContentType;

            // MIME type (e.g. "image/jpeg", "image/png")
            string mimeContentType = firstImage.ContentMimeType;

            // Creating Image class instance from content
            using (MemoryStream imageStream = new MemoryStream(firstImage.Content))
            {
                Image image = Image.FromStream(imageStream);
            }


            // HTML & CSS

            // All XHTML files in the book (file name is the key)
            Dictionary<string, EpubTextContentFile> htmlFiles = bookContent.Html;

            // All CSS files in the book (file name is the key)
            Dictionary<string, EpubTextContentFile> cssFiles = bookContent.Css;

            string htmlContent = "";

            // Entire HTML content of the book
            foreach (EpubTextContentFile htmlFile in htmlFiles.Values)
            {
                string htmlFileContent = htmlFile.Content;
                // Parse the content and update links.
                //CollectReplacementLinks(_LinksMapping, GetTrimmedFileName(_OpenFileDialog.FileName, false), htmlFileContent);
                htmlContent += /*NormalizeRefs*/(htmlFileContent);
            }


            // All CSS content in the book
            foreach (EpubTextContentFile cssFile in cssFiles.Values)
            {
                string cssContent = cssFile.Content;
            }

            // OTHER CONTENT

            // All fonts in the book (file name is the key)
            Dictionary<string, EpubByteContentFile> fonts = bookContent.Fonts;

            // All files in the book (including HTML, CSS, images, fonts, and other types of files)
            Dictionary<string, EpubContentFile> allFiles = bookContent.AllFiles;

            // ACCESSING RAW SCHEMA INFORMATION

            // EPUB OPF data
            EpubPackage package = epubBook.Schema.Package;

            // Enumerating book's contributors
            foreach (EpubMetadataContributor contributor in package.Metadata.Contributors)
            {
                string contributorName = contributor.Contributor;
                string contributorRole = contributor.Role;
            }

            // EPUB NCX data
            EpubNavigation navigation = epubBook.Schema.Navigation;

            // Enumerating NCX metadata
            foreach (EpubNavigationHeadMeta meta in navigation.Head)
            {
                string metadataItemName = meta.Name;
                string metadataItemContent = meta.Content;
            }
        }


    }
}
