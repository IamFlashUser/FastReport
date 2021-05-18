using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using FastReport.Utils;

namespace FastReport
{
    /// <summary>
    /// Represents a line object.
    /// </summary>
    /// <remarks>
    /// Use the <b>Border.Width</b>, <b>Border.Style</b> and <b>Border.Color</b> properties to set 
    /// the line width, style and color. Set the <see cref="Diagonal"/> property to <b>true</b>
    /// if you want to show a diagonal line.
    /// </remarks>
    public partial class LineObject : ReportComponentBase
    {
        #region Fields
        private bool diagonal;
        private CapSettings startCap;
        private CapSettings endCap;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating that the line is diagonal.
        /// </summary>
        /// <remarks>
        /// If this property is <b>false</b>, the line can be only horizontal or vertical.
        /// </remarks>
        [DefaultValue(false)]
        [Category("Appearance")]
        public bool Diagonal
        {
            get { return diagonal; }
            set { diagonal = value; }
        }

        /// <summary>
        /// Gets or sets the start cap settings.
        /// </summary>
        [Category("Appearance")]
        public CapSettings StartCap
        {
            get { return startCap; }
            set { startCap = value; }
        }

        /// <summary>
        /// Gets or sets the end cap settings.
        /// </summary>
        [Category("Appearance")]
        public CapSettings EndCap
        {
            get { return endCap; }
            set { endCap = value; }
        }
        #endregion

        #region Public Methods
        /// <inheritdoc/>
        public override void Assign(Base source)
        {
            base.Assign(source);

            LineObject src = source as LineObject;
            Diagonal = src.Diagonal;
            StartCap.Assign(src.StartCap);
            EndCap.Assign(src.EndCap);
        }

        /// <inheritdoc/>
        public override void Draw(FRPaintEventArgs e)
        {
            IGraphics g = e.Graphics;
            // draw marker when inserting a line
            if (Width == 0 && Height == 0)
            {
                g.DrawLine(Pens.Black, AbsLeft * e.ScaleX - 6, AbsTop * e.ScaleY, AbsLeft * e.ScaleX + 6, AbsTop * e.ScaleY);
                g.DrawLine(Pens.Black, AbsLeft * e.ScaleX, AbsTop * e.ScaleY - 6, AbsLeft * e.ScaleX, AbsTop * e.ScaleY + 6);
                return;
            }

            Report report = Report;
            if (report != null && report.SmoothGraphics)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }

            Pen pen = e.Cache.GetPen(Border.Color, Border.Width * e.ScaleX, Border.DashStyle);

            float width = Width;
            float height = Height;
            if (!Diagonal)
            {
                if (Math.Abs(width) > Math.Abs(height))
                    height = 0;
                else
                    width = 0;
            }

            float x1 = AbsLeft * e.ScaleX;
            float y1 = AbsTop * e.ScaleY;
            float x2 = (AbsLeft + width) * e.ScaleX;
            float y2 = (AbsTop + height) * e.ScaleY;

            if (StartCap.Style == CapStyle.None && EndCap.Style == CapStyle.None)
            {
                g.DrawLine(pen, x1, y1, x2, y2);
            }
            else
            {
                // draw line caps manually. It is necessary for correct svg rendering
                float angle = (float)(Math.Atan2(x2 - x1, y2 - y1) / Math.PI * 180);
                float len = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                float scale = Border.Width * e.ScaleX;

                IGraphicsState state = g.Save();
                g.TranslateTransform(x1, y1);
                g.RotateTransform(-angle);
                float y = 0;
                GraphicsPath startCapPath = null;
                GraphicsPath endCapPath = null;
                float inset = 0;
                if (StartCap.Style != CapStyle.None)
                {
                    StartCap.GetCustomCapPath(out startCapPath, out inset);
                    y += inset * scale;
                }
                if (EndCap.Style != CapStyle.None)
                {
                    EndCap.GetCustomCapPath(out endCapPath, out inset);
                    len -= inset * scale;
                }
                g.DrawLine(pen, 0, y, 0, len);
                g.Restore(state);

                pen = e.Cache.GetPen(Border.Color, 1, Border.DashStyle);
                if (StartCap.Style != CapStyle.None)
                {
                    state = g.Save();
                    g.TranslateTransform(x1, y1);
                    g.RotateTransform(180 - angle);
                    g.ScaleTransform(scale, scale);
                    g.DrawPath(pen, startCapPath);
                    g.Restore(state);
                }
                if (EndCap.Style != CapStyle.None)
                {
                    state = g.Save();
                    g.TranslateTransform(x2, y2);
                    g.RotateTransform(-angle);
                    g.ScaleTransform(scale, scale);
                    g.DrawPath(pen, endCapPath);
                    g.Restore(state);
                }
            }

            if (report != null && report.SmoothGraphics && Diagonal)
            {
                g.InterpolationMode = InterpolationMode.Default;
                g.SmoothingMode = SmoothingMode.Default;
            }
        }

        /// <inheritdoc/>
        public override void Serialize(FRWriter writer)
        {
            Border.SimpleBorder = true;
            base.Serialize(writer);
            LineObject c = writer.DiffObject as LineObject;

            if (Diagonal != c.Diagonal)
                writer.WriteBool("Diagonal", Diagonal);
            StartCap.Serialize("StartCap", writer, c.StartCap);
            EndCap.Serialize("EndCap", writer, c.EndCap);
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="LineObject"/> class with default settings.
        /// </summary>
        public LineObject()
        {
            startCap = new CapSettings();
            endCap = new CapSettings();
            FlagSimpleBorder = true;
            FlagUseFill = false;
        }
    }
}
