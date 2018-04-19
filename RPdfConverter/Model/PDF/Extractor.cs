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
    class Extractor : PdfEditorBase, IExtractor
    {
        static private readonly Regex pageFieldRegex = new Regex(@"(\n|\r\n|\r)*\s*(\n|\r\n|\r)*\d{4}(\.\d*)*–\d{1,4}\s*(\n|\r\n|\r)*$");
        static private readonly Regex pageFieldBlankRegex = new Regex(@"(\n|\r\n|\r)*\s*(\n|\r\n|\r)*\d{4}(\.\d*)*–\d{1,4}/blank\s*(\n|\r\n|\r)*$");
        static private readonly Regex wpRegex = new Regex(@"^\d{4}$");

        private readonly String _outputFolder;
        private String outputFolder { get { return _outputFolder; } }

        private readonly String _WpFile;
        private String WpFile { get { return _WpFile; } }

        public Extractor(String inPathPdf, BackgroundWorker inBw, String inOutputFolder, String inWpFile)
            : base(inPathPdf, inBw)
        {
            _outputFolder = inOutputFolder;
            _WpFile = inWpFile;
        }

        public String Extract()
        {
            String currentPageField = String.Empty;
            byte[] byteArrayPdf;
            PdfContentByte cb;
            PdfImportedPage pageImport;
            Int32 totalPages = 0;

            try
            {
                if (!WpFile.isFilePathOK()) { return "Invalid WP file"; }

                if (!outputFolder.isDirectoryPathOK()) { return "Invalid output folder"; }

                BeforeProcessing();

                HashSet<String> WPs = GetWPsToExtract();

                using (PdfReader pdfReader = new PdfReader(PdfPath))
                {
                    using (Document doc = new Document(pdfReader.GetPageSize(1)))
                    {
                        using (FileStream outputStream = new FileStream(outputFolder + "\\ExtractedWPs.pdf", FileMode.Create))
                        {
                            PdfWriter writer = PdfWriter.GetInstance(doc, outputStream);
                            
                                totalPages = pdfReader.NumberOfPages;
                                ITextExtractionStrategy rectangleStrategy;

                                for (Int32 currentPage = 1; currentPage <= pdfReader.NumberOfPages; currentPage++)
                                {
                                    String currentWP = String.Empty;

                                    try { Bw.ReportProgress(Utils.GetPercentage(currentPage, pdfReader.NumberOfPages)); }
                                    catch { }

                                    if (Bw.CancellationPending || Bw == null)
                                    {
                                        myLogger.Log("Processing cancelled on PDF #" + currentPage);
                                        break;
                                    }

                                    float height = pdfReader.GetPageSize(currentPage).Height;
                                    float width = pdfReader.GetPageSize(currentPage).Width;

                                    if (height > 785 && height < 802 && width > 1215 && width < 1230)
                                    {
                                        // Page is 11 x 17
                                        rectangleStrategy = MakeRectangle(450, 1, 450, 70);
                                    }
                                    else if (height > 785 && height < 802 && width > 608 && width < 617)
                                    {
                                        // Page is 8.5 x 11
                                        rectangleStrategy = MakeRectangle(190, 1, 255, 74);
                                    }
                                    else
                                    {
                                        myLogger.Log("Page # " + currentPage.ToString() + " not 8.5 x 11 or 11 x 17");
                                        continue;
                                    }

                                    String currentText = PdfTextExtractor.GetTextFromPage(pdfReader, currentPage, rectangleStrategy);

                                    currentText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));

                                    if (pageFieldRegex.IsMatch(currentText))
                                    {
                                        currentPageField = pageFieldRegex.Match(currentText).Value.Trim();
                                        currentWP = ExtractWP(currentPageField, false);
                                    }
                                    else
                                    {
                                        if (pageFieldBlankRegex.IsMatch(currentText))
                                        {
                                            currentPageField = pageFieldBlankRegex.Match(currentText).Value.Trim();
                                            currentWP = ExtractWP(currentPageField, true);
                                        }
                                        else
                                        {
                                            // This page has no Page # field/WP, so skip it
                                            continue;
                                        }
                                    }
                                    if (WPs.Contains(currentWP))
                                    {
                                        // Write this page to document
                                        if (!doc.IsOpen()) { doc.Open(); }
                                        doc.NewPage();
                                        byteArrayPdf = pdfReader.GetPageContent(currentPage);
                                        cb = writer.DirectContent;
                                        pageImport = writer.GetImportedPage(pdfReader, currentPage);
                                        cb.AddTemplate(pageImport, 0, 0);
                                        writer.Flush();
                                    }
                                }
                                try { writer.Dispose(); } catch { }
                                // PdfWriter
                        }//FileStream
                    }//Doc
                }//PdfReader
            }
            catch(System.Exception se)
            {
                return se.Message;
            }
            finally
            {
                AfterProcessing();
            }

            return String.Concat(totalPages.ToString(),
                     " pages processed in ",
                     timer.Elapsed.TotalSeconds.PrintTimeFromSeconds(),
                     " with ",
                     myLogger.ErrorCount,
                     " errors.");
        }

        // Hashset is best for this case
        private HashSet<String> GetWPsToExtract()
        {
            String line = String.Empty;

            HashSet<String> WPsToExtract = new HashSet<String>();

            try 
            {
                using (StreamReader sr = new StreamReader(WpFile))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        WPsToExtract.Add(wpRegex.Match(line).Value.Trim());
                    }
                }
            }
            catch { throw new FileNotFoundException("Invalid work package file"); }

            return WPsToExtract;
        }
    }
}
