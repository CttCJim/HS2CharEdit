using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Microsoft.Win32;

namespace HS2CharEdit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void NumericBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SelectAddress(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        private void SelectivelyIgnoreMouseButton(object sender,
            MouseButtonEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb != null)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus();
                }
            }
        }

        static int Search(byte[] src, byte[] pattern)
        {
            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }

        int getStat(string propName, byte[] filebytes, int len = 4)
        {
            byte[] searchfor = Encoding.ASCII.GetBytes(propName);

            //find position of the stat in question
            var pos = Search(filebytes, searchfor) + propName.Length + 1; //+1 for the delimiter character

            //get bytes[] at position
            var hexNum = filebytes.Skip(pos).Take(len).ToArray();

            // Hexadecimal Representation of number
            string hexStr = BitConverter.ToString(hexNum);
            hexStr = hexStr.Replace("-", "");

            // Converting to integer
            Int32 IntRep = Int32.Parse(hexStr, NumberStyles.AllowHexSpecifier);
            // Integer to Byte[] and presenting it for float conversion
            float f = BitConverter.ToSingle(BitConverter.GetBytes(IntRep), 0);

            //multiply by 100 to get the int value ingame
            f *= 100;
            int gameval = (int)f;

            return gameval;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true) { 
                byte[] filebytes = File.ReadAllBytes(openFileDialog.FileName);
                string result = System.Text.Encoding.UTF8.GetString(filebytes);

                txt_areolaSize.Text = getStat("areolaSize", filebytes).ToString();
                txt_areolaSize.IsReadOnly = false;

                //show image
                showCard.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            }
        }
    }
}
