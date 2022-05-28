using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Path = System.IO.Path;


namespace HS2CharEdit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //globals
        byte[] pictobytes = Array.Empty<byte>();
        byte[] pictobytes_restore = Array.Empty<byte>();
        byte[] databytes = Array.Empty<byte>();
        bool cardchanged = false;

        private void NumericBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
            if (!e.Handled) { cardchanged = true; }
        }

        private void HexBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[a-fA-F0-9]+$");
            e.Handled = regex.IsMatch(e.Text);
            if (!e.Handled) { cardchanged = true; }
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

        string getStatHex2(string propName, byte[] filebytes, string end, int offset = 0)
        {
            byte[] searchfor = Encoding.ASCII.GetBytes(propName);

            //find position of the stat in question
            var pos = Search(filebytes, searchfor) + propName.Length + offset;

            byte[] current;
            string curstring = "";
            string hexStr = "";

            while(curstring!=end)
            {
                current = filebytes.Skip(pos).Take(1).ToArray();
                curstring = BitConverter.ToString(current).ToLower();
                if(curstring!=end.ToLower())
                {
                    hexStr += curstring;
                    pos++;
                } else
                {
                    return hexStr;
                }
            }

            return hexStr;
        }

        string getStatHex(string propName, byte[] filebytes, int offset = 0, int len = 1)
        {
            byte[] searchfor = Encoding.ASCII.GetBytes(propName);

            //find position of the stat in question
            var pos = Search(filebytes, searchfor) + propName.Length + offset + 1; //+1 for the delimiter character

            //get bytes[] at position
            var hexNum = filebytes.Skip(pos).Take(len).ToArray();

            // Hexadecimal Representation of number
            string hexStr = BitConverter.ToString(hexNum);

            return hexStr;
        }

        float[] getStatColor(string propName, byte[] filebytes, int offset = 0)
        {
            //offset is for when data is unlabeled and we have to go into the data from a known point forward (or backward)
            //colors are stored as rrrr_gggg_bbbb_aaaa where a is alpha value
            //should return an array of [R,G,B,A]
            float[] gameval = {0,0,0,0};
            byte[] searchfor = Encoding.ASCII.GetBytes(propName);

            for (var i = 0; i < 4; i++)
            {
                //find position of the stat in question
                var pos = Search(filebytes, searchfor) + propName.Length + offset + 1 + (i * 5); //+1 for the delimiter character; +5 to get to the next color value

                //get bytes[] at position
                var hexNum = filebytes.Skip(pos).Take(4).ToArray();

                // Hexadecimal Representation of number
                string hexStr = BitConverter.ToString(hexNum);
                hexStr = hexStr.Replace("-", "");

                // Converting to integer
                Int32 IntRep = Int32.Parse(hexStr, NumberStyles.AllowHexSpecifier);
                // Integer to Byte[] and presenting it for float conversion
                float f = BitConverter.ToSingle(BitConverter.GetBytes(IntRep), 0);

                //multiply by 255 or 100 to get the int value ingame
                if(i<3)
                {
                    f *= 255; //colors are RGB 0-255
                } else
                {
                    f *= 100; //alpha is 0-100
                }

                //gameval[i] = (int)Math.Round(f);
                gameval[i] = f;

            }

            return gameval;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Title += Assembly.GetExecutingAssembly().GetName().Version;
        }


        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "png files (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true) {
                loadCard(openFileDialog.FileName);
            }
        }

        private void dropCard(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                var file = files[0];
                loadCard(file);
            }
        }

        private void loadCard(string cardfile)
        {
            if(cardchanged)
            {
                MessageBoxResult result = MessageBox.Show("You have unsaved changes! Load a new card?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
            }

            byte[] filebytes = File.ReadAllBytes(cardfile);

            //separate the data from the image - this will make it easier to replace the image later
            byte[] searchfor = Encoding.ASCII.GetBytes("IEND"); //the 8 characters IEND®B`‚ represent the EOF for a PNG file. HS2/AIS data is appended after this EOF key.
            var IENDpos = Search(filebytes, searchfor); //get position of IEND
            pictobytes_restore = pictobytes = filebytes.Skip(0).Take(IENDpos + 7).ToArray(); //get all bytes from start until IEND+7 in order to get everything including IEND®B`‚
            databytes = filebytes.Skip(IENDpos + 8).Take(filebytes.Length - pictobytes.Length).ToArray(); //get everything from IEND+8 til the end of the file

            ///START HEAD DATA///
            //read Facial Type data
            txt_headContour.Text = getStatHex2("headId", databytes, "a6").ToString();
            txt_headContour.IsReadOnly = false;
            txt_headSkin.Text = getStatHex2("skinId", databytes, "a8").ToString();
            txt_headSkin.IsReadOnly = false;
            txt_headWrinkles.Text = getStatHex2("detailId", databytes, "ab").ToString();
            txt_headWrinkles.IsReadOnly = false;
            txt_headWrinkleIntensity.Text = getStat("detailPower", databytes).ToString();
            txt_headWrinkleIntensity.IsReadOnly = false;

            //read Overall data
            txt_headWidth.Text = getStat("shapeValueFace", databytes, 3).ToString();
            txt_headWidth.IsReadOnly = false;
            txt_headUpperDepth.Text = getStat("shapeValueFace", databytes, 8).ToString();
            txt_headUpperDepth.IsReadOnly = false;
            txt_headUpperHeight.Text = getStat("shapeValueFace", databytes, 13).ToString();
            txt_headUpperHeight.IsReadOnly = false;
            txt_headLowerDepth.Text = getStat("shapeValueFace", databytes, 18).ToString();
            txt_headLowerDepth.IsReadOnly = false;
            txt_headLowerWidth.Text = getStat("shapeValueFace", databytes, 23).ToString();
            txt_headLowerWidth.IsReadOnly = false;

            //read Jaw data
            txt_jawWidth.Text = getStat("shapeValueFace", databytes, 28).ToString();
            txt_jawWidth.IsReadOnly = false;
            txt_jawHeight.Text = getStat("shapeValueFace", databytes, 33).ToString();
            txt_jawHeight.IsReadOnly = false;
            txt_jawDepth.Text = getStat("shapeValueFace", databytes, 38).ToString();
            txt_jawDepth.IsReadOnly = false;
            txt_jawAngle.Text = getStat("shapeValueFace", databytes, 43).ToString();
            txt_jawAngle.IsReadOnly = false;
            txt_neckDroop.Text = getStat("shapeValueFace", databytes, 48).ToString();
            txt_neckDroop.IsReadOnly = false;
            txt_chinSize.Text = getStat("shapeValueFace", databytes, 53).ToString();
            txt_chinSize.IsReadOnly = false;
            txt_chinHeight.Text = getStat("shapeValueFace", databytes, 58).ToString();
            txt_chinHeight.IsReadOnly = false;
            txt_chinDepth.Text = getStat("shapeValueFace", databytes, 63).ToString();
            txt_chinDepth.IsReadOnly = false;

            //read Cheeks data
            txt_cheekLowerHeight.Text = getStat("shapeValueFace", databytes, 68).ToString();
            txt_cheekLowerHeight.IsReadOnly = false;
            txt_cheekLowerDepth.Text = getStat("shapeValueFace", databytes, 73).ToString();
            txt_cheekLowerDepth.IsReadOnly = false;
            txt_cheekLowerWidth.Text = getStat("shapeValueFace", databytes, 78).ToString();
            txt_cheekLowerWidth.IsReadOnly = false;
            txt_cheekUpperHeight.Text = getStat("shapeValueFace", databytes, 83).ToString();
            txt_cheekUpperHeight.IsReadOnly = false;
            txt_cheekUpperDepth.Text = getStat("shapeValueFace", databytes, 88).ToString();
            txt_cheekUpperDepth.IsReadOnly = false;
            txt_cheekUpperWidth.Text = getStat("shapeValueFace", databytes, 93).ToString();
            txt_cheekUpperWidth.IsReadOnly = false;

            //read Eyebrows data
            txt_browWidth.Text = getStat("eyebrowLayout", databytes, 11).ToString();
            txt_browWidth.IsReadOnly = false;
            txt_browHeight.Text = getStat("eyebrowLayout", databytes, 16).ToString();
            txt_browHeight.IsReadOnly = false;
            txt_browPosX.Text = getStat("eyebrowLayout", databytes, 1).ToString();
            txt_browPosX.IsReadOnly = false;
            txt_browPosY.Text = getStat("eyebrowLayout", databytes, 6).ToString();
            txt_browPosY.IsReadOnly = false;
            txt_browAngle.Text = getStat("eyebrowTilt", databytes).ToString();
            txt_browAngle.IsReadOnly = false;

            //read Eyes data
            txt_eyeVertical.Text = getStat("shapeValueFace", databytes, 98).ToString();
            txt_eyeVertical.IsReadOnly = false;
            txt_eyeSpacing.Text = getStat("shapeValueFace", databytes, 103).ToString();
            txt_eyeSpacing.IsReadOnly = false;
            txt_eyeDepth.Text = getStat("shapeValueFace", databytes, 108).ToString();
            txt_eyeDepth.IsReadOnly = false;
            txt_eyeWidth.Text = getStat("shapeValueFace", databytes, 113).ToString();
            txt_eyeWidth.IsReadOnly = false;
            txt_eyeHeight.Text = getStat("shapeValueFace", databytes, 118).ToString();
            txt_eyeHeight.IsReadOnly = false;
            txt_eyeAngleZ.Text = getStat("shapeValueFace", databytes, 123).ToString();
            txt_eyeAngleZ.IsReadOnly = false;
            txt_eyeAngleY.Text = getStat("shapeValueFace", databytes, 128).ToString();
            txt_eyeAngleY.IsReadOnly = false;
            txt_eyeInnerDist.Text = getStat("shapeValueFace", databytes, 133).ToString();
            txt_eyeInnerDist.IsReadOnly = false;
            txt_eyeOuterDist.Text = getStat("shapeValueFace", databytes, 138).ToString();
            txt_eyeOuterDist.IsReadOnly = false;
            txt_eyeInnerHeight.Text = getStat("shapeValueFace", databytes, 143).ToString();
            txt_eyeInnerHeight.IsReadOnly = false;
            txt_eyeOuterHeight.Text = getStat("shapeValueFace", databytes, 148).ToString();
            txt_eyeOuterHeight.IsReadOnly = false;
            txt_eyelidShape1.Text = getStat("shapeValueFace", databytes, 153).ToString();
            txt_eyelidShape1.IsReadOnly = false;
            txt_eyelidShape2.Text = getStat("shapeValueFace", databytes, 158).ToString();
            txt_eyelidShape2.IsReadOnly = false;
            //txt_eyeOpenMax.Text = getStat("shapeValueFace", databytes, 163).ToString(); //wrong index - likely nose height
            //txt_eyeOpenMax.IsReadOnly = false;

            //read Nose data
            txt_noseHeight.Text = getStat("shapeValueFace", databytes, 163).ToString();
            txt_noseHeight.IsReadOnly = false;
            txt_noseDepth.Text = getStat("shapeValueFace", databytes, 168).ToString();
            txt_noseDepth.IsReadOnly = false;
            txt_noseAngle.Text = getStat("shapeValueFace", databytes, 173).ToString();
            txt_noseAngle.IsReadOnly = false;
            txt_noseSize.Text = getStat("shapeValueFace", databytes, 178).ToString();
            txt_noseSize.IsReadOnly = false;

            txt_bridgeHeight.Text = getStat("shapeValueFace", databytes, 183).ToString();
            txt_bridgeHeight.IsReadOnly = false;
            txt_bridgeWidth.Text = getStat("shapeValueFace", databytes, 188).ToString();
            txt_bridgeWidth.IsReadOnly = false;
            txt_bridgeShape.Text = getStat("shapeValueFace", databytes, 193).ToString();
            txt_bridgeShape.IsReadOnly = false;

            txt_nostrilWidth.Text = getStat("shapeValueFace", databytes, 198).ToString();
            txt_nostrilWidth.IsReadOnly = false;
            txt_nostrilHeight.Text = getStat("shapeValueFace", databytes, 203).ToString();
            txt_nostrilHeight.IsReadOnly = false;
            txt_nostrilLength.Text = getStat("shapeValueFace", databytes, 208).ToString();
            txt_nostrilLength.IsReadOnly = false;
            txt_nostrilInnerWidth.Text = getStat("shapeValueFace", databytes, 213).ToString();
            txt_nostrilInnerWidth.IsReadOnly = false;
            txt_nostrilOuterWidth.Text = getStat("shapeValueFace", databytes, 218).ToString();
            txt_nostrilOuterWidth.IsReadOnly = false;

            txt_noseTipLength.Text = getStat("shapeValueFace", databytes, 223).ToString();
            txt_noseTipLength.IsReadOnly = false;
            txt_noseTipHeight.Text = getStat("shapeValueFace", databytes, 228).ToString();
            txt_noseTipHeight.IsReadOnly = false;
            txt_noseTipSize.Text = getStat("shapeValueFace", databytes, 233).ToString();
            txt_noseTipSize.IsReadOnly = false;

            //read Mouth data
            txt_mouthHeight.Text = getStat("shapeValueFace", databytes, 238).ToString();
            txt_mouthHeight.IsReadOnly = false;
            txt_mouthWidth.Text = getStat("shapeValueFace", databytes, 243).ToString();
            txt_mouthWidth.IsReadOnly = false;
            txt_lipThickness.Text = getStat("shapeValueFace", databytes, 248).ToString();
            txt_lipThickness.IsReadOnly = false;
            txt_mouthDepth.Text = getStat("shapeValueFace", databytes, 253).ToString();
            txt_mouthDepth.IsReadOnly = false;
            txt_upperLipThick.Text = getStat("shapeValueFace", databytes, 258).ToString();
            txt_upperLipThick.IsReadOnly = false;
            txt_lowerLipThick.Text = getStat("shapeValueFace", databytes, 263).ToString();
            txt_lowerLipThick.IsReadOnly = false;
            txt_mouthCorners.Text = getStat("shapeValueFace", databytes, 268).ToString();
            txt_mouthCorners.IsReadOnly = false;

            //read Ears data
            txt_earSize.Text = getStat("shapeValueFace", databytes, 273).ToString();
            txt_earSize.IsReadOnly = false;
            txt_earAngle.Text = getStat("shapeValueFace", databytes, 278).ToString();
            txt_earAngle.IsReadOnly = false;
            txt_earRotation.Text = getStat("shapeValueFace", databytes, 283).ToString();
            txt_earRotation.IsReadOnly = false;
            txt_earUpShape.Text = getStat("shapeValueFace", databytes, 288).ToString();
            txt_earUpShape.IsReadOnly = false;
            txt_lowEarShape.Text = getStat("shapeValueFace", databytes, 293).ToString();
            txt_lowEarShape.IsReadOnly = false;

            //read Mole data
            txt_moleID.Text = getStatHex2("moleId", databytes,"a9").ToString();
            txt_moleID.IsReadOnly = false;
            txt_moleWidth.Text = getStat("moleLayout", databytes, 1).ToString();
            txt_moleWidth.IsReadOnly = false;
            txt_moleHeight.Text = getStat("moleLayout", databytes, 6).ToString();
            txt_moleHeight.IsReadOnly = false;
            txt_molePosX.Text = getStat("moleLayout", databytes, 11).ToString();
            txt_molePosX.IsReadOnly = false;
            txt_molePosY.Text = getStat("moleLayout", databytes, 16).ToString();
            txt_molePosY.IsReadOnly = false;

            float[] moleColors = getStatColor("moleColor", databytes, 1);
            txt_moleRed.Text = Math.Floor(moleColors[0]).ToString();
            txt_moleRed.IsReadOnly = false;
            txt_moleBlue.Text = Math.Floor(moleColors[1]).ToString();
            txt_moleBlue.IsReadOnly = false;
            txt_moleGreen.Text = Math.Floor(moleColors[2]).ToString();
            txt_moleGreen.IsReadOnly = false; 
            txt_moleAlpha.Text = Math.Floor(moleColors[3]).ToString();
            txt_moleAlpha.IsReadOnly = false;


            ///START BODY DATA///
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
            // showCard.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            showCard.Source = ToImage(pictobytes);

            //housekeeping
            cardchanged = false;

            //enable save button
            //btnSaveFile.IsEnabled = true;
        }

        private void btnSaveFile_Click(object sender, RoutedEventArgs e)
        {
            byte[] filebytes = new byte[databytes.Length+pictobytes.Length];
            pictobytes.CopyTo(filebytes, 0);
            databytes.CopyTo(filebytes, pictobytes.Length);
            //MessageBox.Show(filebytes.Length.ToString());
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "png files (*.png)|*.png";
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(saveFileDialog.FileName, filebytes);
                    MessageBox.Show(Path.GetFileName(saveFileDialog.FileName)+" saved.");
                    cardchanged=false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                
            }
        }

        public BitmapImage ToImage(byte[] array)
        {
            using (var ms = new System.IO.MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // here
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        private void loadNewPNG(object sender, RoutedEventArgs e)
        {
            //load a new png to do image replacement
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "png files (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                byte[] filebytes = File.ReadAllBytes(openFileDialog.FileName);

                //separate the data from the image - this will make it easier to replace the image later
                byte[] searchfor = Encoding.ASCII.GetBytes("IEND"); //the 8 characters IEND®B`‚ represent the EOF for a PNG file. HS2/AIS data is appended after this EOF key.
                var IENDpos = Search(filebytes, searchfor); //get position of IEND
                pictobytes = filebytes.Skip(0).Take(IENDpos + 7).ToArray(); //get all bytes from start until IEND+7 in order to get everything including IEND®B`‚
                showCard.Source = ToImage(pictobytes);
            }
        }

            private void loadOldPNG(object sender, RoutedEventArgs e)
        {
            //restore the original png that was loaded
            pictobytes = pictobytes_restore; //this doesnt work i think
            showCard.Source = ToImage(pictobytes);
            MessageBox.Show("Original card image restored.");
        }
        private void showAbout(object sender, RoutedEventArgs e)
        {
            //AboutBox1 p = new AboutBox1();
            //p.Show();
        }
    }
}
