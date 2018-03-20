using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace GoByTrainController.Views.Controls
{
    public sealed class SpeedMeter : Control
    {
        #region Constants

        // Template Parts.
        private const string ContainerPartName = "PART_Container";
        private const string ScalePartName = "PART_Scale";
        private const string TrailPartName = "PART_Trail";

        // For convenience.
        private const double Degrees2Radians = Math.PI / 180;

        // Candidate dependency properties.
        // Feel free to modify...
        private const double MinAngle = -150.0;
        private const double MaxAngle = 150.0;
        private const float ScalePadding = 23.0f;

        #endregion Constants

        #region Dependency Property Registrations

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(SpeedMeter), new PropertyMetadata(0.0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(SpeedMeter), new PropertyMetadata(100.0));

        public static readonly DependencyProperty ScaleWidthProperty =
            DependencyProperty.Register("ScaleWidth", typeof(Double), typeof(SpeedMeter), new PropertyMetadata(26.0));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(SpeedMeter), new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(SpeedMeter), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ScaleBrushProperty =
            DependencyProperty.Register("ScaleBrush", typeof(Brush), typeof(SpeedMeter), new PropertyMetadata(new SolidColorBrush(Colors.DarkGray)));

        public static readonly DependencyProperty TrailBrushProperty =
            DependencyProperty.Register("TrailBrush", typeof(Brush), typeof(SpeedMeter), new PropertyMetadata(new SolidColorBrush(Colors.Orange)));

        public static readonly DependencyProperty UnitBrushProperty =
            DependencyProperty.Register("UnitBrush", typeof(Brush), typeof(SpeedMeter), new PropertyMetadata(new SolidColorBrush(Colors.White)));

        public static readonly DependencyProperty ValueAngleProperty =
            DependencyProperty.Register("ValueAngle", typeof(double), typeof(SpeedMeter), new PropertyMetadata(null));

        public static readonly DependencyProperty SpeedLevelProperty =
            DependencyProperty.Register(nameof(SpeedLevel), typeof(int), typeof(SpeedMeter), new PropertyMetadata(0, OnSpeedLevelChanged));

        #endregion Dependency Property Registrations

        #region Constructors

        #endregion Constructors

        public SpeedMeter()
        {
            DefaultStyleKey = typeof(SpeedMeter);

            Minimum = 0;
            Maximum = 5;
        }

        #region Properties

        /// <summary>
        /// Gets or sets the minimum on the scale.
        /// </summary>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum on the scale.
        /// </summary>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the scale.
        /// </summary>
        public double ScaleWidth
        {
            get => (double)GetValue(ScaleWidthProperty);
            set => SetValue(ScaleWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the unit measure.
        /// </summary>
        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        /// <summary>
        /// Gets or sets the trail brush.
        /// </summary>
        public Brush TrailBrush
        {
            get => (Brush)GetValue(TrailBrushProperty);
            set => SetValue(TrailBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the scale brush.
        /// </summary>
        public Brush ScaleBrush
        {
            get => (Brush)GetValue(ScaleBrushProperty);
            set => SetValue(ScaleBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the unit brush.
        /// </summary>
        public Brush UnitBrush
        {
            get => (Brush)GetValue(UnitBrushProperty);
            set => SetValue(UnitBrushProperty, value);
        }

        public double ValueAngle
        {
            get => (double)GetValue(ValueAngleProperty);
            set => SetValue(ValueAngleProperty, value);
        }

        public int SpeedLevel
        {
            get => (int)GetValue(SpeedLevelProperty);
            set => SetValue(SpeedLevelProperty, value);
        }

        #endregion Properties

        protected override void OnApplyTemplate()
        {
            // Scale.
            var scale = GetTemplateChild(ScalePartName) as Path;
            if (scale != null)
            {
                var pg = new PathGeometry();
                var pf = new PathFigure {IsClosed = false};
                var middleOfScale = 100 - ScalePadding - ScaleWidth / 2;
                pf.StartPoint = ScalePoint(MinAngle, middleOfScale);
                var seg = new ArcSegment
                {
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = true,
                    Size = new Size(middleOfScale, middleOfScale),
                    Point = ScalePoint(MaxAngle, middleOfScale)
                };
                pf.Segments.Add(seg);
                pg.Figures.Add(pf);
                scale.Data = pg;
            }

            OnValueChanged(this);
            base.OnApplyTemplate();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnValueChanged(d);
        }

        private static void OnSpeedLevelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = obj as SpeedMeter;

            if (ctrl == null) return;

            ctrl.Value = ctrl.SpeedLevel;
        }

        /// <summary>
        /// Updates the needle rotation, the trail, and the value text according to the new value.
        /// </summary>
        private static void OnValueChanged(DependencyObject d)
        {
            var c = d as SpeedMeter;
            if (c != null && !Double.IsNaN(c.Value))
            {
                var middleOfScale = 100 - ScalePadding - c.ScaleWidth / 2;
                c.ValueAngle = c.ValueToAngle(c.Value);

                // Trail
                var trail = c.GetTemplateChild(TrailPartName) as Path;
                if (trail == null) return;
                if (c.ValueAngle > MinAngle)
                {
                    trail.Visibility = Visibility.Visible;
                    var pg = new PathGeometry();
                    var pf = new PathFigure
                    {
                        IsClosed = false,
                        StartPoint = c.ScalePoint(MinAngle, middleOfScale)
                    };
                    var seg = new ArcSegment
                    {
                        SweepDirection = SweepDirection.Clockwise,
                        IsLargeArc = c.ValueAngle > (180 + MinAngle),
                        Size = new Size(middleOfScale, middleOfScale),
                        Point = c.ScalePoint(Math.Min(c.ValueAngle, MaxAngle), middleOfScale)
                    };
                    // We start from -150, so +30 becomes a large arc.
                    // On overflow, stop trail at MaxAngle.
                    pf.Segments.Add(seg);
                    pg.Figures.Add(pf);
                    trail.Data = pg;
                }
                else
                {
                    trail.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Transforms a set of polar coordinates into a Windows Point.
        /// </summary>
        private Point ScalePoint(double angle, double middleOfScale)
        {
            return new Point(100 + Math.Sin(Degrees2Radians * angle) * middleOfScale, 100 - Math.Cos(Degrees2Radians * angle) * middleOfScale);
        }

        /// <summary>
        /// Returns the angle for a specific value.
        /// </summary>
        /// <returns>In degrees.</returns>
        private double ValueToAngle(double value)
        {
            // Off-scale on the left.
            if (value < Minimum)
            {
                return -157.5D;
            }

            // Off-scale on the right.
            if (value > this.Maximum)
            {
                return MaxAngle + 7.5;
            }

            return (value - Minimum) / (Maximum - Minimum) * (MaxAngle - MinAngle) + MinAngle;
        }
    }
}
