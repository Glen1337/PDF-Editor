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
    class Splitter : PdfEditorBase, ISplitter
    {
        static private readonly Regex pageFieldRegex = new Regex(@"(\n|\r\n|\r)*\s*(\n|\r\n|\r)*\d{4}(\.\d*)*–\d{1,4}\s*(\n|\r\n|\r)*$");
        static private readonly Regex pageFieldBlankRegex = new Regex(@"(\n|\r\n|\r)*\s*(\n|\r\n|\r)*\d{4}(\.\d*)*–\d{1,4}/blank\s*(\n|\r\n|\r)*$");

        private readonly String _outputFolder;
        private String outputFolder { get { return _outputFolder; } }

        public Splitter(String inPathPdf, BackgroundWorker inBw, String inOutputFolder)
            : base(inPathPdf, inBw)
        {
            _outputFolder = inOutputFolder;
        }

        public String Split()
        {
            Int32 currentPage = 1;
            Int32 totalPages = 0;
            String currentPageField = String.Empty;
            String previousWP = String.Empty;
            String currentWP = String.Empty;

            FileStream outputStream = null; ;
            PdfWriter writer;
            PdfImportedPage pageImport;
            byte[] byteArrayPdf;
            PdfContentByte cb;
            Document doc = null;

            try
            {
                if (!outputFolder.isDirectoryPathOK()) { return "Invalid output folder"; }

                BeforeProcessing();

                //Document doc;
                PdfReader pdfReaderTemp = new PdfReader(PdfPath);
                Document docTemp = new Document(pdfReaderTemp.GetPageSize(currentPage));
                outputStream = new FileStream(outputFolder + "\\tmp.pdf", FileMode.OpenOrCreate);
                outputStream.Flush();
                writer = PdfWriter.GetInstance(docTemp, outputStream);
                outputStream.Close();
                pdfReaderTemp.Close();

                using (PdfReader pdfReader = new PdfReader(PdfPath))
                {
                    totalPages = pdfReader.NumberOfPages;
                    ITextExtractionStrategy rectangleStrategy;

                    /*Document*/
                    doc = new Document(pdfReader.GetPageSize(1));

                    for (currentPage = 1; currentPage <= pdfReader.NumberOfPages; currentPage++)
                    {
                        // Update progress bar
                        try { Bw.ReportProgress(Utils.GetPercentage(currentPage, pdfReader.NumberOfPages)); }
                        catch { }

                        // check if bw is trying to be cancelled
                        if (Bw == null || Bw.CancellationPending)
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
                                continue;
                            }
                        }

                        if (!String.Equals(previousWP, currentWP))
                        {
                            //current wp is different than previous

                            try { doc.Close(); }
                            catch { }
                            doc = new Document(pdfReader.GetPageSize(currentPage));
                            outputStream = new FileStream(outputFolder + "\\" + currentWP + ".pdf", FileMode.Create);

                            writer = PdfWriter.GetInstance(doc, outputStream);

                            byteArrayPdf = pdfReader.GetPageContent(currentPage);
                            doc.Open();
                            cb = writer.DirectContent;
                            pageImport = writer.GetImportedPage(pdfReader, currentPage);
                            cb.AddTemplate(pageImport, 0, 0);
                            writer.Flush();
                            previousWP = currentWP;
                        }
                        else
                        {
                            //current wp is the same as previous
                            doc.NewPage();
                            byteArrayPdf = pdfReader.GetPageContent(currentPage);
                            cb = writer.DirectContent;
                            pageImport = writer.GetImportedPage(pdfReader, currentPage);
                            cb.AddTemplate(pageImport, 0, 0);
                            writer.Flush();
                        }
                    }

                    //try { doc.Close(); }
                    //catch { }

                    try { File.Delete(outputFolder + "\\tmp.pdf"); }
                    catch { }
                }// using PdfReader pdfReader

            }
            catch (System.Exception se)
            {
                return se.Message;
            }
            finally
            {
                try { doc.Close(); }
                catch { }
                try { outputStream.Dispose(); }
                catch { }
                try { File.Delete(outputFolder + "\\tmp.pdf"); }
                catch { }
                AfterProcessing();
            }

            return String.Concat(totalPages.ToString(),
                                 " pages processed in ",
                                 timer.Elapsed.TotalSeconds.PrintTimeFromSeconds(),
                                 " with ",
                                 myLogger.ErrorCount,
                                 " errors.");
        }
    }
}
