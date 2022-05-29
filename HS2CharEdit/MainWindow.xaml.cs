using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        Window mainWindow;
        string VERSIONNUMBER = "0.1.1.1";
        byte[] pictobytes = Array.Empty<byte>();
        byte[] pictobytes_restore = Array.Empty<byte>();
        byte[] databytes = Array.Empty<byte>();
        bool loading = false;
        //finders are arrays of strings. Before getting data, the object will find each keyword in turn and start after the last one.
        static readonly string[] finders_facetype = { "headId" };
        static readonly string[] finders_skintype = { "bustWeight" };
        static readonly string[] finders_bodypaint1 = { "sunburnColor", "paintInfo" };
        static readonly string[] finders_bodypaint1a = { "sunburnColor", "paintInfo", "layoutId" };
        static readonly string[] finders_bodypaint2 = { "sunburnColor","paintInfo","rotation" };
        static readonly string[] finders_bodypaint2a = { "sunburnColor", "paintInfo", "rotation", "layoutId" };


        //Charstat(string cname, string dstyle, string pn, int ofst, string ender="")
        readonly Charstat[] allstats = {
        ///read Futanari
        //c2 for no, c3 for yes
        new Charstat("txt_futastate", "hex", "futanari", 0, "b0"),
        ///START HEAD DATA///
        //read Facial Type data
        new Charstat("txt_headContour", "hex", "headId", 0, "a6"),
        new Charstat("txt_headSkin", "hex", "skinId", 0, "a8", finders_facetype),
        new Charstat("txt_headWrinkles", "hex", "detailId", 0, "ab", finders_facetype ),
        new Charstat("txt_headWrinkleIntensity", "normal", "detailPower", 0,"", finders_facetype),
        //read Overall data
        new Charstat("txt_headWidth", "normal", "shapeValueFace", 3),
        new Charstat("txt_headUpperDepth", "normal", "shapeValueFace", 8),
        new Charstat("txt_headUpperHeight", "normal", "shapeValueFace", 13),
        new Charstat("txt_headLowerDepth", "normal", "shapeValueFace", 18),
        new Charstat("txt_headLowerWidth", "normal", "shapeValueFace", 23),
        //read Jaw data
        new Charstat("txt_jawWidth", "normal", "shapeValueFace", 28),
        new Charstat("txt_jawHeight", "normal", "shapeValueFace", 33),
        new Charstat("txt_jawDepth", "normal", "shapeValueFace", 38),
        new Charstat("txt_jawAngle", "normal", "shapeValueFace", 43),
        new Charstat("txt_neckDroop", "normal", "shapeValueFace", 48),
        new Charstat("txt_chinSize", "normal", "shapeValueFace", 53),
        new Charstat("txt_chinHeight", "normal", "shapeValueFace", 58),
        new Charstat("txt_chinDepth", "normal", "shapeValueFace", 63),
        //read Mole data
        new Charstat("txt_moleID", "hex", "moleId", 0, "a9"),
        new Charstat("txt_moleWidth", "normal", "moleLayout", 1),
        new Charstat("txt_moleHeight", "normal", "moleLayout", 6),
        new Charstat("txt_molePosX", "normal", "moleLayout", 11),
        new Charstat("txt_molePosY", "normal", "moleLayout", 16),
        new Charstat("txt_moleRed","color","moleColor",1,"0"),
        new Charstat("txt_moleGreen","color","moleColor",1,"1"),
        new Charstat("txt_moleBlue","color","moleColor",1,"2"),
        new Charstat("txt_moleAlpha","color","moleColor",1,"3"),
        //read Cheeks data
        new Charstat("txt_cheekLowerHeight", "normal", "shapeValueFace", 68),
        new Charstat("txt_cheekLowerDepth", "normal", "shapeValueFace", 73),
        new Charstat("txt_cheekLowerWidth", "normal", "shapeValueFace", 78),
        new Charstat("txt_cheekUpperHeight", "normal", "shapeValueFace", 83),
        new Charstat("txt_cheekUpperDepth", "normal", "shapeValueFace", 88),
        new Charstat("txt_cheekUpperWidth", "normal", "shapeValueFace", 93),
        //read Eyebrows data
        new Charstat("txt_browPosX", "normal", "eyebrowLayout", 1),
        new Charstat("txt_browPosY", "normal", "eyebrowLayout", 6),
        new Charstat("txt_browWidth", "normal", "eyebrowLayout", 11),
        new Charstat("txt_browHeight", "normal", "eyebrowLayout", 16),
        new Charstat("txt_browAngle", "normal", "eyebrowTilt"),
        //read Eyes data
        new Charstat("txt_eyeVertical", "normal", "shapeValueFace", 98),
        new Charstat("txt_eyeSpacing", "normal", "shapeValueFace", 103),
        new Charstat("txt_eyeDepth", "normal", "shapeValueFace", 108),
        new Charstat("txt_eyeWidth", "normal", "shapeValueFace", 113),
        new Charstat("txt_eyeHeight", "normal", "shapeValueFace", 118),
        new Charstat("txt_eyeAngleZ", "normal", "shapeValueFace", 123),
        new Charstat("txt_eyeAngleY", "normal", "shapeValueFace", 128),
        new Charstat("txt_eyeInnerDist", "normal", "shapeValueFace", 133),
        new Charstat("txt_eyeOuterDist", "normal", "shapeValueFace", 138),
        new Charstat("txt_eyeInnerHeight", "normal", "shapeValueFace", 143),
        new Charstat("txt_eyeOuterHeight", "normal", "shapeValueFace", 148),
        new Charstat("txt_eyelidShape1", "normal", "shapeValueFace", 153),
        new Charstat("txt_eyelidShape2", "normal", "shapeValueFace", 158),
        new Charstat("txt_eyeOpenMax", "normal", "eyesOpenMax"),
        //txt_eyeOpenMax
        //read Nose data
        new Charstat("txt_noseHeight", "normal", "shapeValueFace", 163),
        new Charstat("txt_noseDepth", "normal", "shapeValueFace", 168),
        new Charstat("txt_noseAngle", "normal", "shapeValueFace", 173),
        new Charstat("txt_noseSize", "normal", "shapeValueFace", 178),
        new Charstat("txt_bridgeHeight", "normal", "shapeValueFace", 183),
        new Charstat("txt_bridgeWidth", "normal", "shapeValueFace", 188),
        new Charstat("txt_bridgeShape", "normal", "shapeValueFace", 193),
        new Charstat("txt_nostrilWidth", "normal", "shapeValueFace", 198),
        new Charstat("txt_nostrilHeight", "normal", "shapeValueFace", 203),
        new Charstat("txt_nostrilLength", "normal", "shapeValueFace", 208),
        new Charstat("txt_nostrilInnerWidth", "normal", "shapeValueFace", 213),
        new Charstat("txt_nostrilOuterWidth", "normal", "shapeValueFace", 218),
        new Charstat("txt_noseTipLength", "normal", "shapeValueFace", 223),
        new Charstat("txt_noseTipHeight", "normal", "shapeValueFace", 228),
        new Charstat("txt_noseTipSize", "normal", "shapeValueFace", 233),
        //read Mouth data
        new Charstat("txt_mouthHeight", "normal", "shapeValueFace", 238),
        new Charstat("txt_mouthWidth", "normal", "shapeValueFace", 243),
        new Charstat("txt_lipThickness", "normal", "shapeValueFace", 248),
        new Charstat("txt_mouthDepth", "normal", "shapeValueFace", 253),
        new Charstat("txt_upperLipThick", "normal", "shapeValueFace", 258),
        new Charstat("txt_lowerLipThick", "normal", "shapeValueFace", 263),
        new Charstat("txt_mouthCorners", "normal", "shapeValueFace", 268),
        //read Ears data
        new Charstat("txt_earSize", "normal", "shapeValueFace", 273),
        new Charstat("txt_earAngle", "normal", "shapeValueFace", 278),
        new Charstat("txt_earRotation", "normal", "shapeValueFace", 283),
        new Charstat("txt_earUpShape", "normal", "shapeValueFace", 288),
        new Charstat("txt_lowEarShape", "normal", "shapeValueFace", 293),
        ///START BODY DATA///
        //read Overall data
        new Charstat("txt_ovrlHeight", "normal", "shapeValueBody", 3),
        new Charstat("txt_headSize", "normal", "shapeValueBody", 48),
        //read Breast data
        new Charstat("txt_bustSize", "normal", "shapeValueBody", 8),
        new Charstat("txt_bustHeight", "normal", "shapeValueBody", 13),
        new Charstat("txt_bustDirection", "normal", "shapeValueBody", 18),
        new Charstat("txt_bustSpacing", "normal", "shapeValueBody", 23),
        new Charstat("txt_bustAngle", "normal", "shapeValueBody", 28),
        new Charstat("txt_bustLength", "normal", "shapeValueBody", 33),
        new Charstat("txt_areolaSize", "normal", "areolaSize"),
        new Charstat("txt_areolaDepth", "normal", "shapeValueBody", 38),
        new Charstat("txt_bustSoftness", "normal", "bustSoftness"),
        new Charstat("txt_bustWeight", "normal", "bustWeight"),
        new Charstat("txt_nippleWidth", "normal", "shapeValueBody", 43),
        new Charstat("txt_nippleDepth", "normal", "bustSoftness", -18),
        //read Upper Body data
        new Charstat("txt_neckWidth", "normal", "shapeValueBody", 53),
        new Charstat("txt_neckThickness", "normal", "shapeValueBody", 58),
        new Charstat("txt_shoulderWidth", "normal", "shapeValueBody", 63),
        new Charstat("txt_shoulderThickness", "normal", "shapeValueBody", 68),
        new Charstat("txt_chestWidth", "normal", "shapeValueBody", 73),
        new Charstat("txt_chestThickness", "normal", "shapeValueBody", 78),
        new Charstat("txt_waistWidth", "normal", "shapeValueBody", 83),
        new Charstat("txt_waistThickness", "normal", "shapeValueBody", 88),
        //read Lower body data
        new Charstat("txt_waistHeight", "normal", "shapeValueBody", 93),
        new Charstat("txt_pelvisWidth", "normal", "shapeValueBody", 98),
        new Charstat("txt_pelvisThickness", "normal", "shapeValueBody", 103),
        new Charstat("txt_hipsWidth", "normal", "shapeValueBody", 108),
        new Charstat("txt_hipsThickness", "normal", "shapeValueBody", 113),
        new Charstat("txt_buttSize", "normal", "shapeValueBody", 118),
        new Charstat("txt_buttAngle", "normal", "shapeValueBody", 123),
        //read Arms data
        new Charstat("txt_shoulderSize", "normal", "shapeValueBody", 148),
        new Charstat("txt_upperArms", "normal", "shapeValueBody", 153),
        new Charstat("txt_forearm", "normal", "shapeValueBody", 158),
        //read Legs data
        new Charstat("txt_thighs", "normal", "shapeValueBody", 128),
        new Charstat("txt_legs", "normal", "shapeValueBody", 133),
        new Charstat("txt_calves", "normal", "shapeValueBody", 138),
        new Charstat("txt_ankles", "normal", "shapeValueBody", 143),
        ///START SKIN DATA///
        //read Skin Type data
        new Charstat("txt_skinType", "hex", "skinId", 0, "a8", finders_skintype ), //start searching after bustWeight to avoid grabbing head data instead of body
        new Charstat("txt_skinBuild", "hex", "detailId", 0, "ab",finders_skintype),
        new Charstat("txt_skinBuildDef", "normal", "detailPower", 0, "", finders_skintype),
        new Charstat("txt_skinRed","color","skinColor",1,"0"),
        new Charstat("txt_skinGreen","color","skinColor",1,"1"),
        new Charstat("txt_skinBlue","color","skinColor",1,"2"),
        new Charstat("txt_skinShine", "normal", "skinGlossPower"),
        new Charstat("txt_skinTexture", "normal", "skinMetallicPower"),
        //read Suntan data
        new Charstat("txt_tanType", "hex", "sunburnId", 0, "ac"),
        new Charstat("txt_tanRed","color","sunburnColor",1,"0"),
        new Charstat("txt_tanGreen","color","sunburnColor",1,"1"),
        new Charstat("txt_tanBlue","color","sunburnColor",1,"2"),
        new Charstat("txt_tanAlpha","color","sunburnColor",1,"3"),
        //read Nipple Skin data
        new Charstat("txt_nipType", "hex", "nipId", 0, "a8"),
        new Charstat("txt_nipRed","color","nipColor",1,"0"),
        new Charstat("txt_nipGreen","color","nipColor",1,"1"),
        new Charstat("txt_nipBlue","color","nipColor",1,"2"),
        new Charstat("txt_nipAlpha","color","nipColor",1,"3"),
        new Charstat("txt_nipShine", "normal", "nipGlossPower"),
        //read Pubic Hair data
        new Charstat("txt_pubeType", "hex", "underhairId", 0, "ae"),
        new Charstat("txt_pubeRed","color","underhairColor",1,"0"),
        new Charstat("txt_pubeGreen","color","underhairColor",1,"1"),
        new Charstat("txt_pubeBlue","color","underhairColor",1,"2"),
        new Charstat("txt_pubeAlpha","color","underhairColor",1,"3"),
        //read Fingernail data
        new Charstat("txt_nailRed","color","nailColor",1,"0"),
        new Charstat("txt_nailGreen","color","nailColor",1,"1"),
        new Charstat("txt_nailBlue","color","nailColor",1,"2"),
        new Charstat("txt_nailAlpha","color","nailColor",1,"3"),
        new Charstat("txt_nailShine", "normal", "nailGlossPower"),
        //read Body Paint 1 data
        new Charstat("txt_paint1Type", "hex", "id", 0, "a5",finders_bodypaint1),
        new Charstat("txt_paint1Red","color","color",1,"0",finders_bodypaint1),
        new Charstat("txt_paint1Green","color","color",1,"1",finders_bodypaint1),
        new Charstat("txt_paint1Blue","color","color",1,"2", finders_bodypaint1),
        new Charstat("txt_paint1Alpha","color","color",1,"3", finders_bodypaint1),
        new Charstat("txt_paint1Shine", "normal", "glossPower",0,"", finders_bodypaint1),
        new Charstat("txt_paint1Texture", "normal", "metallicPower",0,"", finders_bodypaint1),
        new Charstat("txt_paint1Position", "hex", "layoutId", 0, "a6", finders_bodypaint1),
        new Charstat("txt_paint1Width", "normal", "layout", 1,"", finders_bodypaint1a),
        new Charstat("txt_paint1Height", "normal", "layout", 6,"", finders_bodypaint1a),
        new Charstat("txt_paint1PosX", "normal", "layout", 11,"", finders_bodypaint1a),
        new Charstat("txt_paint1PosY", "normal", "layout", 16,"", finders_bodypaint1a),
        new Charstat("txt_paint1Rotation", "normal", "rotation", 0,"", finders_bodypaint1a),
        //read Body Paint 2 data
        new Charstat("txt_paint2Type", "hex", "id", 0, "a5",finders_bodypaint2),
        new Charstat("txt_paint2Red","color","color",1,"0",finders_bodypaint2),
        new Charstat("txt_paint2Green","color","color",1,"1",finders_bodypaint2),
        new Charstat("txt_paint2Blue","color","color",1,"2", finders_bodypaint2),
        new Charstat("txt_paint2Alpha","color","color",1,"3", finders_bodypaint2),
        new Charstat("txt_paint2Shine", "normal", "glossPower",0,"", finders_bodypaint2),
        new Charstat("txt_paint2Texture", "normal", "metallicPower",0,"", finders_bodypaint2),
        new Charstat("txt_paint2Position", "hex", "layoutId", 0, "a6", finders_bodypaint2),
        new Charstat("txt_paint2Width", "normal", "layout", 1,"", finders_bodypaint2a),
        new Charstat("txt_paint2Height", "normal", "layout", 6,"", finders_bodypaint2a),
        new Charstat("txt_paint2PosX", "normal", "layout", 11,"", finders_bodypaint2a),
        new Charstat("txt_paint2PosY", "normal", "layout", 16,"", finders_bodypaint2a),
        new Charstat("txt_paint2Rotation", "normal", "rotation", 0,"", finders_bodypaint2a),

        };

        bool cardchanged = false;

        //property array
        public class Charstat
        {
            public string displayval="";
            public string controlname="";
            public string propName; //name of string to start from when locating the data
            public string datastyle= "";
            public string end = "";
            //used to store terminator chars for hex vars, and color sequence IDs for colors.
            //datastyle values:
            //"hex": variable-length hex values loaded from a starting position to a terminating hex byte, displayed as a raw hex string
            //"color": 4x4-byte RGBa values read as a batch from a starting position. R,G,B are x255 to get ingame values; Alpha value is x100. Numbers must be rounded down.
            //"normal": 4-byte hex values loaded from a starting position, x100 and rounded to nearest int to get in-game value.
            public int offset;
            public int pos;
            public int idx=0;
            public string[] findfirst;

            public Charstat(string cname, string dstyle, string pn, int ofst = 0, string ender = "", string[]? ff = null)
            {
                controlname = cname;
                datastyle = dstyle;
                propName = pn;
                offset = ofst;
                end = ender;
                if(ff==null) { ff = Array.Empty<string>(); }
                findfirst = ff;
            }

            public void LoadData(byte[] filebytes)
            {
                //string to search for
                byte[] searchfor = Encoding.ASCII.GetBytes(propName);

                if(findfirst.Length>1)
                {
                    for(var i=0;i<findfirst.Length; i++)
                    {
                        //find position of the marker to start reading from
                        byte[] marker = Encoding.ASCII.GetBytes(findfirst[i]);
                        int starthere = Search(filebytes, marker);
                        //look at bytes starting from there for the first instance
                        filebytes = filebytes.Skip(starthere + findfirst[i].Length).ToArray();

                    }
                }

                string hexStr = "";
                switch (datastyle)
                {
                    case "hex":
                        {
                            pos = Search(filebytes, searchfor) + propName.Length + offset;

                            string curstring = "";
                            byte[] current;

                            while (curstring != end.ToLower())
                            {
                                current = filebytes.Skip(pos).Take(1).ToArray();
                                curstring = BitConverter.ToString(current).ToLower();
                                if (curstring != end.ToLower())
                                {
                                    hexStr += curstring;
                                    pos++;
                                }
                                else
                                {
                                    displayval = hexStr;
                                    break;
                                   // return hexStr;
                                }
                            }
                            break;
                        }
                    case "color":
                        {
                            float gameval;
                            idx = Int32.Parse(end);
                     
                            //find position of the stat in question
                            pos = Search(filebytes, searchfor) + propName.Length + offset + 1 + (idx * 5); //+1 for the delimiter character; +5 to get to the next color value

                            //get bytes[] at position
                            var hexNum = filebytes.Skip(pos).Take(4).ToArray();

                            // Hexadecimal Representation of number
                            hexStr = BitConverter.ToString(hexNum);
                            hexStr = hexStr.Replace("-", "");

                            // Converting to integer
                            Int32 IntRep = Int32.Parse(hexStr, NumberStyles.AllowHexSpecifier);
                            // Integer to Byte[] and presenting it for float conversion
                            float f = BitConverter.ToSingle(BitConverter.GetBytes(IntRep), 0);

                            //multiply by 255 or 100 to get the int value ingame
                            if (idx < 3)
                            {
                                f = (float)Math.Floor(f*255); //colors are RGB 0-255
                            }
                            else
                            {
                                f = (float)Math.Floor(f * 100); //alpha is 0-100
                            }

                            gameval = f;

                            displayval = gameval.ToString();
                        }

                        break;
                    case "normal":
                        {
                            //find position of the stat in question
                            pos = Search(filebytes, searchfor) + propName.Length + offset + 1; //+1 for the delimiter character

                            //get bytes[] at position
                            var hexNum = filebytes.Skip(pos).Take(4).ToArray();

                            // Hexadecimal Representation of number
                            hexStr = BitConverter.ToString(hexNum).Replace("-", "");

                            // Converting to integer
                            Int32 IntRep = Int32.Parse(hexStr, NumberStyles.AllowHexSpecifier);
                            // Integer to Byte[] and presenting it for float conversion
                            float f = BitConverter.ToSingle(BitConverter.GetBytes(IntRep), 0);

                            //multiply by 100 to get the int value ingame
                            f *= 100;
                            int gameval = (int)Math.Round(f);

                            displayval = gameval.ToString();
                            break;
                        }
                }

                //put the new value in the text box
                ((MainWindow)Application.Current.MainWindow).fillBox(controlname, displayval); //leave last argument empty to set ReadOnly false
            }

            public void Update(string contents)
            {
                //convert data to bytes and call public function to save it to the active memory copy of the card data

                //1 - convert content string to hex string
                byte[] content = Array.Empty<byte>();
                displayval = contents;

                switch (datastyle)
                {
                    case "hex":
                        {
                            //convert display value directly to bytes
                            content = StringToByteArray(displayval);
                            break;
                        }
                    case "color":
                        {
                            float f;
                            //divide by 255 or 100 to get the int value ingame
                            if (idx < 3)
                            {
                                //colors are RGB 0-255
                                f = float.Parse(displayval) / 255;
                            }
                            else
                            {
                                //alpha is 0-100
                                f = float.Parse(displayval) / 100;
                            }
                            content = ((MainWindow)Application.Current.MainWindow).FloatToHex(f);
                            break;
                        }
                    case "normal":
                        {
                            //convert back into float decimal ("71"->0.71)
                            float x = float.Parse(displayval) / 100;
                            //convert to byte array
                            content = ((MainWindow)Application.Current.MainWindow).FloatToHex(x);
                            
                            break;
                        }
                }

                //2 - call external func to save the data
                ((MainWindow)Application.Current.MainWindow).SaveData(content, pos, end);

            }
        }

        public void Futacheck(object sender, RoutedEventArgs e)
        {
            if(loading) { return; } //changed during a loading event

            //the futa checkbox has been changed by the user
            bool? fchecked = chk_futastate.IsChecked;
            if((fchecked.HasValue)&&(fchecked == true))
            {
                loading = true;
                txt_futastate.Text = "c3";
                loading = false;
            } else {
                loading = true;
                txt_futastate.Text = "c2";
                loading = false;
            }
        }

        public void futatxtChanged(object sender, RoutedEventArgs e)
        {
            loading = true;
            if(txt_futastate.Text=="c3")
            {
                //check box yes
                chk_futastate.IsChecked = true;
            } 
            else
            {
                //check box no
                chk_futastate.IsChecked = false;
            }
            loading = false;
        }


        public void SaveData(byte[] contentbytes, int pos, string end = "")
        {
            //save the content into the right place in a copy of databytes
            //using a copy here in case the array size changes
            byte[] before;
            byte[] after;
            int contentlength;
            if (end=="")
            {
                contentlength = contentbytes.Length;
            }
            else
            {
                //variable length; need to find out how long the original data was
                string curstring = "";
                byte[] current;
                int postemp = pos;

                while (curstring != end.ToLower())
                {
                    current = databytes.Skip(postemp).Take(1).ToArray();
                    curstring = BitConverter.ToString(current).ToLower();
                    if (curstring != end.ToLower())
                    {
                        postemp++;
                    }
                    else
                    {
                        break;
                    }
                }
                contentlength = postemp - pos;
            }

            //get bytes before and after the data
            before = databytes.Take(pos).ToArray();
            after = databytes.Skip(pos + contentlength).ToArray();
            
            byte[] combined = new byte[before.Length+ contentbytes.Length + after.Length];
            

            before.CopyTo(combined, 0);
            contentbytes.CopyTo(combined, pos);
            after.CopyTo(combined,pos+contentbytes.Length); //using length of new content

            //overwrite databytes with the new array
            databytes = combined;

            for (var i = 0; i < allstats.Length; i++)
            {
                allstats[i].LoadData(databytes);
            }

        }
        
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length / 2).Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16)).ToArray();
        }
        
        private void statChanged(object sender, TextChangedEventArgs args)
        {
            if(loading) { return; } //don't take action while loading a card!
            string boxname = ((TextBox)sender).Name;
            string data = ((TextBox)sender).Text;
            //find the object for this box and save changes to the copy in memory
            for (var i = 0; i < allstats.Length; i++)
            {
                if (allstats[i].controlname==boxname)
                {
                    allstats[i].Update(data);
                    break;
                }
            }
        }

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

        public MainWindow()
        {
            InitializeComponent();
            Title += VERSIONNUMBER;
            mainWindow = this;
            /*
            byte[] x = StringToByteArray("3fab56ac");
            string test = BitConverter.ToString(x);
            MessageBox.Show("3fab56ac" + " : " + test);
            */
            /*
            float x = float.Parse("67") / 100;
            byte[] lVal = BitConverter.GetBytes(x);
            Array.Reverse(lVal);
            string test = BitConverter.ToString(lVal);
            MessageBox.Show(test + " : " + x.ToString());
            */
        }

        public byte[] FloatToHex(float f)
        {
            byte[] hexes = BitConverter.GetBytes(f);
            Array.Reverse(hexes);
            return hexes;
        }

        private void WinLoaded(object sender, RoutedEventArgs e)
        {
            

        }

        public void fillBox(string boxname, string content, bool ro = false)
        {
            loading = true;
            TextBox? textBox = this.FindName(boxname) as TextBox;
            if (textBox != null)
            {
                textBox.Text = content;
                textBox.IsReadOnly = ro;
            }
            loading = false;
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "png files (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true) {
                LoadCard(openFileDialog.FileName);
            }
        }

        private void DropCard(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                var file = files[0];
                LoadCard(file);
            }
        }

        private void LoadCard(string cardfile)
        {
            
            if(cardchanged)
            {
                MessageBoxResult result = MessageBox.Show(this,"You have unsaved changes! Load a new card?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if(result== MessageBoxResult.No)
                {
                    return;
                }
            }


            byte[] filebytes = File.ReadAllBytes(cardfile);

            //separate the data from the image - this will make it easier to replace the image later
            byte[] searchfor = Encoding.ASCII.GetBytes("IEND"); //the 8 characters IEND®B`‚ represent the EOF for a PNG file. HS2/AIS data is appended after this EOF key.
            var IENDpos = Search(filebytes, searchfor); //get position of IEND
            pictobytes_restore = pictobytes = filebytes.Skip(0).Take(IENDpos + 8).ToArray(); //get all bytes from start until IEND+7 in order to get everything including IEND®B`‚
            databytes = filebytes.Skip(IENDpos + 8).ToArray(); //get everything from IEND+8 til the end of the file
            if(databytes.Length==0)
            {
                MessageBox.Show("The PNG you selected has no card data", "Card Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //load all stats from card data to text boxes
            for(var i=0;i<allstats.Length;i++)
            {
                allstats[i].LoadData(databytes);
            }
 
            //show image
            showCard.Source = ToImage(pictobytes);

            //housekeeping
            cardchanged = false;

            //enable save button
            //btnSaveFile.IsEnabled = true;
        }

        private void btnSaveFile_Click(object sender, RoutedEventArgs e)
        {
           // byte[] filebytes = new byte[pictobytes.Length];
           // pictobytes.CopyTo(filebytes, 0);
            byte[] filebytes = new byte[databytes.Length+pictobytes.Length];
            pictobytes.CopyTo(filebytes, 0);
            databytes.CopyTo(filebytes, pictobytes.Length);
           
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
                pictobytes = filebytes.Skip(0).Take(IENDpos + 8).ToArray(); //get all bytes from start until IEND+7 in order to get everything including IEND®B`‚
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

        private void btn_Paypal_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("If you want to show appreciation for development, you can send tips to:\ncontact@jaceybooks.com\nOpen Paypal in your browser now?",
                                          "Donate",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
            if(result== MessageBoxResult.Yes)
            {
                string url = "https://www.paypal.com/myaccount/transfer/homepage";
                Process.Start("explorer", url);
            }
        }

        private void btn_Patreon_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://patreon.com/cttcjim";
            Process.Start("explorer", url);
        }
    }
}
