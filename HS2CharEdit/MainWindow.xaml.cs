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
using System.Windows.Controls.Primitives;
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
        string VERSIONNUMBER = "0.2.1.2";
        byte[] pictobytes = Array.Empty<byte>();
        byte[] pictobytes_restore = Array.Empty<byte>();
        byte[] databytes = Array.Empty<byte>();
        int[] fullnameInts = new int[8]; //used for tracking length and size changes for fullname change
        int[] fullnameIntsPos = new int[8]; //used for tracking length and size changes for fullname change
        //positions displaced by character name - 5xpos, 1xsize

        bool loading = false;
        //finders are arrays of strings. Before getting data, the object will find each keyword in turn and start after the last one.
        static readonly string[] finders_facetype = { "headId" };
        static readonly string[] finders_skintype = { "bustWeight" };
        static readonly string[] finders_righteye = { "whiteColor", "whiteColor" };
        static readonly string[] finders_righteye_whites = { "whiteColor", };
        static readonly string[] finders_bodypaint1 = { "sunburnColor", "paintInfo" };
        static readonly string[] finders_bodypaint1a = { "sunburnColor", "paintInfo", "layoutId" };
        static readonly string[] finders_bodypaint2 = { "sunburnColor", "paintInfo", "rotation" };
        static readonly string[] finders_bodypaint2a = { "sunburnColor", "paintInfo", "rotation", "layoutId" };
        static readonly string[] finders_facepaint1 = { "lipGloss", "paintInfo" };
        static readonly string[] finders_facepaint1a = { "lipGloss", "paintInfo", "layoutId" };
        static readonly string[] finders_facepaint2 = { "lipGloss", "paintInfo", "rotation" };
        static readonly string[] finders_facepaint2a = { "lipGloss", "paintInfo", "rotation", "layoutId" };
        static readonly string[] finders_favor = { "hAttribute" };


        //Charstat(string cname, string dstyle, string pn, int ofst, string ender="")
        readonly Charstat[] allstats = {
        ///START PERSONALITY DATA///
        new Charstat("txt_charName", "fullname", "fullname", 1, "ab"),
        new Charstat("txt_birthMonth", "dec1byte", "birthMonth", 0, "a8"),
        new Charstat("txt_birthDay", "dec1byte", "birthDay", 0, "a9"),
        new Charstat("txt_personality", "dec1byte", "personality", 0, "instance01"),
        new Charstat("txt_trait", "dec1byte", "trait", 0, "a4"),
        new Charstat("txt_mentality", "dec1byte", "mind", 0, "aa"),
        new Charstat("txt_sextrait", "dec1byte", "hAttribute", 0, "b0"),
        new Charstat("txt_favor", "dec1byte", "Favor", 0, "a9",finders_favor,"sld_favor"),
        new Charstat("txt_slavery", "dec1byte", "Slavery", 0, "a6",null,"sld_slavery"),
        new Charstat("txt_enjoyment", "dec1byte", "Enjoyment", 0, "a8",null,"sld_enjoyment"),
        new Charstat("txt_aversion", "dec1byte", "Aversion", 0, "a7",null,"sld_aversion"),
        new Charstat("txt_dependence", "dec1byte", "Dependence", 0, "a5",null,"sld_dependence"),
        new Charstat("txt_broken", "dec1byte", "Broken", 0, "a5", null, "sld_broken"),
        new Charstat("txt_voiceRate", "normal", "voiceRate",0,"instance01",null,"sld_voiceRate"), //need to get 2nd instance
        ///read Futanari
        //c2 for no, c3 for yes
        new Charstat("txt_futastate", "hex", "futanari", 0, "b0"),
        ///START HAIR DATA///
        //hair checkboxes
        new Charstat("txt_match_hair", "hex", "sameSetting", 0, "ab"),
        new Charstat("txt_auto_hair_color", "hex", "autoSetting", 0, "ac"),
        new Charstat("txt_hair_axis_ctrl", "hex", "ctrlTogether", 0, "a5"),
        //hair types
        //txt_backHairType txt_bangsType txt_sideHairType txt_hairExtType
        /*
        new Charstat("txt_backHairType", "hex", "sameSetting", 0, "ab"),
        new Charstat("txt_bangsType", "hex", "sameSetting", 0, "ab"),
        new Charstat("txt_sideHairType", "hex", "sameSetting", 0, "ab"),
        new Charstat("txt_hairExtType", "hex", "sameSetting", 0, "ab"),
        */
        ///START HEAD DATA///
        //read Eye Shadow data
        new Charstat("txt_eyeshadowType", "hex", "eyeshadowId", 0, "ae"),
        new Charstat("txt_eyeshadowRed","color","eyeshadowColor",1,"0"),
        new Charstat("txt_eyeshadowGreen","color","eyeshadowColor",1,"1"),
        new Charstat("txt_eyeshadowBlue","color","eyeshadowColor",1,"2"),
        new Charstat("txt_eyeshadowAlpha","color","eyeshadowColor",1,"3"),
        new Charstat("txt_eyeshadowShine", "normal", "eyeshadowGloss"),
        //read Cheeks data
        new Charstat("txt_cheekType", "hex", "cheekId", 0, "aa"),
        new Charstat("txt_cheekRed","color","cheekColor",1,"0"),
        new Charstat("txt_cheekGreen","color","cheekColor",1,"1"),
        new Charstat("txt_cheekBlue","color","cheekColor",1,"2"),
        new Charstat("txt_cheekAlpha","color","cheekColor",1,"3"),
        new Charstat("txt_cheekShine", "normal", "cheekGloss"),
        //read Lips data
        new Charstat("txt_lipType", "hex", "lipId", 0, "a8"),
        new Charstat("txt_lipRed","color","lipColor",1,"0"),
        new Charstat("txt_lipGreen","color","lipColor",1,"1"),
        new Charstat("txt_lipBlue","color","lipColor",1,"2"),
        new Charstat("txt_lipAlpha","color","lipColor",1,"3"),
        new Charstat("txt_lipShine", "normal", "lipGloss"),
        //read Face Paint 1 data
        new Charstat("txt_paintf1Type", "hex", "id", 0, "a5",finders_facepaint1),
        new Charstat("txt_paintf1Red","color","color",1,"0",finders_facepaint1),
        new Charstat("txt_paintf1Green","color","color",1,"1",finders_facepaint1),
        new Charstat("txt_paintf1Blue","color","color",1,"2", finders_facepaint1),
        new Charstat("txt_paintf1Alpha","color","color",1,"3", finders_facepaint1),
        new Charstat("txt_paintf1Shine", "normal", "glossPower",0,"", finders_facepaint1),
        new Charstat("txt_paintf1Texture", "normal", "metallicPower",0,"", finders_facepaint1),
        new Charstat("txt_paintf1Position", "hex", "layoutId", 0, "a6", finders_facepaint1),
        new Charstat("txt_paintf1Width", "normal", "layout", 1,"", finders_facepaint1a),
        new Charstat("txt_paintf1Height", "normal", "layout", 6,"", finders_facepaint1a),
        new Charstat("txt_paintf1PosX", "normal", "layout", 11,"", finders_facepaint1a),
        new Charstat("txt_paintf1PosY", "normal", "layout", 16,"", finders_facepaint1a),
        new Charstat("txt_paintf1Rotation", "normal", "rotation", 0,"", finders_facepaint1a),
        //read Face Paint 2 data
        new Charstat("txt_paintf2Type", "hex", "id", 0, "a5",finders_facepaint2),
        new Charstat("txt_paintf2Red","color","color",1,"0",finders_facepaint2),
        new Charstat("txt_paintf2Green","color","color",1,"1",finders_facepaint2),
        new Charstat("txt_paintf2Blue","color","color",1,"2", finders_facepaint2),
        new Charstat("txt_paintf2Alpha","color","color",1,"3", finders_facepaint2),
        new Charstat("txt_paintf2Shine", "normal", "glossPower",0,"", finders_facepaint2),
        new Charstat("txt_paintf2Texture", "normal", "metallicPower",0,"", finders_facepaint2),
        new Charstat("txt_paintf2Position", "hex", "layoutId", 0, "a6", finders_facepaint2),
        new Charstat("txt_paintf2Width", "normal", "layout", 1,"", finders_facepaint2a),
        new Charstat("txt_paintf2Height", "normal", "layout", 6,"", finders_facepaint2a),
        new Charstat("txt_paintf2PosX", "normal", "layout", 11,"", finders_facepaint2a),
        new Charstat("txt_paintf2PosY", "normal", "layout", 16,"", finders_facepaint2a),
        new Charstat("txt_paintf2Rotation", "normal", "rotation", 0,"", finders_facepaint2a),
        //read Left/Right Eye data
        //weirdly, pupil = iris, black = pupil, and whites = whites.
        new Charstat("txt_leftIrisType", "hex", "pupilId", 0, "aa"),
        new Charstat("txt_rightIrisType", "hex", "pupilId", 0, "aa",finders_righteye),
        new Charstat("txt_leftIrisRed","color","pupilColor",1,"0"),
        new Charstat("txt_leftIrisGreen","color","pupilColor",1,"1"),
        new Charstat("txt_leftIrisBlue","color","pupilColor",1,"2"),
        new Charstat("txt_leftIrisAlpha","color","pupilColor",1,"3"),
        new Charstat("txt_rightIrisRed","color","pupilColor",1,"0",finders_righteye),
        new Charstat("txt_rightIrisGreen","color","pupilColor",1,"1",finders_righteye),
        new Charstat("txt_rightIrisBlue","color","pupilColor",1,"2",finders_righteye),
        new Charstat("txt_rightIrisAlpha","color","pupilColor",1,"3",finders_righteye),
        new Charstat("txt_leftIrisGlow", "normal", "pupilEmission", 0),
        new Charstat("txt_leftIrisWidth", "normal", "pupilW", 0),
        new Charstat("txt_leftIrisHeight", "normal", "pupilH", 0),
        new Charstat("txt_rightIrisGlow", "normal", "pupilEmission", 0,"",finders_righteye),
        new Charstat("txt_rightIrisWidth", "normal", "pupilW", 0,"",finders_righteye),
        new Charstat("txt_rightIrisHeight", "normal", "pupilH", 0,"",finders_righteye),
        new Charstat("txt_leftPupilType", "hex", "blackId", 0, "aa"),
        new Charstat("txt_rightPupilType", "hex", "blackId", 0, "aa",finders_righteye),
        new Charstat("txt_leftPupilRed","color","blackColor",1,"0"),
        new Charstat("txt_leftPupilGreen","color","blackColor",1,"1"),
        new Charstat("txt_leftPupilBlue","color","blackColor",1,"2"),
        new Charstat("txt_leftPupilAlpha","color","blackColor",1,"3"),
        new Charstat("txt_rightPupilRed","color","blackColor",1,"0",finders_righteye),
        new Charstat("txt_rightPupilGreen","color","blackColor",1,"1",finders_righteye),
        new Charstat("txt_rightPupilBlue","color","blackColor",1,"2",finders_righteye),
        new Charstat("txt_rightPupilAlpha","color","blackColor",1,"3",finders_righteye),
        new Charstat("txt_leftPupilWidth", "normal", "blackW", 0),
        new Charstat("txt_leftPupilHeight", "normal", "blackH", 0),
        new Charstat("txt_rightPupilWidth", "normal", "blackW", 0,"",finders_righteye),
        new Charstat("txt_rightPupilHeight", "normal", "blackH", 0,"",finders_righteye),
        new Charstat("txt_leftWhitesRed","color","whiteColor",1,"0"),
        new Charstat("txt_leftWhitesGreen","color","whiteColor",1,"1"),
        new Charstat("txt_leftWhitesBlue","color","whiteColor",1,"2"),
        new Charstat("txt_leftWhitesAlpha","color","whiteColor",1,"3"),
        new Charstat("txt_rightWhitesRed","color","whiteColor",1,"0",finders_righteye_whites),
        new Charstat("txt_rightWhitesGreen","color","whiteColor",1,"1",finders_righteye_whites),
        new Charstat("txt_rightWhitesBlue","color","whiteColor",1,"2",finders_righteye_whites),
        new Charstat("txt_rightWhitesAlpha","color","whiteColor",1,"3",finders_righteye_whites),
        //read Iris Settings data
        new Charstat("txt_irisHeightAdj", "normal", "pupilY", 0),
        new Charstat("txt_irisShadow", "normal", "pupilH", 0),
        //read Eye Highlights data
        new Charstat("txt_hlType", "hex", "hlId", 0, "a7"),
        new Charstat("txt_hlRed","color","hlColor",1,"0"),
        new Charstat("txt_hlGreen","color","hlColor",1,"1"),
        new Charstat("txt_hlBlue","color","hlColor",1,"2"),
        new Charstat("txt_hlAlpha","color","hlColor",1,"3"),
        new Charstat("txt_hlWidth", "normal", "hlLayout", 1),
        new Charstat("txt_hlHeight", "normal", "hlLayout", 6),
        new Charstat("txt_hlXAxis", "normal", "hlLayout", 11),
        new Charstat("txt_hlYAxis", "normal", "hlLayout", 16),
        new Charstat("txt_hlTilt", "normal", "hlTilt", 0),
        //read Eyebrow Type data
        new Charstat("txt_eyebrowType", "hex", "eyebrowId", 0, "ac"),
        new Charstat("txt_eyebrowRed","color","eyebrowColor",1,"0"),
        new Charstat("txt_eyebrowGreen","color","eyebrowColor",1,"1"),
        new Charstat("txt_eyebrowBlue","color","eyebrowColor",1,"2"),
        new Charstat("txt_eyebrowAlpha","color","eyebrowColor",1,"3"),
        //read Eyelash Type data
        new Charstat("txt_eyelashType", "hex", "eyelashesId", 0, "ae"),
        new Charstat("txt_eyelashRed","color","eyelashesColor",1,"0"),
        new Charstat("txt_eyelashGreen","color","eyelashesColor",1,"1"),
        new Charstat("txt_eyelashBlue","color","eyelashesColor",1,"2"),
        new Charstat("txt_eyelashAlpha","color","eyelashesColor",1,"3"),
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
            //"dec1byte": 1-byte hax value representing an integer. Used in Month and Day boxes.
            public int offset;
            public int pos;
            public int idx=0;
            public string[] findfirst;
            public int instance=0;
            public string slidername = "";

            public Charstat(
                string cname, string dstyle, 
                string pn, int ofst = 0, string ender = "", 
                string[]? ff = null, string sdrname = "")
            {
                controlname = cname;
                datastyle = dstyle;
                propName = pn;
                offset = ofst;
                end = ender;
                if(ff==null) { ff = Array.Empty<string>(); }
                findfirst = ff;
                slidername = sdrname;
            }

            public string GetDatastyle()
            {
                return datastyle;
            }

            private string ASCIItoHex(string Value)
            {
                StringBuilder sb = new StringBuilder();

                foreach (byte b in Value)
                {
                    sb.Append(string.Format("{0:x2}", b));
                }

                return sb.ToString();
            }

            public static string HexToASCII(String hexString)
            {
                try
                {
                    string ascii = string.Empty;

                    for (int i = 0; i < hexString.Length; i += 2)
                    {
                        String hs = string.Empty;

                        hs = hexString.Substring(i, 2);
                        uint decval = System.Convert.ToUInt32(hs, 16);
                        char character = System.Convert.ToChar(decval);
                        ascii += character;

                    }

                    return ascii;
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }

                return string.Empty;
            }

            public void LoadData(byte[] filebytes)
            {
                int starthere = 0;

                //string to search for
                byte[] searchfor = Encoding.ASCII.GetBytes(propName);

                //check findfirsts
                for(var i=0;i<findfirst.Length; i++)
                {
                    //find position of the marker to start reading from
                    byte[] marker = Encoding.ASCII.GetBytes(findfirst[i]);
                    starthere = Search(filebytes, marker);
                    //look at bytes starting from there for the first instance
                    //filebytes = filebytes.Skip(starthere + findfirst[i].Length).ToArray();
                    //actually no thats stupid, send the position to the search function instead

                }

                if(end.Length>=8)
                {
                    if (end[..8] == "instance")
                    {
                        string t = end.Substring(8,end.Length - 8);
                        instance = int.Parse(t);
                        end = "";
                    }

                }

                string hexStr = "";

                switch (datastyle)
                {
                    case "dec1byte":
                        {
                            pos = Search(filebytes, searchfor, instance, starthere) + propName.Length + offset;

                            string curstring;
                            byte[] current;

                            current = filebytes.Skip(pos).Take(1).ToArray();
                            curstring = BitConverter.ToString(current).ToLower();
                            hexStr = Convert.ToInt32(curstring, 16).ToString();
                            displayval = hexStr;

                            break;
                        }
                    case "fullname":
                    case "hex":
                        {
                            pos = Search(filebytes, searchfor, 0, starthere) + propName.Length + offset;
                            int oldpos = pos;

                            string curstring = "";
                            byte[] current;

                            while (curstring != end.ToLower())
                            {
                                current = filebytes.Skip(pos).Take(1).ToArray();
                                curstring = BitConverter.ToString(current).ToLower();
                                if (curstring != end.ToLower())
                                {
                                    hexStr += curstring;
                                    if (datastyle == "dec1byte")
                                    {
                                        hexStr = Convert.ToInt32(hexStr, 16).ToString();
                                        displayval = hexStr;
                                        break;
                                    }
                                    pos++;
                                }
                                else
                                {
                                    if (datastyle == "fullname")
                                    {
                                        hexStr = HexToASCII(hexStr);
                                    }
                                    displayval = hexStr;
                                    break;
                                   // return hexStr;
                                }
                            }
                            
                            pos = oldpos;

                            if(datastyle=="fullname")
                            {
                                //get extra ints
                                //currently doing this in the LoadCard function instead.
                            }
                            
                            break;
                        }
                    case "color":
                        {
                            float gameval;
                            idx = Int32.Parse(end);
                     
                            //find position of the stat in question
                            pos = Search(filebytes, searchfor, 0, starthere) + propName.Length + offset + 1 + (idx * 5); //+1 for the delimiter character; +5 to get to the next color value

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
                            pos = Search(filebytes, searchfor, instance, starthere) + propName.Length + offset + 1; //+1 for the delimiter character

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
                bool ro = false;
                if(datastyle=="fullname")
                {
                    ro = true;
                }
                //put the new value in the text box
                ((MainWindow)Application.Current.MainWindow).fillBox(controlname, displayval, ro); //leave last argument empty to set ReadOnly false
            }

            public void Update(string thedata)
            {
                //convert data to bytes and call public function to save it to the active memory copy of the card data

                //1 - convert content string to hex string
                byte[] content = Array.Empty<byte>();
              /*  if(displayval!=thedata)
                {
                    //make sure the box matches the data
                    ((MainWindow)Application.Current.MainWindow).fillBox(controlname, thedata);
                }*/
                displayval = thedata;

                

                switch (datastyle)
                {
                    case "dec1byte":
                        {
                            int i = int.Parse(displayval);
                            content = BitConverter.GetBytes(i).Take(1).ToArray();
                            break;
                        }
                    case "fullname":
                        {
                            content = StringToByteArray(ASCIItoHex(displayval));
                            ((MainWindow)Application.Current.MainWindow).SaveNameInts(content.Length);

                            break;
                        }
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
                string edr = end;
                if(datastyle=="dec1byte")
                {
                    edr = "1byte";
                }
                ((MainWindow)Application.Current.MainWindow).SaveData(content, pos, edr);
                if(datastyle=="fullname")
                {
                    //length may have changed. update lositions of all variables by loading them from the copy in memory.
                    ((MainWindow)Application.Current.MainWindow).updateAll();
                }
            }
        }

        public void axismatchcheck(object sender, RoutedEventArgs e)
        {
            checktotext(chk_axis_hair, txt_hair_axis_ctrl);
        }
        public void axishairtxtChanged(object sender, RoutedEventArgs e)
        {
            texttocheck(chk_axis_hair, txt_hair_axis_ctrl);
        }
        public void automatchcheck(object sender, RoutedEventArgs e)
        {
            checktotext(chk_auto_hair, txt_auto_hair_color);
        }
        public void autohairtxtChanged(object sender, RoutedEventArgs e)
        {
            texttocheck(chk_auto_hair, txt_auto_hair_color);
        }
        public void hairmatchcheck(object sender, RoutedEventArgs e)
        {
            checktotext(chk_match_hair, txt_match_hair);
        }
        public void hairmatchtxtChanged(object sender, RoutedEventArgs e)
        {
            texttocheck(chk_match_hair, txt_match_hair);
        }
        public void Futacheck(object sender, RoutedEventArgs e)
        {
            checktotext(chk_futastate, txt_futastate);
        }
        public void futatxtChanged(object sender, RoutedEventArgs e)
        {
            texttocheck(chk_futastate, txt_futastate);
        }

        public void checktotext(CheckBox thecheck, TextBox thetext)
        {
            if(loading) { return; } //changed during a loading event
            bool? fchecked = thecheck.IsChecked;
            if ((fchecked.HasValue) && (fchecked == true))
            {
                loading = true;
                thetext.Text = "c3";
                loading = false;
            }
            else
            {
                loading = true;
                thetext.Text = "c2";
                loading = false;
            }
        }
        public void texttocheck(CheckBox thecheck, TextBox thetext)
        {
            loading = true;
            if (thetext.Text == "c3")
            {
                //check box yes
                thecheck.IsChecked = true;
            }
            else
            {
                //check box no
                thecheck.IsChecked = false;
            }
            loading = false;
        }

        public void SaveNameInts(int namelen)
        {
            for(int i = 0; i < 8; i++)
            {
                //get new value of int
                int newlen = fullnameInts[i] + namelen;
                //convert to hex bytes
                byte[] td = BitConverter.GetBytes(newlen);

                if((i == 5)||(i == 7))
                {
                    //only 1 byte for this one
                    byte[] data = { td[0] };
                    SaveData(data, fullnameIntsPos[i]);
                } else
                {
                    byte[] data = { td[1], td[0] };
                    SaveData(data, fullnameIntsPos[i]);
                }
            }
        }

        public void SaveData(byte[] contentbytes, int pos, string end = "")
        {
            //save the content into the right place in a copy of databytes
            //using a copy here in case the array size changes
            byte[] before;
            byte[] after;
            int contentlength;
            string[] validEnds = { "","1byte","0","1","2","3"};
            if(validEnds.Contains(end))
            {
                contentlength = contentbytes.Length;
            }
            else
            {
                //variable length; need to find out how long the original data was
                string curstring = "";
                byte[] current;
                int postemp = pos+1; //start 1 forward because fullname has the same first and last character.

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
            /*
            for (var i = 0; i < allstats.Length; i++)
            {
                allstats[i].LoadData(databytes);
            }
            */
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
            textboxchanged(boxname, data);
        }

        private void dropSelectChanged(object sender, SelectionChangedEventArgs args)
        {
            if (loading) { return; }

            //not loading; user has changed the data
            string boxname = ((ComboBox)sender).Name;
            int idx = ((ComboBox)sender).SelectedIndex;
            if((boxname=="cbo_month")||(boxname=="cbo_day"))
            {
                idx += 1;
            }
            string tp = "0" + idx.ToString();
            tp = tp.Substring(tp.Length - 2);

            switch (boxname)
            {
                case "cbo_month":
                    txt_birthMonth.Text = tp;
                    break;
                case "cbo_day":
                    txt_birthDay.Text = tp;
                    break;
                case "cbo_personality":
                    txt_personality.Text = tp;
                    break;
                case "cbo_trait":
                    txt_trait.Text = tp;
                    break;
                case "cbo_mentality":
                    txt_mentality.Text = tp;
                    break;
                case "cbo_sextrait":
                    txt_sextrait.Text = tp;
                    break;
            }

        }

        private void droptextChanged(object sender, TextChangedEventArgs args)
        {
            string boxname = ((TextBox)sender).Name;
            string data = ((TextBox)sender).Text;

            if(!loading){ 
                //update the changes to memory
                textboxchanged(boxname, data);
                return;
            } else
            {
                //this is on card load
                //make sure the combobox matches
                switch(boxname)
                {
                    case "txt_birthMonth":
                        cbo_month.SelectedIndex = int.Parse(data) - 1;
                        //TODO: check for out of range day
                        break;
                    case "txt_birthDay":
                        cbo_day.SelectedIndex = int.Parse(data) - 1;
                        break;
                    case "txt_personality":
                        cbo_personality.SelectedIndex = int.Parse(data);
                        break;
                    case "txt_trait":
                        cbo_trait.SelectedIndex = int.Parse(data);
                        break;
                    case "txt_mentality":
                        cbo_mentality.SelectedIndex = int.Parse(data);
                        break;
                    case "txt_sextrait":
                        cbo_sextrait.SelectedIndex = int.Parse(data);
                        break;
                }
            }
        }

        private void textboxchanged(string boxname, string data)
        {
            //find the object for this box and save changes to the copy in memory
            for (var i = 0; i < allstats.Length; i++)
            {
                if (allstats[i].controlname == boxname)
                {
                    allstats[i].Update(data);
                    break;
                }
            }
        }

        private void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //get data
            double dval = ((Slider)sender).Value;
            int val = (int)Math.Round(dval);
            string data = val.ToString();
            string sdrname = ((Slider)sender).Name;

            //change txtbox value
            SliderToText(sdrname, data);

        }

        private void Slider_KeyUp(object sender, KeyboardEventArgs e)
        {
            //get data
            double dval = ((Slider)sender).Value;
            int val = (int)Math.Round(dval);
            string data = val.ToString();
            string sdrname = ((Slider)sender).Name;

            //change txtbox value
            SliderToText(sdrname, data);

        }

        public void SliderToText(string sdrname, string data)
        {

            //change txtbox value

            for (var i = 0; i < allstats.Length; i++)
            {
                if (allstats[i].slidername == sdrname)
                {
                    //put data in textbox control - not strictly necessary but good practice
                    TextBox? textBox = this.FindName(allstats[i].controlname) as TextBox;
                    if (textBox != null)
                    {
                        textBox.Text = data;
                    }

                    //put same data into the display label if it exists
                    string lblname = "dis_" + sdrname;
                    Label? thelabel = this.FindName(lblname) as Label;
                    if (thelabel != null)
                    {
                        thelabel.Content = data;
                    }

                    //update memory copy of the data
                    allstats[i].Update(data);
                    break;
                }
            }
        }

        public void TextToSlider(object sender, TextChangedEventArgs e)
        {
            string boxname = ((TextBox)sender).Name;
            string boxtext = ((TextBox)sender).Text;
            for (var i = 0; i < allstats.Length; i++)
            {
                if (allstats[i].controlname == boxname)
                {
                    string slidername = allstats[i].slidername;
                    
                    Slider? theSlider = this.FindName(slidername) as Slider;
                    if (theSlider != null)
                    {
                        theSlider.Value = int.Parse(boxtext);
                    }

                    //put same data into the display label if it exists
                    string lblname = "dis_" + slidername;
                    Label? thelabel = this.FindName(lblname) as Label;
                    if (thelabel != null)
                    {
                        thelabel.Content = boxtext;
                    }
                    break;
                }
            }
        }

        private void CheckNumBox(object sender, EventArgs e)
        {
            if (loading) { return; } //don't take action while loading a card!
            TextBox tb = (TextBox)sender;
            int numValue;

            //check if it's a number
            bool isNum = int.TryParse(tb.Text, out numValue);
            if(!isNum)
            {
                tb.Text = "0";
                MessageBox.Show("Invalid number detected. Reset to zero.");
            }

            //check if it's out of range (color values)
            //get object
            string ds="";
            int idx=0;
            for (var i = 0; i < allstats.Length; i++)
            {
                if (allstats[i].controlname == tb.Name)
                {
                     ds = allstats[i].datastyle;
                     idx = allstats[i].idx;
                }
            }

            int min=0;
            int max=0;
            bool hasLimits = false;
            switch(ds)
            {
                case "color":
                    {
                        min = 0;
                        if(idx==3)
                        {
                            max = 100;
                        } else
                        {
                            max = 255;
                        }
                        hasLimits = true;
                        break;
                    }
            }

            if(hasLimits)
            {
                if ((numValue < min) || (numValue > max))
                {
                    tb.Text = "0";
                    MessageBox.Show("Out of range (" + min.ToString() + "-" + max.ToString()+"). Reset to zero.");
                }
            }


            string boxname = tb.Name;
            string data = tb.Text;
            cardchanged = true;
            //find the object for this box and save changes to the copy in memory
            textboxchanged(boxname, data);
        }

        private void NameEnterChecker(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                //MessageBox.Show("Pressed enter.");
                //btn_editName.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                btn_editName.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        /*
        private void NumericBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9-]+");
            e.Handled = regex.IsMatch(e.Text);
            if (!e.Handled) { cardchanged = true; }
        }*/

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

        public MainWindow()
        {
            InitializeComponent();
            
            Title += VERSIONNUMBER;
            mainWindow = this;

            getReleases();
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

        async public void getReleases()
        {
            try
            {
                Octokit.GitHubClient client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("HS2CharEdit"));

                var releases = await client.Repository.Release.GetAll("CttCJim", "HS2CharEdit");
                var latest = releases[0];
                // MessageBox.Show("The latest release is tagged at " + latest.TagName + " and is named " + latest.Name);
                string v1 = VERSIONNUMBER;
                string v2 = latest.TagName.Replace("v", "");

                var version1 = new Version(v1);
                var version2 = new Version(v2);

                var result = version1.CompareTo(version2);
                if (result > 0)
                {
                    //MessageBox.Show("version1 is greater");
                    MessageBoxResult res = MessageBox.Show("Warning: your version number is " + version1 + " but the latest version is " + version2 + ".\nFor best performance, only download this software from the official Github release at https://github.com/CttCJim/HS2CharEdit/releases \nDo you want to go there now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                    {
                        string url = "https://github.com/CttCJim/HS2CharEdit/releases";
                        Process.Start("explorer", url);
                    }
                }
                else if (result < 0)
                {
                    //MessageBox.Show("version2 is greater");
                    
                    MessageBoxResult res = MessageBox.Show("A new version is available! (" + version2 + ") Go to Releases to down load it now?",
                                  "Update Available",
                                  MessageBoxButton.YesNo,
                                  MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        string url = "https://github.com/CttCJim/HS2CharEdit/releases";
                        Process.Start("explorer", url);
                    }

                }
                else
                {
                    // MessageBox.Show("versions are equal");
                }
            } catch
            {
                MessageBox.Show("Failed to verify version number. To check for updates go to https://github.com/CttCJim/HS2CharEdit/releases or visit my Patreon.");
            }
            
            return;
        }

        public byte[] FloatToHex(float f)
        {
            byte[] hexes = BitConverter.GetBytes(f);
            Array.Reverse(hexes);
            return hexes;
        }

        private void WinLoaded(object sender, RoutedEventArgs e)
        {
            //initialize
            /*
            //test: converting int to 1 byte array
            int i = int.Parse("12");
            byte[] content = BitConverter.GetBytes(i).Take(1).ToArray();
            //display result
            string curstring = BitConverter.ToString(content).ToLower();
            MessageBox.Show(curstring);
            */

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

        public void updateAll()
        {
            //update all stats from databytes in memory
            for (var i = 0; i < allstats.Length; i++)
            {
                allstats[i].LoadData(databytes);
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

        static int Search(byte[] src, byte[] pattern, int occurrence = 0, int starthere = 0)
        {
            int timesfound = 0;
            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = starthere; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1)
                    {
                        if (timesfound == occurrence)
                        {
                            return i;
                        }
                        else
                        {
                            timesfound++;
                            continue;
                        }
                    }
                }
            }
            return -1;
        }

        private void LoadCard(string cardfile)
        {

            if (cardchanged)
            {
                MessageBoxResult result = MessageBox.Show(this, "You have unsaved changes! Load a new card?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
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
            if (databytes.Length == 0)
            {
                MessageBox.Show("The PNG you selected has no card data", "Card Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }



            //load all stats from card data to text boxes
            for (var i = 0; i < allstats.Length; i++)
            {
                allstats[i].LoadData(databytes);
            }

            //get name length stats
            int oldnamelen = txt_charName.Text.Length;
            //getpos will return the char after a keyword
            //getpos(byte[] filebytes, string propName, string[] findfirst)
            /*
        int[] fullnameInts = new int[7]; //used for tracking length and size changes for fullname change
        int[] fullnameIntsPos = new int[7]; //used for tracking length and size changes for fullname change
        //positions displaced by character name - 5xpos, 1xsize
             * */
            searchfor = Encoding.ASCII.GetBytes("pos");
            fullnameIntsPos[0] = Search(databytes, searchfor) + 4;
            fullnameIntsPos[1] = Search(databytes, searchfor, 4) + 4;
            fullnameIntsPos[2] = Search(databytes, searchfor, 5) + 4;
            fullnameIntsPos[3] = Search(databytes, searchfor, 6) + 4;
            fullnameIntsPos[4] = Search(databytes, searchfor, 7) + 4;
            searchfor = Encoding.ASCII.GetBytes("size");
            fullnameIntsPos[5] = Search(databytes, searchfor, 3) + 4; //this one is 1 byte only
            fullnameIntsPos[6] = Search(databytes, searchfor, 7) + 6;
            searchfor = Encoding.ASCII.GetBytes("fullname");
            fullnameIntsPos[7] = Search(databytes, searchfor) + 8; //this one is 1 byte only

            for (int i=0;i<8;i++)
            {
                int j; //number of bytes to read
                if ((i == 5)||(i==7))
                {
                    j = 1;
                }
                else
                {
                    j = 2;
                }
                //get bytes[] at position
                var hexNum = databytes.Skip(fullnameIntsPos[i]).Take(j).ToArray();

                // Hexadecimal Representation of number
                string hexStr = BitConverter.ToString(hexNum).Replace("-", "");

                // Converting to integer
                fullnameInts[i] = Int32.Parse(hexStr, NumberStyles.HexNumber) - oldnamelen;
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
            //trick the selected control into losing focus and saving its data
            txt_focusSnatcher.Focus();
            //allow time for the lost focus event to fire async
            //i know this is a hack, eff off. i'll do something better later.
            System.Threading.Thread.Sleep(50);

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

        private void btn_editName_Click(object sender, RoutedEventArgs e)
        {
            Button thebutton = ((Button)sender);
            if (txt_charName.IsReadOnly)
            {
                thebutton.Content = "Save";
            } else
            {
                thebutton.Content = "Edit";
                textboxchanged(txt_charName.Name, txt_charName.Text);
            }
            txt_charName.IsReadOnly = !txt_charName.IsReadOnly;
        }
    }
}
