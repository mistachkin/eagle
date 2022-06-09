/*
 * TclForm.Designer.cs --
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

namespace TclSample.Forms
{
    public sealed partial class TclForm
    {
        #region Protected Methods
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
            ComponentResourceManager resources =
                new ComponentResourceManager(typeof(TclForm));

            ///////////////////////////////////////////////////////////////////

            this.grpScript = new GroupBox();
            this.txtScript = new TextBox();
            this.grpResult = new GroupBox();
            this.txtResult = new TextBox();
            this.btnNew = new Button();
            this.btnEvaluate = new Button();
            this.btnCancel = new Button();
            this.grpResult.SuspendLayout();
            this.grpScript.SuspendLayout();
            this.SuspendLayout();

            ///////////////////////////////////////////////////////////////////
            //
            // grpScript
            //
            this.grpScript.Controls.Add(this.txtScript);
            this.grpScript.Font = new Font("Microsoft Sans Serif", 10F,
                FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.grpScript.Location = new Point(12, 12);
            this.grpScript.Name = "grpScript";
            this.grpScript.Size = new Size(970, 233);
            this.grpScript.TabIndex = 0;
            this.grpScript.TabStop = false;
            this.grpScript.Text = "&Script";

            ///////////////////////////////////////////////////////////////////
            //
            // txtScript
            //
            this.txtScript.AcceptsReturn = true;
            this.txtScript.BackColor = SystemColors.Window;
            this.txtScript.Font = new Font("Courier New", 20F,
                FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.txtScript.Location = new Point(6, 19);
            this.txtScript.Multiline = true;
            this.txtScript.Name = "txtScript";
            this.txtScript.ScrollBars = ScrollBars.Both;
            this.txtScript.Size = new Size(958, 208);
            this.txtScript.TabIndex = 1;

            ///////////////////////////////////////////////////////////////////
            //
            // grpResult
            //
            this.grpResult.Controls.Add(this.txtResult);
            this.grpResult.Font = new Font("Microsoft Sans Serif", 10F,
                FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.grpResult.Location = new Point(12, 251);
            this.grpResult.Name = "grpResult";
            this.grpResult.Size = new Size(970, 372);
            this.grpResult.TabIndex = 2;
            this.grpResult.TabStop = false;
            this.grpResult.Text = "&Results";

            ///////////////////////////////////////////////////////////////////
            //
            // txtResult
            //
            this.txtResult.Font = new Font("Courier New", 20F,
                FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.txtResult.Location = new Point(6, 19);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.ScrollBars = ScrollBars.Both;
            this.txtResult.Size = new Size(958, 347);
            this.txtResult.TabIndex = 3;

            ///////////////////////////////////////////////////////////////////
            //
            // btnNew
            //
            this.btnNew.Location = new Point(694, 632);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new Size(92, 34);
            this.btnNew.TabIndex = 4;
            this.btnNew.Text = "&New";
            this.btnNew.UseVisualStyleBackColor = true;

            ///////////////////////////////////////////////////////////////////
            //
            // btnEvaluate
            //
            this.btnEvaluate.Location = new Point(792, 632);
            this.btnEvaluate.Name = "btnEvaluate";
            this.btnEvaluate.Size = new Size(92, 34);
            this.btnEvaluate.TabIndex = 5;
            this.btnEvaluate.Text = "&Evaluate";
            this.btnEvaluate.UseVisualStyleBackColor = true;

            ///////////////////////////////////////////////////////////////////
            //
            // btnCancel
            //
            this.btnCancel.Location = new Point(890, 632);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(92, 34);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;

            ///////////////////////////////////////////////////////////////////
            //
            // TclForm
            //
            this.AcceptButton = this.btnEvaluate;
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(994, 678);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnNew);
            this.Controls.Add(this.grpScript);
            this.Controls.Add(this.btnEvaluate);
            this.Controls.Add(this.grpResult);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Icon = ((Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "TclForm";
            this.Text = "Tcl Form";
            this.grpResult.ResumeLayout(false);
            this.grpResult.PerformLayout();
            this.grpScript.ResumeLayout(false);
            this.grpScript.PerformLayout();
            this.ResumeLayout(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private IContainer components = null;
        private GroupBox grpResult;
        private TextBox txtResult;
        private Button btnEvaluate;
        private GroupBox grpScript;
        private TextBox txtScript;
        private Button btnNew;
        private Button btnCancel;
        #endregion
    }
}
