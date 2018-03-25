using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using iTextSharp;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text;

namespace PDFConverter.Model
{
    public class DataExporter : PdfEditorBase, IDataExporter
    {
        static private readonly Regex indexRegex = new Regex(@"\s*(\r|\r\n|\n)*\s*Index–\d{1,5}\s*(\r|\r\n|\n)*\s*$");
        static private readonly Regex LetterRegex = new Regex(@"((\r|\r\n|\n)+|^)([A-Za-z]{1})$");
        static private readonly Regex OfficialUseRegex = new Regex(@"FOROFFICIALUSEONLY");
        static private readonly Regex NumDashNumRegex = new Regex(@"\s*(\n|\r|\r\n)?\d{1,4}(\.\d*)*–\d{1,4}\s*(\n|\r|\r\n)?$");
        static private readonly Regex NumDashNumBlankRegex = new Regex(@"\s*(\n|\r|\r\n)?\d{1,4}(\.\d*)*–\d{1,4}(/blank)+\s*");
        static private readonly Regex romanNumRegex = new Regex(@"(\n|\r|\r\n)*(M{1,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})|M{0,4}(CM|C?D|D?C{1,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})|M{0,4}(CM|CD|D?C{0,3})(XC|X?L|L?X{1,3})(IX|IV|V?I{0,3})|M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|I?V|V?I{1,3}))$");

        private readonly String _ExportFilePath;
        private String ExportFilePath { get { return _ExportFilePath; } }

        public DataExporter(String inPathPdf, BackgroundWorker inBw, String inExportFile)
            : base(inPathPdf, inBw)
        {
            _ExportFilePath = inExportFile;
        }

        public String ExportData()
        {
            //Document variables

            DocInfo docInfo = new DocInfo();
            System.Boolean hasOfficialUse = false;
            string officialText;

            try
            {
                if (!ExportFilePath.isFilePathOK(".txt"))
                {
                    return "Invalid export file path: " + ExportFilePath;
                }

                BeforeProcessing();

                using (var pdfReader = new PdfReader(PdfPath))
                {
                    // For image checking
                    var parser = new PdfReaderContentParser(pdfReader);
                    ImageRenderListener listener = null;

                    // Check to see if doc has "for official use only" at the bottom                 
                    ITextExtractionStrategy officialTextRectangle = MakeRectangle(70, 1, 375, 120);
                    officialText = PdfTextExtractor.GetTextFromPage(pdfReader, 1, officialTextRectangle);
                    officialText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(officialText)));

                    if (officialText.ToString().ToUpper().Contains("FOROFFICIALUSEONLY"))
                    {
                        hasOfficialUse = true;
                    }
                    else { hasOfficialUse = false; }

                    // Loop through each page of the PDF
                    for (Int32 currentPage = 1; currentPage <= pdfReader.NumberOfPages; currentPage++)
                    {
                        PageInfo currentPageInfo = new PageInfo() { PageNum = currentPage };

                        ITextExtractionStrategy rectangleStrategy;

                        float height = pdfReader.GetPageSize(currentPage).Height;
                        float width = pdfReader.GetPageSize(currentPage).Width;

                        if (height > 785 && height < 802 && width > 1215 && width < 1230)
                        {
                            rectangleStrategy = MakeRectangle(450, 1, 450, 70);
                        }
                        else if (height > 785 && height < 802 && width > 608 && width < 617)
                        {
                            rectangleStrategy = MakeRectangle(190, 1, 255, 74);
                        }
                        else
                        {
                            myLogger.Log("Page # " + currentPage.ToString() + " not 8.5 x 11 or 11 x 17");
                            continue;
                        }

                        string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, currentPage, rectangleStrategy);
                        currentText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));

                        if (hasOfficialUse) { currentText = OfficialUseRegex.Replace(currentText, "").Trim(); }

                        ITextExtractionStrategy workPackageIndexStrategy = MakeRectangle(60, 600, 160, 50);
                        string WPI = PdfTextExtractor.GetTextFromPage(pdfReader, currentPage, workPackageIndexStrategy);
                        WPI = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(WPI)));

                        if (WPI.ToUpper().Contains("WORKPACKAGEINDEX"))
                        {
                            currentPageInfo.HasWpIndex = true;
                        }

                        // #-# 
                        if (NumDashNumRegex.IsMatch(currentText))
                        {
                            currentPageInfo.PageNumText = NumDashNumRegex.Match(currentText).Value.Trim();
                            currentPageInfo.IsWP = true;
                        }
                        else
                        {
                            // #-#/blank 
                            if (NumDashNumBlankRegex.IsMatch(currentText))
                            {
                                currentPageInfo.PageNumText = NumDashNumBlankRegex.Match(currentText).Value.Trim();
                                currentPageInfo.IsDashBlank = true;
                                currentPageInfo.IsWP = true;
                            }
                            else
                            {
                                if (romanNumRegex.IsMatch(currentText.ToUpper().Trim()))
                                {
                                    currentPageInfo.PageNumText = romanNumRegex.Match(currentText.ToUpper().Trim()).Value.Trim();

                                    if (String.Equals(currentPageInfo.PageNumText.ToUpper(), "C") || String.Equals(currentPageInfo.PageNumText.ToUpper(), "D"))
                                    {
                                        currentPageInfo.PageNumText = currentPageInfo.PageNumText.ToLower();
                                        currentPageInfo.IsLetter = true;
                                    }
                                    else
                                    {
                                        currentPageInfo.IsRoman = true;
                                    }
                                }
                                else
                                {
                                    if (LetterRegex.IsMatch(currentText.Trim()))
                                    {
                                        currentPageInfo.PageNumText = LetterRegex.Match(currentText).Value.Trim();
                                        currentPageInfo.IsLetter = true;
                                    }
                                    else
                                    {
                                        // Check if whole page is empty
                                        parser.ProcessContent(currentPage, (listener = new ImageRenderListener()));

                                        ITextExtractionStrategy currentTextRectangle = MakeRectangle(1, 1, 1000000, 1000000);

                                        String checkText = PdfTextExtractor.GetTextFromPage(pdfReader, currentPage, currentTextRectangle);
                                        checkText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(checkText)));

                                        if ((listener.Images.Count <= 0) && String.IsNullOrWhiteSpace(checkText))
                                        {
                                            currentPageInfo.IsWholePageEmpty = true;
                                            currentPageInfo.IsPageNumAreaBlank = true;
                                        }
                                        else
                                        {
                                            if (String.IsNullOrWhiteSpace(currentText))
                                            {
                                                currentPageInfo.IsPageNumAreaBlank = true;
                                            }
                                            else
                                            {
                                                if (indexRegex.IsMatch(currentText.Trim()))
                                                {
                                                    currentPageInfo.PageNumText = indexRegex.Match(currentText).Value.Trim();
                                                    currentPageInfo.IsIndex = true;
                                                }
                                                else
                                                {
                                                    currentPageInfo.PageNumText = currentText;
                                                    currentPageInfo.IsMisc = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (Bw.CancellationPending)
                        {
                            myLogger.Log("Processing cancelled at dwg #: " + currentPage.ToString());
                            break;
                        }

                        Bw.ReportProgress(Utils.GetPercentage(currentPage, pdfReader.NumberOfPages));

                        docInfo.Pages.Add(currentPageInfo);
                    }
                }

                WriteDocInfoToTextFile(docInfo);
            }
            catch (System.Exception se)
            {
                return se.Message;
            }
            finally
            {
                AfterProcessing();
            }
            
            return String.Concat(docInfo.ToString(),
                                 Environment.NewLine,
                                 "Processing completed in ",
                                 timer.Elapsed.TotalSeconds.PrintTimeFromSeconds(),
                                 Environment.NewLine,
                                 myLogger.ErrorCount.ToString(),
                                 " errors found.");

            //return String.Concat(
            //        docInfo.NumSheets,
            //        "Processing completed in ",
            //        timer.Elapsed.TotalSeconds.PrintTimeFromSeconds(),
            //        " with ",
            //        myLogger.ErrorCount,
            //        " errors.");
        }

        private void WriteDocInfoToTextFile(DocInfo docInfo)
        {
            Regex DashRegex = new Regex("–");
            Regex blankRegex = new Regex(@"/blank$");

            int convertedRoman = -1;
            String lastWpPageNum = String.Empty;
            string[] dashedNums = new string[2];
            int currentWpCount = 0;
            int wpCount = 0;
            string currentWorkPackageNumber = String.Empty;

            using (StreamWriter sw = new StreamWriter(ExportFilePath))
            {
                sw.Write("PDF Page #\tWork Package\t\tFull Page #" + Environment.NewLine + Environment.NewLine);

                foreach (PageInfo currentPageInfo in docInfo.Pages)
                {

                    if (Bw.CancellationPending || Bw == null)
                    {
                        myLogger.Log("Processing cancelled while exporting PDF information to text file");
                        break;
                    }

                    // Is Work Package ?
                    if (currentPageInfo.IsWP)
                    {
                        dashedNums = DashRegex.Split(currentPageInfo.PageNumText);

                        if (dashedNums[0] == currentWorkPackageNumber)
                        {
                            currentWpCount++;
                        }
                        else
                        {
                            if (currentWorkPackageNumber != String.Empty)
                            {
                                sw.Write("WP page count for WP" + currentWorkPackageNumber + ": " + currentWpCount + Environment.NewLine + Environment.NewLine);
                            }
                            wpCount++;
                            currentWorkPackageNumber = dashedNums[0];
                            currentWpCount = 1;
                        }
                        sw.Write(currentPageInfo.PageNum + "\t\t\t" + dashedNums[0] + "\t\t" + currentPageInfo.PageNumText + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);

                        continue;
                    }

                    // Is Dash Blank ?
                    if (currentPageInfo.IsDashBlank)
                    {
                        dashedNums = DashRegex.Split(blankRegex.Replace(currentPageInfo.PageNumText, " "));

                        if (dashedNums[0] == currentWorkPackageNumber)
                        {
                            currentWpCount++;
                        }
                        else
                        {
                            if (currentWorkPackageNumber != String.Empty)
                            {
                                sw.Write("WP page count for WP " + currentWorkPackageNumber + ": " + currentWpCount + Environment.NewLine + Environment.NewLine);
                            }
                            wpCount++;
                            currentWorkPackageNumber = dashedNums[0];
                            currentWpCount = 1;
                        }
                        sw.Write(currentPageInfo.PageNum + "\t\t\t" + dashedNums[0] + "\t\t" + currentPageInfo.PageNumText + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);

                        lastWpPageNum = currentWorkPackageNumber;
                        continue;
                    }

                    if (currentPageInfo.IsRoman)
                    {
                        convertedRoman = RomanToNumber(currentPageInfo.PageNumText.Trim().ToUpper());

                        sw.Write(currentPageInfo.PageNum + "\t\t\t\t\t" + currentPageInfo.PageNumText + " (" + convertedRoman + ")" + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);
                        continue;
                    }

                    if (currentPageInfo.IsLetter)
                    {
                        sw.Write(currentPageInfo.PageNum + "\t\t\t\t\t" + currentPageInfo.PageNumText + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);
                        continue;
                    }

                    if (currentPageInfo.IsMisc)
                    {
                        sw.Write(currentPageInfo.PageNum + "\t\t\t\t\t" + (currentPageInfo.PageNumText.Length > 15 ? currentPageInfo.PageNumText.Substring(0, 15) : currentPageInfo.PageNumText) + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);
                        continue;
                    }

                    if (currentPageInfo.IsPageNumAreaBlank)
                    {
                        sw.Write(currentPageInfo.PageNum + "\t\t\t\t\t" + "*No Page #*" + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);
                        continue;
                    }

                    if (currentPageInfo.IsWholePageEmpty)
                    {
                        sw.Write(currentPageInfo.PageNum + "\t\t\t\t\t" + "*Empty Page*" + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);
                        continue;
                    }

                    if (currentPageInfo.IsIndex)
                    {
                        sw.Write(currentPageInfo.PageNum + "\t\t\t\t\t" + currentPageInfo.PageNumText + (currentPageInfo.HasWpIndex ? " - WP INDEX" : "") + Environment.NewLine);
                        continue;
                    }
                }
                sw.Write("WP page count for WP " + currentWorkPackageNumber + ": " + currentWpCount + Environment.NewLine + Environment.NewLine);
                sw.Write("Number of total Work Packages: " + wpCount + Environment.NewLine);
            }
            //int i = docInfo.NumPrintable;
            return;
        }

        // Converts a single roman numeral to an integer.
        // Takes a roman numeral as a string and returns its integer equivalent.
        private static int RomanToNumber(String roman)
        {
            int number = 0;
            char nextChar = ' ';

            for (int i = 0; i < roman.Length; i++)
            {
                nextChar = (i + 1 < roman.Length) ? roman[i + 1] : '\0';

                if ((i < (roman.Length - 1) && !romanMap.ContainsKey(nextChar)) ||
                    (i == roman.Length - 1 && nextChar != '\0'))
                {
                    return -1;
                }

                if (i + 1 < roman.Length && romanMap[roman[i]] < romanMap[roman[i + 1]])
                {
                    number -= romanMap[roman[i]];
                }
                else
                {
                    number += romanMap[roman[i]];
                }
            }
            return number;
        }

        // Dictionary that provides roman numeral to integer mappings for roman to int conversion
        private static readonly Dictionary<char, int> romanMap = new Dictionary<char, int>()
        {
            {'I', 1},
            {'V', 5},
            {'X', 10},
            {'L', 50},
            {'C', 100},
            {'D', 500}, 
            {'M', 1000},
        };
    }

    public class DocInfo
    {
        public List<PageInfo> Pages = new List<PageInfo>();

        // Calculate page info for document lazily
        public DocInfo()
        {
            _NumSheets = new Lazy<Int32>(new Func<Int32>(() => Pages.Count()));
            _NumNonPrintable = new Lazy<Int32>(() => Pages.Where(s => s.IsWholePageEmpty).Count());
            _NumPrintable = new Lazy<Int32>(() => NumSheets - NumNonPrintable);
            _NumWorkPackages = new Lazy<Int32>(() => Pages.Where(s => s.IsWP).Count());
            _NumPageNums = new Lazy<Int32>(() => Pages.Where(s => !s.IsPageNumAreaBlank).Count());//new Func<Int32>(()=> Pages.Count));           
        }
 
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Total Pages: " + String.Format("{0:0000}", NumSheets) + Environment.NewLine);
            sb.Append("Non-printable pages: " + String.Format("{0:0000}", NumNonPrintable) + Environment.NewLine);
            sb.Append("Printable Pages: " + String.Format("{0:0000}", NumPrintable) + Environment.NewLine);
            sb.Append("Work packages: " + String.Format("{0:0000}", NumWorkPackages) + Environment.NewLine);
            sb.Append("Pages with page numbers: " + String.Format("{0:0000}", NumPageNums) + Environment.NewLine);
            return sb.ToString();
        }

        public Int32 NumPageNums { get { return _NumPageNums.Value; } }
        public Int32 NumSheets { get { return _NumSheets.Value; } }
        public Int32 NumNonPrintable { get { return _NumNonPrintable.Value; } }
        public Int32 NumWorkPackages { get { return _NumWorkPackages.Value; } }
        public Int32 NumPrintable { get { return _NumPrintable.Value; } }

        private Lazy<Int32> _NumPrintable;
        private Lazy<Int32> _NumPageNums;
        private Lazy<Int32> _NumSheets;
        private Lazy<Int32> _NumNonPrintable;
        private Lazy<Int32> _NumWorkPackages;
    }

    public class PageInfo
    {
        public PageInfo()
        {
            PageNumText = String.Empty;
            PageNum = 0;
            IsMisc = false;
            IsWP = false;
            IsRoman = false;
            IsPageNumAreaBlank = false;
            IsWholePageEmpty = false;
            IsDashBlank = false;
            IsLetter = false;
            IsIndex = false;
            HasWpIndex = false;
        }

        public String PageNumText { get; set; }
        public Int32 PageNum { get; set; }
        public Boolean IsMisc { get; set; }
        public Boolean IsWP { get; set; }
        public Boolean IsRoman { get; set; }
        public Boolean IsPageNumAreaBlank { get; set; }
        public Boolean IsWholePageEmpty { get; set; }
        public Boolean IsDashBlank { get; set; }
        public Boolean IsLetter { get; set; }
        public Boolean IsIndex { get; set; }
        public Boolean HasWpIndex { get; set; }
    }

    public class ImageRenderListener : IRenderListener
    {
        Dictionary<System.Drawing.Image, string> images = new Dictionary<System.Drawing.Image, string>();

        public Dictionary<System.Drawing.Image, string> Images
        {
            get { return images; }
        }

        public void BeginTextBlock() { }
        public void EndTextBlock() { }

        public void RenderImage(ImageRenderInfo renderInfo)
        {
            PdfImageObject image = renderInfo.GetImage();
            PdfName filter = (PdfName)image.Get(PdfName.FILTER);

            //int width = Convert.ToInt32(image.Get(PdfName.WIDTH).ToString()); 
            //int bitsPerComponent = Convert.ToInt32(image.Get(PdfName.BITSPERCOMPONENT).ToString()); 
            //string subtype = image.Get(PdfName.SUBTYPE).ToString(); 
            //int height = Convert.ToInt32(image.Get(PdfName.HEIGHT).ToString()); 
            //int length = Convert.ToInt32(image.Get(PdfName.LENGTH).ToString()); 
            //string colorSpace = image.Get(PdfName.COLORSPACE).ToString(); 

            /* It appears to be safe to assume that when filter == null, PdfImageObject  
             * does not know how to decode the image to a System.Drawing.Image. 
             *  
             * Uncomment the code above to verify, but when I’ve seen this happen,  
             * width, height and bits per component all equal zero as well. */
            if (filter != null)
            {
                System.Drawing.Image drawingImage = image.GetDrawingImage();

                string extension = ".";

                if (filter == PdfName.DCTDECODE)
                {
                    extension += PdfImageObject.ImageBytesType.JPG.FileExtension;
                }
                else if (filter == PdfName.JPXDECODE)
                {
                    extension += PdfImageObject.ImageBytesType.JP2.FileExtension;
                }
                else if (filter == PdfName.FLATEDECODE)
                {
                    extension += PdfImageObject.ImageBytesType.PNG.FileExtension;
                }
                else if (filter == PdfName.LZWDECODE)
                {
                    extension += PdfImageObject.ImageBytesType.CCITT.FileExtension;
                }

                /* Rather than struggle with the image stream and try to figure out how to handle  
                 * BitMapData scan lines in various formats (like virtually every sample I’ve found  
                 * online), use the PdfImageObject.GetDrawingImage() method, which does the work for us. */
                this.Images.Add(drawingImage, extension);
            }
        }
        public void RenderText(TextRenderInfo renderInfo) { }
    }
}
