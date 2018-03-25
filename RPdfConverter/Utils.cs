using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Ookii.Dialogs.Wpf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text;
using iTextSharp;

namespace PDFConverter
{
    public static class Utils
    {
        public static String nl = Environment.NewLine;

        public static String n(int num)
        {
            String s = String.Empty;
            if (num < 1) { return s; }

            try { s = String.Concat(Enumerable.Repeat<String>(Environment.NewLine, num)); }
            catch { }

            return s;
        }

        public static void ShowProcessFinishedDialog(String ProcessResult)
        {
            if (TaskDialog.OSSupportsTaskDialogs)
            {
                TaskDialog td = new TaskDialog()
                {
                    WindowTitle = "PDF Processing Complete",
                    Content = ProcessResult
                };

                td.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                td.ShowDialog();
            }
        }

        // Takes seconds arg, rounds and returns string of minutes if seconds are >60 and seconds if <60
        public static String PrintTimeFromSeconds(this double inSeconds, int places = 2)
        {
            //const int places = 2;

            if (inSeconds <= 0.0) { return "0 seconds"; }

            try
            {
                return (inSeconds < 60) ? (String.Concat((Math.Round(inSeconds, places, MidpointRounding.AwayFromZero)).ToString(), " seconds"))
                                        : (String.Concat((Math.Round((inSeconds / 60.00d), places)).ToString(), " minutes"));
            }
            catch { return ""; }
        }

        // Returns true if file is valid, accessible, and has correct file extension
        public static Boolean isFilePathOK(this String inFilePath, String extension)
        {
            return (inFilePath.isFilePathOK() &&
                    String.Equals(System.IO.Path.GetExtension(inFilePath).ToUpper().Trim(),
                                  extension.ToUpper().Trim())
                   );
        }

        // Returns true if file is valid and accessible
        public static Boolean isFilePathOK(this String inFilePath)
        {
            FileInfo fi = null;

            if (String.IsNullOrWhiteSpace(inFilePath)) { return false; }

            try { fi = new FileInfo(inFilePath); }
            catch { return false; }

            if (!File.Exists(inFilePath) || fi == null) { return false; }

            return true;
        }

        // Returns true if directory is a valid and accessible directory
        public static Boolean isDirectoryPathOK(this String inDirectoryPath)
        {
            DirectoryInfo di = null;

            if (String.IsNullOrWhiteSpace(inDirectoryPath)) { return false; }

            try { di = new DirectoryInfo(inDirectoryPath); }
            catch { return false; }

            if (!Directory.Exists(inDirectoryPath) || di == null) { return false; }

            return true;
        }

        // Gives percentage value of current page
        public static Int32 GetPercentage(Int32 current, Int32 total)
        {
            //Math.Ceiling(Convert.ToInt32())
            if (current < 0) { throw new ArgumentException("Invalid argument", "current"); }
            if (total < 1) { throw new ArgumentException("Invalid argument", "total"); }

            return ((Int32)Math.Round((((double)current / (double)total) * 100), MidpointRounding.AwayFromZero));
        }

        // Truncates double after given # of decimal places
        public static String truncstring(this double inDouble, int numPlaces = 3)
        {
            if (numPlaces < 0)
            {
                throw new ArgumentOutOfRangeException("numPlaces", "Cannot truncate argument to less than 0 places");
            }

            return inDouble.ToString(String.Concat("###.", String.Concat(Enumerable.Repeat<String>("0", numPlaces))));

            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings
            //Char[] placeArray;
            //placeArray = new Char[numPlaces];
        }

        public static String ExtractWP(String inString, Boolean blank)
        {
            String wp = String.Empty;
            // –
            Regex wpRegex = new Regex(@"^\d{4}–");
            wp = wpRegex.Match(inString).Value;
            if (blank) { wp = wp.Replace("/blank", " ").Replace('–', ' '); }
            else { wp = wp.Replace('–', ' '); }
            return wp.Trim();
        }
    }
}
