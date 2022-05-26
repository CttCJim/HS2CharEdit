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

        int getStat(string propName, byte[] filebytes, int offset = 0, int len = 4)
            //offset is for when data is unlabeled and we have to go into the data from a known point forward (or backward)
            //len is for if a variable is more than 4 bytes
        {
            byte[] searchfor = Encoding.ASCII.GetBytes(propName);

            //find position of the stat in question
            var pos = Search(filebytes, searchfor) + propName.Length + offset + 1; //+1 for the delimiter character

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
            int gameval = (int)Math.Round(f);

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

                //separate the data from the image - this will make it easier to replace the image later
                byte[] searchfor = Encoding.ASCII.GetBytes("IEND"); //the 8 characters IEND®B`‚ represent the EOF for a PNG file. HS2/AIS data is appended after this EOF key.
                var IENDpos = Search(filebytes, searchfor); //get position of IEND
                byte[] pictobytes = filebytes.Skip(0).Take(IENDpos+7).ToArray(); //get all bytes from start until IEND+7 in order to get everything including IEND®B`‚
                byte[] databytes = filebytes.Skip(IENDpos+8).Take(filebytes.Length - pictobytes.Length).ToArray(); //get everything from IEND+8 til the end of the file

                //read Overall data
                txt_ovrlHeight.Text = getStat("shapeValueBody", databytes, 3).ToString();
                txt_ovrlHeight.IsReadOnly = false;
                txt_headSize.Text = getStat("shapeValueBody", databytes, 48).ToString();
                txt_headSize.IsReadOnly = false;

                //read Breast data
                txt_bustSize.Text = getStat("shapeValueBody", databytes, 8).ToString();
                txt_bustSize.IsReadOnly = false;
                txt_bustHeight.Text = getStat("shapeValueBody", databytes, 13).ToString();
                txt_bustHeight.IsReadOnly = false;
                txt_bustDirection.Text = getStat("shapeValueBody", databytes, 18).ToString();
                txt_bustDirection.IsReadOnly = false;
                txt_bustSpacing.Text = getStat("shapeValueBody", databytes, 23).ToString();
                txt_bustSpacing.IsReadOnly = false;
                txt_bustAngle.Text = getStat("shapeValueBody", databytes, 28).ToString();
                txt_bustAngle.IsReadOnly = false;
                txt_bustLength.Text = getStat("shapeValueBody", databytes, 33).ToString();
                txt_bustLength.IsReadOnly = false;
                txt_areolaSize.Text = getStat("areolaSize", databytes).ToString();
                txt_areolaSize.IsReadOnly = false;
                txt_areolaDepth.Text = getStat("shapeValueBody", databytes, 38).ToString();
                txt_areolaDepth.IsReadOnly = false;
                txt_bustSoftness.Text = getStat("bustSoftness", databytes).ToString();
                txt_bustSoftness.IsReadOnly = false;
                txt_bustWeight.Text = getStat("bustWeight", databytes).ToString();
                txt_bustWeight.IsReadOnly = false;
                txt_nippleWidth.Text = getStat("shapeValueBody", databytes, 43).ToString();
                txt_nippleWidth.IsReadOnly = false;
                txt_nippleDepth.Text = getStat("bustSoftness", databytes, -18).ToString();
                txt_nippleDepth.IsReadOnly = false;

                //read Upper Body data
                txt_neckWidth.Text = getStat("shapeValueBody", databytes, 53).ToString();
                txt_neckWidth.IsReadOnly = false;
                txt_neckThickness.Text = getStat("shapeValueBody", databytes, 58).ToString();
                txt_neckThickness.IsReadOnly = false;
                txt_shoulderWidth.Text = getStat("shapeValueBody", databytes, 63).ToString();
                txt_shoulderWidth.IsReadOnly = false;
                txt_shoulderThickness.Text = getStat("shapeValueBody", databytes, 68).ToString();
                txt_shoulderThickness.IsReadOnly = false;
                txt_chestWidth.Text = getStat("shapeValueBody", databytes, 73).ToString();
                txt_chestWidth.IsReadOnly = false;
                txt_chestThickness.Text = getStat("shapeValueBody", databytes, 78).ToString();
                txt_chestThickness.IsReadOnly = false;
                txt_waistWidth.Text = getStat("shapeValueBody", databytes, 83).ToString();
                txt_waistWidth.IsReadOnly = false;
                txt_waistThickness.Text = getStat("shapeValueBody", databytes, 88).ToString();
                txt_waistThickness.IsReadOnly = false;

                //read Lower body data
                txt_waistHeight.Text = getStat("shapeValueBody", databytes, 93).ToString();
                txt_waistHeight.IsReadOnly = false;
                txt_pelvisWidth.Text = getStat("shapeValueBody", databytes, 98).ToString();
                txt_pelvisWidth.IsReadOnly = false;
                txt_pelvisThickness.Text = getStat("shapeValueBody", databytes, 103).ToString();
                txt_pelvisThickness.IsReadOnly = false;
                txt_hipsWidth.Text = getStat("shapeValueBody", databytes, 108).ToString();
                txt_hipsWidth.IsReadOnly = false;
                txt_hipsThickness.Text = getStat("shapeValueBody", databytes, 113).ToString();
                txt_hipsThickness.IsReadOnly = false;
                txt_buttSize.Text = getStat("shapeValueBody", databytes, 118).ToString();
                txt_buttSize.IsReadOnly = false;
                txt_buttAngle.Text = getStat("shapeValueBody", databytes, 123).ToString();
                txt_buttAngle.IsReadOnly = false;

                //read Arms data
                txt_shoulderSize.Text = getStat("shapeValueBody", databytes, 148).ToString();
                txt_shoulderSize.IsReadOnly = false;
                txt_upperArms.Text = getStat("shapeValueBody", databytes, 153).ToString();
                txt_upperArms.IsReadOnly = false;
                txt_forearm.Text = getStat("shapeValueBody", databytes, 158).ToString();
                txt_forearm.IsReadOnly = false;

                //read Legs data

                txt_thighs.Text = getStat("shapeValueBody", databytes, 128).ToString();
                txt_thighs.IsReadOnly = false;
                txt_legs.Text = getStat("shapeValueBody", databytes, 133).ToString();
                txt_legs.IsReadOnly = false;
                txt_calves.Text = getStat("shapeValueBody", databytes, 138).ToString();
                txt_calves.IsReadOnly = false;
                txt_ankles.Text = getStat("shapeValueBody", databytes, 143).ToString();
                txt_ankles.IsReadOnly = false;

                //show image
                showCard.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            }
        }
    }
}
