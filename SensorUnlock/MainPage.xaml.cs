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
using Windows.Devices.Sensors;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace SensorUnlock
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const double Threshold = 0.8;

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

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.accelerometer = Accelerometer.GetDefault();

            this.accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
        }

        private void UpdateOrientation()
        {
            orientationLabel.Text = Orientation.ToString();
        }

        async private void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                (this.xRect.RenderTransform as CompositeTransform).ScaleY = args.Reading.AccelerationX;
                (this.yRect.RenderTransform as CompositeTransform).ScaleY = args.Reading.AccelerationY;
                (this.zRect.RenderTransform as CompositeTransform).ScaleY = args.Reading.AccelerationZ;

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
            });
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
}
