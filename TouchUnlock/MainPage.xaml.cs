using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.UI;
using System.Threading;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace TouchUnlock
{
    // LOTS of color code from http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private float rainbowProgress = 0;
        private DispatcherTimer timer;

        //rgb(128, 204, 230)
        //CIE-L*1, CIE-a*1, CIE-b*1          //Color #1 CIE-L*ab values
        private double secretCIEL1 = 78.162;
        private double secretCIEa1 = -16.816;
        private double secretCIEb1 = -20.223;

        //rgb(204, 51, 0)
        //CIE-L*1, CIE-a*1, CIE-b*1          //Color #1 CIE-L*ab values
        private double secretCIEL2 = 45.914;
        private double secretCIEa2 = 58.061;
        private double secretCIEb2 = 58.172;
        
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.timer = new DispatcherTimer();
            // set the interval to every 100 milliseconds
            this.timer.Interval = TimeSpan.FromMilliseconds(100);
            // subscribe to tick event
            timer.Tick += Timer_Tick;
            // start the timer
            this.timer.Start();

            //this.colorPick1.ColorChanged += colorPick1_ColorChanged;
            this.colorPick1.Color = Windows.UI.Color.FromArgb(255, 0, 0, 255);
            this.colorPick2.Color = Windows.UI.Color.FromArgb(255, 255, 0, 255);
            //this.colorPicker.SelectedColorChanged += ColorPicker_SelectedColorChanged;
            
            this.unlockButton.Click += UnlockButton_Click;
            this.unlockedScreen.Click += UnlockedScreen_Click;
        }

        private void UnlockedScreen_Click(object sender, RoutedEventArgs e)
        {
            Storyboard sb = (Storyboard)this.Resources["HideUnlock"];
            sb.Begin();
        }

        private void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
           // Debug.WriteLine("PRESSED");
            // check the colors
            //From: http://stackoverflow.com/questions/5392061/algorithm-to-check-similarity-of-colors-based-on-rgb-values-or-maybe-hsv   
            //I would recommend using cie94 (deltae-1994), it's said to be a decent representation of the human color perception. i've used it quite a bit in my computer-vision related applications, and i am rather happy with the result.

            //it's however rather computational expensive to perform such a comparison:

            //rgb to xyz for both colors
            //xyz to lab for both colors
            //diff = deltae94(labcolor1, labcolor2)
            Color color = this.colorPick1.Color;

            //Debug.WriteLine("secretCIEL1:" + secretCIEL1 + " secretCIELa1:" + secretCIEa1 + " secretCIEb1:" + secretCIEb1);
            //Debug.WriteLine("r:" + color.R + " g:" + color.G + " b:" + color.B);
            CIEXYZ c1 = RGBtoXYZ(color.R, color.G, color.B);
            //Debug.WriteLine("x:" + c1.X + " y:" + c1.Y + " z:" + c1.Z);
            CIELab lab1 = XYZtoLab(c1);
            //Debug.WriteLine("lab1L:" + lab1.L + " lab1a:" + lab1.A + " lab1b:" + lab1.B);
            
            double delta = delta1994(lab1.L, lab1.A, lab1.B, secretCIEL1, secretCIEa1, secretCIEb1);
            //Debug.WriteLine("delta:" + delta);
            if (delta <= 20)
            {
                // pretty close match, now check the next one
                Color color2 = this.colorPick2.Color;
                CIELab lab2 = XYZtoLab(RGBtoXYZ(color2.R, color2.G, color2.B));
                delta = delta1994(lab2.L, lab2.A, lab2.B, secretCIEL2, secretCIEa2, secretCIEb2);
                if (delta <= 20)
                {
                    // reset the colors
                    this.colorPick1.Color = Windows.UI.Color.FromArgb(255, 0, 0, 255);
                    this.colorPick2.Color = Windows.UI.Color.FromArgb(0, 255, 0, 255);
                    //Debug.WriteLine("UNLOCKED WOOWOWOWOWOW");
                    Storyboard sb = (Storyboard)this.Resources["ShowUnlock"];
                    sb.Begin();
                }
            }
        }

        /// <summary>
        /// Converts RGB to CIELab.
        /// </summary>
        public CIELab RGBtoLab(int red, int green, int blue)
        {
            return XYZtoLab(RGBtoXYZ(red, green, blue));
        }

        /// <summary>
        /// XYZ to L*a*b* transformation function.
        /// </summary>
        private double Fxyz(double t)
        {
            return ((t > 0.008856) ? Math.Pow(t, (1.0 / 3.0)) : (7.787 * t + 16.0 / 116.0));
        }

        /// <summary>
        /// Converts CIEXYZ to CIELab.
        /// </summary>
        public CIELab XYZtoLab(CIEXYZ xyz)
        {
            CIELab lab = CIELab.Empty;

            lab.L = 116.0 * Fxyz(xyz.Y / CIEXYZ.D65.Y) - 16;
            lab.A = 500.0 * (Fxyz(xyz.X / CIEXYZ.D65.X) - Fxyz(xyz.Y / CIEXYZ.D65.Y));
            lab.B = 200.0 * (Fxyz(xyz.Y / CIEXYZ.D65.Y) - Fxyz(xyz.Z / CIEXYZ.D65.Z));

            return lab;
        }

        /// <summary>
        /// Converts RGB to CIE XYZ (CIE 1931 color space)
        /// </summary>
        public CIEXYZ RGBtoXYZ(int red, int green, int blue)
        {
            // normalize red, green, blue values
            double rLinear = (double)red / 255.0;
            double gLinear = (double)green / 255.0;
            double bLinear = (double)blue / 255.0;

            // convert to a sRGB form
            double r = (rLinear > 0.04045) ? Math.Pow((rLinear + 0.055) / (
                1 + 0.055), 2.2) : (rLinear / 12.92);
            double g = (gLinear > 0.04045) ? Math.Pow((gLinear + 0.055) / (
                1 + 0.055), 2.2) : (gLinear / 12.92);
            double b = (bLinear > 0.04045) ? Math.Pow((bLinear + 0.055) / (
                1 + 0.055), 2.2) : (bLinear / 12.92);

            // converts
            return new CIEXYZ(
                (r * 0.4124 + g * 0.3576 + b * 0.1805),
                (r * 0.2126 + g * 0.7152 + b * 0.0722),
                (r * 0.0193 + g * 0.1192 + b * 0.9505)
                );
        }

        private double delta1994(double L1, double a1, double b1, double L2, double a2, double b2) {
            double xC1 = Math.Sqrt(Math.Pow(a1,2) + Math.Pow(b1,2));
            double xC2 = Math.Sqrt(Math.Pow(a2,2) + Math.Pow(b2,2));
            double xDL = L2 - L1;
            double xDC = xC2 - xC1;
            double xDE = Math.Sqrt(((L1 - L2) * (L1 - L2))
                + ((a1 - a2) * (a1 - a2))
                + ((b1 - b2) * (b1 - b2)));
            double xDH = 0;
if (Math.Sqrt(xDE) > (Math.Sqrt(Math.Abs(xDL)) + Math.Sqrt(Math.Abs(xDC))))
            {
                xDH = (double)Math.Sqrt((xDE * xDE) - (xDL * xDL) - (xDC * xDC));
}

            double xSC = 1 + (0.045 * xC1);
            double xSH = 1 + (0.015 * xC1);

                //Weighting factors depending 
                //on the application (1 = default)
            xDL /= 1;
            xDC /= 1 * xSC;
            xDH /= 1 * xSH;
            double E94 = Math.Sqrt(Math.Pow(xDL, 2) + Math.Pow(xDC, 2) + Math.Pow(xDH, 2));

            return E94;
        }
        
        private void Timer_Tick(object sender, object e)
        {
            rainbowProgress += 0.02f;
            Color newColor = Rainbow(rainbowProgress);
            this.unlockButton.Background = new SolidColorBrush(newColor);
        }

        void colorPick1_ColorChanged(object sender, Windows.UI.Color color)
        {
            //Debug.WriteLine(picker.SelectedColor.Color);
            Debug.WriteLine("r:" + color.R);
            Debug.WriteLine("g:" + color.G);
            Debug.WriteLine("b:" + color.B);
            Debug.WriteLine("a:" + color.A);
        }
        
        public Color Rainbow(float progress)
        {
            float div = (Math.Abs(progress % 1) * 6);
            int ascending = (int)((div % 1) * 255);
            int descending = 255 - ascending;

            switch ((int)div)
            {
                case 0:
                    return Color.FromArgb(255, 255, (byte)ascending, 0);
                case 1:
                    return Color.FromArgb(255, (byte)descending, 255, 0);
                case 2:
                    return Color.FromArgb(255, 0, 255, (byte)ascending);
                case 3:
                    return Color.FromArgb(255, 0, (byte)descending, 255);
                case 4:
                    return Color.FromArgb(255, (byte)ascending, 0, 255);
                default: // case 5:
                    return Color.FromArgb(255, 255, 0, (byte)descending);
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

       
    }
    /// <summary>
    /// Structure to define CIE L*a*b*.
    /// </summary>
    public struct CIELab
    {
        /// <summary>
        /// Gets an empty CIELab structure.
        /// </summary>
        public static readonly CIELab Empty = new CIELab();

        private double l;
        private double a;
        private double b;


        public static bool operator ==(CIELab item1, CIELab item2)
        {
            return (
                item1.L == item2.L
                && item1.A == item2.A
                && item1.B == item2.B
                );
        }

        public static bool operator !=(CIELab item1, CIELab item2)
        {
            return (
                item1.L != item2.L
                || item1.A != item2.A
                || item1.B != item2.B
                );
        }


        /// <summary>
        /// Gets or sets L component.
        /// </summary>
        public double L
        {
            get
            {
                return this.l;
            }
            set
            {
                this.l = value;
            }
        }

        /// <summary>
        /// Gets or sets a component.
        /// </summary>
        public double A
        {
            get
            {
                return this.a;
            }
            set
            {
                this.a = value;
            }
        }

        /// <summary>
        /// Gets or sets a component.
        /// </summary>
        public double B
        {
            get
            {
                return this.b;
            }
            set
            {
                this.b = value;
            }
        }

        public CIELab(double l, double a, double b)
        {
            this.l = l;
            this.a = a;
            this.b = b;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return (this == (CIELab)obj);
        }

        public override int GetHashCode()
        {
            return L.GetHashCode() ^ a.GetHashCode() ^ b.GetHashCode();
        }

    }
    /// <summary>
    /// Structure to define CIE XYZ.
    /// </summary>
    public struct CIEXYZ
    {
        /// <summary>
        /// Gets an empty CIEXYZ structure.
        /// </summary>
        public static readonly CIEXYZ Empty = new CIEXYZ();
        /// <summary>
        /// Gets the CIE D65 (white) structure.
        /// </summary>
        public static readonly CIEXYZ D65 = new CIEXYZ(0.9505, 1.0, 1.0890);


        private double x;
        private double y;
        private double z;

        public static bool operator ==(CIEXYZ item1, CIEXYZ item2)
        {
            return (
                item1.X == item2.X
                && item1.Y == item2.Y
                && item1.Z == item2.Z
                );
        }

        public static bool operator !=(CIEXYZ item1, CIEXYZ item2)
        {
            return (
                item1.X != item2.X
                || item1.Y != item2.Y
                || item1.Z != item2.Z
                );
        }

        /// <summary>
        /// Gets or sets X component.
        /// </summary>
        public double X
        {
            get
            {
                return this.x;
            }
            set
            {
                this.x = (value > 0.9505) ? 0.9505 : ((value < 0) ? 0 : value);
            }
        }

        /// <summary>
        /// Gets or sets Y component.
        /// </summary>
        public double Y
        {
            get
            {
                return this.y;
            }
            set
            {
                this.y = (value > 1.0) ? 1.0 : ((value < 0) ? 0 : value);
            }
        }

        /// <summary>
        /// Gets or sets Z component.
        /// </summary>
        public double Z
        {
            get
            {
                return this.z;
            }
            set
            {
                this.z = (value > 1.089) ? 1.089 : ((value < 0) ? 0 : value);
            }
        }

        public CIEXYZ(double x, double y, double z)
        {
            this.x = (x > 0.9505) ? 0.9505 : ((x < 0) ? 0 : x);
            this.y = (y > 1.0) ? 1.0 : ((y < 0) ? 0 : y);
            this.z = (z > 1.089) ? 1.089 : ((z < 0) ? 0 : z);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return (this == (CIEXYZ)obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

    }
}
