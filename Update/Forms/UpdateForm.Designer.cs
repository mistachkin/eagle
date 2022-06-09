/*
 * UpdateForm.Designer.cs --
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
using System.Windows.Forms;
using Eagle._Components.Private;
using Eagle._Controls.Private;
using Eagle._Resources;

namespace Eagle._Forms
{
    internal partial class UpdateForm
    {
        #region Private Data
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        ///////////////////////////////////////////////////////////////////////

        private Button btnCancel;
        private Button btnUpdate;
        private Label lblBanner;
        private Label lblPercent;
        private Label lblUpdate;
        private Label lblUri;
        private TextProgressBar prbUpdate;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">
        /// true if managed resources should be disposed; otherwise, false.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Color lightBlueColor = Color.FromArgb(
                ((int)(((byte)(118)))), ((int)(((byte)(134)))),
                ((int)(((byte)(157)))));

            Color darkBlueColor = Color.FromArgb(
                ((int)(((byte)(72)))), ((int)(((byte)(93)))),
                ((int)(((byte)(124)))));

            Font font = new Font(
                "Segoe UI", 9.00F, FontStyle.Regular, GraphicsUnit.Point,
                ((byte)(0)));

            Font buttonFont = new Font(
                "Segoe UI", 13.00F, FontStyle.Regular, GraphicsUnit.Point,
                ((byte)(0)));

            BorderStyle labelBorderStyle = BorderStyle.None;

            ComponentResourceManager resources =
                new ComponentResourceManager(typeof(UpdateForm));

            ///////////////////////////////////////////////////////////////////

            this.btnCancel = new Button();
            this.btnUpdate = new Button();
            this.lblBanner = new Label();
            this.lblPercent = new Label();
            this.lblUpdate = new Label();
            this.lblUri = new Label();
            this.prbUpdate = new TextProgressBar();

            ///////////////////////////////////////////////////////////////////

            this.SuspendLayout();

            ///////////////////////////////////////////////////////////////////
            //
            // btnCancel
            //
            this.btnCancel.BackColor = lightBlueColor;
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.Font = buttonFont;
            this.btnCancel.Location = new Point(415, 180);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(207, 41);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;

            ///////////////////////////////////////////////////////////////////
            //
            // btnUpdate
            //
            this.btnUpdate.BackColor = lightBlueColor;
            this.btnUpdate.FlatStyle = FlatStyle.Flat;
            this.btnUpdate.Font = buttonFont;
            this.btnUpdate.Location = new Point(12, 180);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new Size(207, 41);
            this.btnUpdate.TabIndex = 5;
            this.btnUpdate.Text = "&Update";
            this.btnUpdate.UseVisualStyleBackColor = false;

            ///////////////////////////////////////////////////////////////////
            //
            // lblBanner
            //
            this.lblBanner.BorderStyle = labelBorderStyle;
            this.lblBanner.Image = Resources.bannerMini;
            this.lblBanner.Location = new Point(12, 12);
            this.lblBanner.Name = "lblBanner";
            this.lblBanner.Size = new Size(180, 60);
            this.lblBanner.TabIndex = 0;
            this.lblBanner.UseMnemonic = false;

            ///////////////////////////////////////////////////////////////////
            //
            // lblPercent
            //
            this.lblPercent.BorderStyle = labelBorderStyle;
            this.lblPercent.BackColor = darkBlueColor;
            this.lblPercent.Font = font;
            this.lblPercent.ForeColor = Color.White;
            this.lblPercent.Location = new Point(200, 62);
            this.lblPercent.Name = "lblPercent";
            this.lblPercent.Size = new Size(422, 22);
            this.lblPercent.TabIndex = 2;
            this.lblPercent.UseMnemonic = false;

            ///////////////////////////////////////////////////////////////////
            //
            // lblUpdate
            //
            this.lblUpdate.BorderStyle = labelBorderStyle;
            this.lblUpdate.BackColor = darkBlueColor;
            this.lblUpdate.Font = font;
            this.lblUpdate.ForeColor = Color.White;
            this.lblUpdate.Location = new Point(200, 12);
            this.lblUpdate.Name = "lblUpdate";
            this.lblUpdate.Size = new Size(422, 42);
            this.lblUpdate.TabIndex = 1;
            this.lblUpdate.UseMnemonic = false;

            ///////////////////////////////////////////////////////////////////
            //
            // lblUri
            //
            this.lblUri.BorderStyle = labelBorderStyle;
            this.lblUri.BackColor = darkBlueColor;
            this.lblUri.Font = font;
            this.lblUri.ForeColor = Color.White;
            this.lblUri.Location = new Point(12, 130);
            this.lblUri.Name = "lblUri";
            this.lblUri.Size = new Size(610, 42);
            this.lblUri.TabIndex = 4;
            this.lblUri.UseMnemonic = false;

            ///////////////////////////////////////////////////////////////////
            //
            // prbUpdate
            //
            this.prbUpdate.ForeColor = lightBlueColor;
            this.prbUpdate.TextColor = Color.White;
            this.prbUpdate.Location = new Point(12, 92);
            this.prbUpdate.Maximum = 1000;
            this.prbUpdate.Name = "prbUpdate";
            this.prbUpdate.Size = new Size(610, 30);
            this.prbUpdate.Style = ProgressBarStyle.Continuous;
            this.prbUpdate.TabIndex = 3;

            ///////////////////////////////////////////////////////////////////
            //
            // UpdateForm
            //
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.lblBanner);
            this.Controls.Add(this.lblPercent);
            this.Controls.Add(this.lblUpdate);
            this.Controls.Add(this.lblUri);
            this.Controls.Add(this.prbUpdate);

            ///////////////////////////////////////////////////////////////////
            //
            // HACK: *MONO* Disable auto-scaling when running on Mono as
            //       it makes the form the wrong size.
            //
            this.AutoScaleMode = VersionOps.IsMono() ?
                AutoScaleMode.None : AutoScaleMode.Font;

            ///////////////////////////////////////////////////////////////////
            //
            // HACK: *MONO* Slightly adjust the height of the form when
            //       running on Mono; otherwise, it gets the size wrong
            //       for reasons that are unclear.
            //
            this.ClientSize = VersionOps.IsMono() ?
                new Size(648, 269) : new Size(634, 233);

            ///////////////////////////////////////////////////////////////////

            this.AcceptButton = this.btnUpdate;
            this.CancelButton = this.btnCancel;
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.BackColor = darkBlueColor;
            this.ControlBox = false;
            this.Font = font;
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Icon = ((Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "UpdateForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = DefaultFormText;

            ///////////////////////////////////////////////////////////////////

            this.ResumeLayout(false);
        }
        #endregion
    }
}
