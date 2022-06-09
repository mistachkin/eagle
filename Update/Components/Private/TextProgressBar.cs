/*
 * TextProgressBar.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Eagle._Controls.Private
{
    [Guid("ad5bffd9-b0a6-4a21-8313-a9ed8bd80cd0")]
    internal sealed class TextProgressBar : ProgressBar
    {
        #region Private Constants
        private const int WM_PAINT = 0x000F;
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region System.Windows.Forms.Control Overrides
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                Refresh();
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        protected override void OnPaintBackground(
            PaintEventArgs pevent
            )
        {
            //
            // NOTE: To help prevent flicker, do nothing.
            //
        }

        ///////////////////////////////////////////////////////////////////////////

        protected override void WndProc(
            ref Message message
            )
        {
            base.WndProc(ref message);

            if (message.Msg == WM_PAINT)
            {
                //
                // NOTE: If we are not supposed to show text when the value is
                //       zero (and it is currently zero), just return now.
                //
                if (!showTextForZero && (this.Value == 0))
                    return;

                //
                // NOTE: Create a graphics context to handle this message.
                //
                using (Graphics graphics = CreateGraphics())
                {
                    //
                    // NOTE: Grab our parent control.
                    //
                    Control parent = this.Parent;

                    //
                    // NOTE: If our parent control is valid use the same font
                    //       it is using; otherwise, just use the system status
                    //       font.
                    //
                    Font font = (parent != null) ? parent.Font :
                        SystemFonts.StatusFont;

                    //
                    // NOTE: Create a boldface copy of the selected font.
                    //
                    using (font = new Font(font, FontStyle.Bold))
                    {
                        //
                        // NOTE: Create a solid brush using the configured text
                        //       color.
                        //
                        using (Brush brush = new SolidBrush(this.TextColor))
                        {
                            //
                            // NOTE: What is our size?
                            //
                            Size size = new Size(this.Width, this.Height);

                            //
                            // NOTE: What is the current text to display?
                            //
                            string text = Text;

                            //
                            // NOTE: Measure the size of the text to display.
                            //
                            SizeF textSize = graphics.MeasureString(text, font);

                            //
                            // NOTE: Draw the text within our graphics context.
                            //
                            graphics.DrawString(text, font, brush,
                                (size.Width - textSize.Width) / 2,
                                (size.Height - textSize.Height) / 2);
                        }
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Public Properties
        private Color textColor;
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        [Category("Appearance")]
        [Description("The text color of this component, " +
            "which is used to display the progress bar text.")]
        public Color TextColor
        {
            get { return textColor; }
            set { textColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////

        private bool showTextForZero;
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        [Category("Appearance")]
        [Description("When true, the text will be displayed " +
            "when the value of this control is zero.")]
        public bool ShowTextForZero
        {
            get { return showTextForZero; }
            set { showTextForZero = value; }
        }
        #endregion
    }
}
