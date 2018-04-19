using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using iTextSharp.text.pdf.parser;

namespace PDFConverter.Model
{
    public abstract class PdfEditorBase
    {
        protected String PdfPath { get; private set; }
        protected BackgroundWorker Bw { get; private set; }
        protected Logger myLogger { get; private set; }
        protected Stopwatch timer { get; private set; }
        protected long counter { get; private set; }

        public PdfEditorBase(String inPdfPath, BackgroundWorker inBw)
        {
            PdfPath = inPdfPath;
            Bw = inBw;
        }

        protected virtual void BeforeProcessing()
        {
            // Check PDF path
            if (!PdfPath.isFilePathOK(".pdf"))
            {
                throw new System.IO.FileNotFoundException(String.Concat("File not found: ", PdfPath));
            }

            // Open Logger
            try { myLogger = new Logger(String.Concat(System.IO.Path.GetDirectoryName(PdfPath), "\\ErrorLog.txt")); }
            catch 
            {
                throw; 
            }
            
            // Start Timer
            timer = new Stopwatch();
            timer.Start();

            // Start counter at 0
            counter = 0;
            
            if (Bw == null)
            {
                throw new ArgumentNullException("Background Worker is null");
            }
        }

        protected virtual void AfterProcessing()
        {
            try { myLogger.Dispose(); } catch { }
            try { timer.Stop(); } catch { }
        }

        public static ITextExtractionStrategy MakeRectangle(float pixelDistanceFromLeft, float pixelDistanceFromBottom, float pixelDistanceWidth, float pixelDistanceHeight)
        {
            var rectangle = new System.util.RectangleJ(pixelDistanceFromLeft, pixelDistanceFromBottom, pixelDistanceWidth, pixelDistanceHeight);

            var filters = new RenderFilter[1];

            filters[0] = new RegionTextRenderFilter(rectangle);

            ITextExtractionStrategy strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), filters);

            return strategy;
        }

        public static String ExtractWP(String inString, Boolean blank)
        {
            String wp = String.Empty;
            // –
            System.Text.RegularExpressions.Regex wpRegex = new System.Text.RegularExpressions.Regex(@"^\d{4}–");
            wp = wpRegex.Match(inString).Value;
            if (blank) { wp = wp.Replace("/blank", " ").Replace('–', ' '); }
            else { wp = wp.Replace('–', ' '); }
            return wp.Trim();
        }

    }
}
