// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microcharts
{
    using System.Linq;
    using SkiaSharp;

    /// <summary>
    /// ![chart](../images/Line.png)
    /// 
    /// Line chart.
    /// </summary>
    public class LineChart : PointChart
    {
        #region Constructors

        public LineChart()
        {
            this.PointSize = 10;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the size of the line.
        /// </summary>
        /// <value>The size of the line.</value>
        public float LineSize { get; set; } = 3;

        /// <summary>
        /// Gets or sets the line mode.
        /// </summary>
        /// <value>The line mode.</value>
        public LineMode LineMode { get; set; } = LineMode.Spline;

        /// <summary>
        /// Gets or sets the alpha of the line area.
        /// </summary>
        /// <value>The line area alpha.</value>
        public byte LineAreaAlpha { get; set; } = 32;

        #endregion

        #region Methods

        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            var valueLabelSizes = MeasureValueLabels();
            var footerHeight = CalculateFooterHeight(valueLabelSizes);
            var headerHeight = CalculateHeaderHeight(valueLabelSizes);
            this.YAxeWidth = this.CalculateYAxeWidth(valueLabelSizes);
            var itemSize = CalculateItemSize(width, height, footerHeight, headerHeight);
            var origin = CalculateYOrigin(itemSize.Height, headerHeight);
            var points = this.CalculatePoints(itemSize, origin, headerHeight);

            //this.DrawArea(canvas, points, itemSize, origin);
            this.DrawLine(canvas, points, itemSize);
            this.DrawAxes(canvas, height, width, points, footerHeight, headerHeight);
            this.DrawPoints(canvas, points);
            this.DrawFooter(canvas, points, itemSize, height, footerHeight);
            this.DrawValueLabel(canvas, points, itemSize, height, valueLabelSizes);
        }

        protected void DrawLine(SKCanvas canvas, SKPoint[] points, SKSize itemSize)
        {
            if (points.Length > 1 && this.LineMode != LineMode.None)
            {
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Black,
                    StrokeWidth = this.LineSize,
                    IsAntialias = true,
                })
                {
                    
                        
                        
                        var path = new SKPath();

                        path.MoveTo(points.First());

                        var last = (this.LineMode == LineMode.Spline) ? points.Length - 1 : points.Length;
                        for (int i = 0; i < last; i++)
                        {
                            if (this.LineMode == LineMode.Spline)
                            {
                                var entry = this.Entries.ElementAt(i);
                                var nextEntry = this.Entries.ElementAt(i + 1);
                                var cubicInfo = this.CalculateCubicInfo(points, i, itemSize);
                                path.CubicTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                            }
                            else if (this.LineMode == LineMode.Straight)
                            {
                                path.LineTo(points[i]);
                            }
                        }

                        canvas.DrawPath(path, paint);
                    
                }


            }
        }

        protected void DrawAxes(SKCanvas canvas, int height, int width, SKPoint[] points, float footerHeight, float headerHeight)
        {
            if (showYAxe && points.Length > 0)
            {
                var firstPointX = points.First().X;
                var lowestY = points.Min(p => p.Y);
                var highestY = points.Max(p => p.Y);

                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Gray,
                    StrokeWidth = this.LineSize,
                    IsAntialias = true,
                })
                {
                    var path = new SKPath();
                    var from = new SKPoint(firstPointX, lowestY - MarginY + MarginY/2);
                    path.MoveTo(from);

                    var toYAxe = new SKPoint(firstPointX, highestY + MarginY);
                   
                    path.LineTo(toYAxe);

                    var toXAxe = new SKPoint(width - MarginX, highestY + MarginY);
                    path.LineTo(toXAxe);
                    
                    canvas.DrawPath(path, paint);
                    
                }

                using (var paint = new SKPaint())
                {
                    paint.TextSize = this.LabelTextSize;
                    paint.IsAntialias = true;
                    paint.Color = SKColors.Gray;
                    paint.IsStroke = false;

                    SKRect boundsLow = new SKRect();
                    SKRect boundsHigh = new SKRect();
                    var HighestValue = MaxValue.ToString();
                    var LowestValue = MinValue.ToString();
                    paint.MeasureText(HighestValue, ref boundsHigh);
                    paint.MeasureText(LowestValue, ref boundsLow);

                    canvas.DrawText(HighestValue, firstPointX - MarginX - boundsHigh.Width, lowestY + boundsHigh.Height/2, paint);
                    canvas.DrawText(LowestValue, firstPointX - MarginX - boundsLow.Width, highestY + boundsLow.Height/2, paint);
                }
            }
        }

        protected void DrawArea(SKCanvas canvas, SKPoint[] points, SKSize itemSize, float origin)
        {
            if (this.LineAreaAlpha > 0 && points.Length > 1)
            {
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.White,
                    IsAntialias = true,
                })
                {
                    using (var shader = this.CreateGradient(points, this.LineAreaAlpha))
                    {
                        paint.Shader = shader;

                        var path = new SKPath();

                        path.MoveTo(points.First().X, origin);
                        path.LineTo(points.First());

                        var last = (this.LineMode == LineMode.Spline) ? points.Length - 1 : points.Length;
                        for (int i = 0; i < last; i++)
                        {
                            if (this.LineMode == LineMode.Spline)
                            {
                                var entry = this.Entries.ElementAt(i);
                                var nextEntry = this.Entries.ElementAt(i + 1);
                                var cubicInfo = this.CalculateCubicInfo(points, i, itemSize);
                                path.CubicTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                            }
                            else if (this.LineMode == LineMode.Straight)
                            {
                                path.LineTo(points[i]);
                            }
                        }

                        path.LineTo(points.Last().X, origin);

                        path.Close();

                        canvas.DrawPath(path, paint);
                    }
                }
            }
        }

        private (SKPoint point, SKPoint control, SKPoint nextPoint, SKPoint nextControl) CalculateCubicInfo(SKPoint[] points, int i, SKSize itemSize)
        {
            var point = points[i];
            var nextPoint = points[i + 1];
            var controlOffset = new SKPoint(itemSize.Width * 0.8f, 0);
            var currentControl = point + controlOffset;
            var nextControl = nextPoint - controlOffset;
            return (point, currentControl, nextPoint, nextControl);
        }

        private SKShader CreateGradient(SKPoint[] points, byte alpha = 255)
        {
            var startX = points.First().X;
            var endX = points.Last().X;
            var rangeX = endX - startX;

            return SKShader.CreateLinearGradient(
                new SKPoint(startX, 0),
                new SKPoint(endX, 0),
                this.Entries.Select(x => SKColors.Black).ToArray(),
                null,
                SKShaderTileMode.Clamp);
        }

        #endregion
    }
}