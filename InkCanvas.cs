using System;
using System.Collections.Generic;
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

namespace WPUnlock
{
    public class ColorSet
    {
        public static SolidColorBrush Black = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 51));
        public static SolidColorBrush White = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

        public static SolidColorBrush Red = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 20, 60));
        public static SolidColorBrush Blue = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 205));
        public static SolidColorBrush Yellow = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 215, 0));

        public static SolidColorBrush Orange = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 126, 34));
        public static SolidColorBrush Green = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 163, 64));
        public static SolidColorBrush Purple = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 86, 51, 147));

    }
    public class InkCanvas : Canvas
    {
        #region Instance Variables

        public double strokeWeight = 5;
        public SolidColorBrush stroke;
        private Dictionary<uint, Path> FingerPaths = new Dictionary<uint, Path>();
        private bool isDrawingEnabled = true;
        private List<Path> redoPaths = new List<Path>();

        #endregion

        #region Properties

        public SolidColorBrush Stroke
        {
            get
            {
                return this.stroke;
            }
            set
            {
                this.stroke = value;
            }
        }

        public double StrokeWeight
        {
            get
            {
                return this.strokeWeight;
            }
            set
            {
                this.strokeWeight = value;
            }
        }

        public bool IsDrawingEnabled
        {
            get
            {
                return this.isDrawingEnabled;
            }
            set
            {
                this.isDrawingEnabled = value;
            }
        }

        #endregion

        #region Constructor

        public InkCanvas()
        {
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;

            this.Background = ColorSet.White;
            this.stroke = ColorSet.Black;
        }

        #endregion

        #region Touch Events

        void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (this.isDrawingEnabled)
                CreatePath(e.Pointer.PointerId, new Point(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y));
        }

        void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (this.isDrawingEnabled)
                UpdatePath(e.Pointer.PointerId, new Point(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y));
        }

        void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (this.isDrawingEnabled)
            {
                UpdatePath(e.Pointer.PointerId, new Point(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y));
                CheckIfEmptyPath(e.Pointer.PointerId);
                this.FingerPaths.Remove(e.Pointer.PointerId);

            }
        }

        #endregion

        #region Drawing Methods

        private void CreatePath(uint id, Point position)
        {
            Path pathForCurrentFinger = new Path();
            this.FingerPaths.Add(id, pathForCurrentFinger);
            this.Children.Add(pathForCurrentFinger);

            pathForCurrentFinger.StrokeThickness = this.strokeWeight;
            pathForCurrentFinger.Stroke = this.stroke;

            PathGeometry geometryForCurrentFinger = new PathGeometry();
            geometryForCurrentFinger.Figures = new PathFigureCollection();

            PathFigure f = new PathFigure();
            f.StartPoint = new Point(position.X, position.Y);
            geometryForCurrentFinger.Figures.Add(f);

            pathForCurrentFinger.Data = geometryForCurrentFinger;
            System.Diagnostics.Debug.WriteLine("DEBUG! - PATH CREATED");
            this.redoPaths.Clear();
        }

        private void UpdatePath(uint id, Point position)
        {
            PathGeometry geometryForCurrentFinger = this.FingerPaths[id].Data as PathGeometry;
            QuadraticBezierSegment segment = new QuadraticBezierSegment();

            if (geometryForCurrentFinger.Figures[0].Segments.Count > 0)
            {
                QuadraticBezierSegment lastLine = geometryForCurrentFinger.Figures[0].Segments.Last() as QuadraticBezierSegment;
                Point lastEndPoint = lastLine.Point2;
                segment.Point1 = lastEndPoint;
            }
            else
            {
                segment.Point1 = geometryForCurrentFinger.Figures[0].StartPoint;
                segment.Point2 = position;
            }

            segment.Point2 = position;
            geometryForCurrentFinger.Figures[0].Segments.Add(segment);
            geometryForCurrentFinger.Figures[0].IsFilled = true;
        }

        private void CheckIfEmptyPath(uint id)
        {
            double length = 0;
            PathGeometry geometryForCurrentFinger = this.FingerPaths[id].Data as PathGeometry;
            if (geometryForCurrentFinger.Figures[0].Segments.Count > 0)
            {
                foreach (QuadraticBezierSegment segment in geometryForCurrentFinger.Figures[0].Segments)
                {
                    length += Utils.Dist(segment.Point1, segment.Point2);
                }
            }
            if (length == 0)
            {
                this.Children.Remove(this.FingerPaths[id]);
            }

        }

        public void ClearStrokes()
        {
            this.Children.Clear();
        }

        public void Undo()
        {
            if (this.Children.Count > 0)
            {
                this.redoPaths.Add(this.Children.Last() as Path);

                System.Diagnostics.Debug.WriteLine("Undid " + this.Children.Last() + " :: " + this.Children.Count);
                this.Children.Remove(this.Children.Last());
            }
        }

        public void Redo()
        {
            if (redoPaths.Count != 0)
            {
                this.Children.Add(this.redoPaths.First());
                this.redoPaths.Remove(this.redoPaths.First());
            }
        }

        #endregion

    }
}