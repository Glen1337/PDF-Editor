using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PDFConverter
{
    /// <summary>
    /// Interaction logic for ResultDialog.xaml
    /// </summary>
    public partial class ResultDialog : UserControl
    {
        public ResultDialog(String result)
        {
            InitializeComponent();

            this.TextBox.Text = result;

            // Add event to ok button to close parent window when its clicked
            this.OkButton.Click += delegate(object sender, RoutedEventArgs e)
            {
                Window.GetWindow(this).Close();
            };
        }

        // When copy button is clicked, copy text button events to clipboard
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try { System.Windows.Clipboard.SetText(this.TextBox.Text); }
            catch { }
        }
        
    }
}
