using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Sensors;
using Windows.UI;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

// fingerprint image from https://lindadanaher.files.wordpress.com/2012/02/fingerprint.jpg
namespace SensorUnlock
{
    // LOTS of color code from http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const double Threshold = 0.8;

        public List<Color> secretColors = new List<Color>() {
            Color.FromArgb(255, 255, 0 , 0),
            Color.FromArgb(255, 0, 255 , 0),
            Color.FromArgb(255, 0, 0 , 255),
            Color.FromArgb(255, 255, 255, 0)
        };

        private List<Rectangle> rects = new List<Rectangle>();
        private Color currentSecret;
        private int secretRectIndex = 0;
        private int correctInARow = 0;
        private const int MATCH_NUM = 4;
        private bool activateButtonDown = false;

        public enum RoughOrientation
        {
            None,
            Up,
            Down,
            Left,
            Right,
            Forward,
            Back
        }

        private RoughOrientation orientation;
        private RoughOrientation Orientation
        {
            get
            {
                return this.orientation;
            }
            set
            {
                if (this.orientation != value)
                {
                    this.orientation = value;
                    UpdateOrientation();
                }
            }
        }

        private Accelerometer accelerometer;
        private Random rand;

        public MainPage()
        {
            rand = new Random();
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            rects.Add(this.leftRect);
            rects.Add(this.rightRect);
            rects.Add(this.forwardRect);

            this.unlockShot.Click += UnlockShot_Click;

            this.activateButton.PointerPressed += activateButton_PointerPressed;
            this.activateButton.PointerReleased += activateButton_PointerReleased;
            //this.activateButton.AddHandler(PointerPressedEvent, activateButton_PointerPressed, true);
            this.accelerometer = Accelerometer.GetDefault();

            this.accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

            setRandColors();
        }

        private void UnlockShot_Click(object sender, RoutedEventArgs e)
        {
            setRandColors();
            Storyboard sb = (Storyboard)this.Resources["HideUnlock"];
            sb.Begin();
        }

        void activateButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("Released");
            activateButtonDown = false;

            if ((this.leftRect.RenderTransform as CompositeTransform).ScaleY >= (this.rightRect.RenderTransform as CompositeTransform).ScaleY
                        && (this.leftRect.RenderTransform as CompositeTransform).ScaleY >= (this.forwardRect.RenderTransform as CompositeTransform).ScaleY)
            {
                // HACKS OH MY
                if (secretRectIndex == 0)
                {
                    correctInARow++;
                }
                else
                {
                    correctInARow = 0;
                }
            }
            else if ((this.rightRect.RenderTransform as CompositeTransform).ScaleY >= (this.leftRect.RenderTransform as CompositeTransform).ScaleY
                && (this.rightRect.RenderTransform as CompositeTransform).ScaleY >= (this.forwardRect.RenderTransform as CompositeTransform).ScaleY)
            {
                // HACKS OH MY
                if (secretRectIndex == 1)
                {
                    correctInARow++;
                }
                else
                {
                    correctInARow = 0;
                }
            }
            else
            {
                // HACKS OH MY
                if (secretRectIndex == 2)
                {
                    correctInARow++;
                }
                else
                {
                    correctInARow = 0;
                }
            }

            if (correctInARow == MATCH_NUM)
            {
                Debug.WriteLine("Unlock!!!");
                Storyboard sb = (Storyboard)this.Resources["ShowUnlock"];
                sb.Begin();
                correctInARow = 0;
            }
            else
            {
                setRandColors();
            }
            Debug.WriteLine("Correct in a row: " + correctInARow);
        }

        void activateButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            activateButtonDown = true;
            Debug.WriteLine("Pressed");
        }

     
        private void UpdateOrientation()
        {
            //orientationLabel.Text = Orientation.ToString();
        }

        async private void setRandColors()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Color secret = secretColors[rand.Next(secretColors.Count)];
                currentSecret = secret;
                int index = rand.Next(rects.Count);
                for (int i = 0; i < rects.Count; i++)
                {
                    Rectangle rect = rects[index];
                    if (i == 0)
                    {
                        // fill first random rect with the secret color
                        rect.Fill = new SolidColorBrush(secret);
                        secretRectIndex = index;
                    }
                    else
                    {
                        rect.Fill = new SolidColorBrush(randNonSecretColor());
                    }
                    index = (index + 1) % rects.Count;
                }
            });
        }

        private Color randNonSecretColor()
        {
            Color randColor;
            bool badRand = true;
            while (badRand)
            {
                byte randR = (byte)rand.Next(0, 255);
                byte randG = (byte)rand.Next(0, 255);
                byte randB = (byte)rand.Next(0, 255);

                bool tooClose = false;
                for (int i = 0; i < secretColors.Count; i++)
                {
                    Color secret = secretColors[i];
                    CIELab secretLab = XYZtoLab(RGBtoXYZ(secret.R, secret.G, secret.B));
                    CIELab randLab = XYZtoLab(RGBtoXYZ(randR, randG, randB));
                    double delta = delta1994(randLab.L, randLab.A, randLab.B, secretLab.L, secretLab.A, secretLab.B);
                    if (delta <= 25)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                {
                    badRand = false;
                    randColor = Color.FromArgb(255, randR, randG, randB);
                }

            }
            return randColor;
        }

        async private void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {

                if (args.Reading.AccelerationY < -Threshold)
                {
                    this.Orientation = RoughOrientation.Down;
                }
                else if (args.Reading.AccelerationY > Threshold)
                {
                    this.Orientation = RoughOrientation.Up;
                }
                else if (args.Reading.AccelerationX < -Threshold)
                {
                    this.Orientation = RoughOrientation.Left;
                }
                else if (args.Reading.AccelerationX > Threshold)
                {
                    this.Orientation = RoughOrientation.Right;
                }
                else if (args.Reading.AccelerationZ < -Threshold)
                {
                    this.Orientation = RoughOrientation.Forward;
                }
                else if (args.Reading.AccelerationZ > Threshold)
                {
                    this.Orientation = RoughOrientation.Back;
                }
                else
                {
                    this.Orientation = RoughOrientation.None;
                }

                if (args.Reading.AccelerationX <= 0)
                {
                    (this.leftRect.RenderTransform as CompositeTransform).ScaleY = -args.Reading.AccelerationX + 0.1;
                    (this.rightRect.RenderTransform as CompositeTransform).ScaleY = 0.1;
                }
                else
                {
                    (this.leftRect.RenderTransform as CompositeTransform).ScaleY = 0.1;
                    (this.rightRect.RenderTransform as CompositeTransform).ScaleY = args.Reading.AccelerationX + 0.1;
                }

                if (args.Reading.AccelerationZ <= 0)
                {
                    (this.forwardRect.RenderTransform as CompositeTransform).ScaleY = -args.Reading.AccelerationZ -0.1;
                }
                else
                {
                    (this.forwardRect.RenderTransform as CompositeTransform).ScaleY = 0.1;
                }

                if (activateButtonDown)
                {
                    // check magnitudes
                    if ((this.leftRect.RenderTransform as CompositeTransform).ScaleY >= (this.rightRect.RenderTransform as CompositeTransform).ScaleY
                        && (this.leftRect.RenderTransform as CompositeTransform).ScaleY >= (this.forwardRect.RenderTransform as CompositeTransform).ScaleY)
                    {
                        this.background.Fill = this.leftRect.Fill;
                    }
                    else if ((this.rightRect.RenderTransform as CompositeTransform).ScaleY >= (this.leftRect.RenderTransform as CompositeTransform).ScaleY
                        && (this.rightRect.RenderTransform as CompositeTransform).ScaleY >= (this.forwardRect.RenderTransform as CompositeTransform).ScaleY)
                    
                    {
                        this.background.Fill = this.rightRect.Fill;
                    }
                    else
                    {
                        this.background.Fill = this.forwardRect.Fill;
                    }
                }
                else
                {
                    this.background.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                }
                //(this.rightRect.RenderTransform as CompositeTransform).ScaleY = args.Reading.AccelerationX;
                //(this.forwardRect.RenderTransform as CompositeTransform).ScaleY = -args.Reading.AccelerationZ;
            });
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
