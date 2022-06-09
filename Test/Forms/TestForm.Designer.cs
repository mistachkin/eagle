/*
 * TestForm.Designer.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

namespace Eagle._Forms
{
    partial class TestForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestForm));
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.prbProgress = new System.Windows.Forms.ProgressBar();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpTest = new System.Windows.Forms.GroupBox();
            this.lstTest = new System.Windows.Forms.ListBox();
            this.ofdTest = new System.Windows.Forms.OpenFileDialog();
            this.grpFileName = new System.Windows.Forms.GroupBox();
            this.btnSelectFileName = new System.Windows.Forms.Button();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.sfdTest = new System.Windows.Forms.SaveFileDialog();
            this.grpStatus.SuspendLayout();
            this.grpTest.SuspendLayout();
            this.grpFileName.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.txtStatus);
            this.grpStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpStatus.Location = new System.Drawing.Point(12, 251);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(970, 265);
            this.grpStatus.TabIndex = 2;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "&Details";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(6, 19);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtStatus.Size = new System.Drawing.Size(958, 240);
            this.txtStatus.TabIndex = 0;
            // 
            // prbProgress
            // 
            this.prbProgress.Location = new System.Drawing.Point(12, 522);
            this.prbProgress.Name = "prbProgress";
            this.prbProgress.Size = new System.Drawing.Size(676, 34);
            this.prbProgress.TabIndex = 3;
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(694, 522);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(92, 34);
            this.btnRun.TabIndex = 4;
            this.btnRun.Text = "&Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(890, 522);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(92, 34);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // grpTest
            // 
            this.grpTest.Controls.Add(this.lstTest);
            this.grpTest.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpTest.Location = new System.Drawing.Point(12, 65);
            this.grpTest.Name = "grpTest";
            this.grpTest.Size = new System.Drawing.Size(970, 180);
            this.grpTest.TabIndex = 1;
            this.grpTest.TabStop = false;
            this.grpTest.Text = "R&esults";
            // 
            // lstTest
            // 
            this.lstTest.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstTest.FormattingEnabled = true;
            this.lstTest.HorizontalScrollbar = true;
            this.lstTest.Location = new System.Drawing.Point(6, 19);
            this.lstTest.Name = "lstTest";
            this.lstTest.ScrollAlwaysVisible = true;
            this.lstTest.Size = new System.Drawing.Size(958, 147);
            this.lstTest.TabIndex = 0;
            // 
            // ofdTest
            // 
            this.ofdTest.DefaultExt = "txt";
            this.ofdTest.FileName = "test.txt";
            this.ofdTest.Filter = "Text files|*.txt|All files|*.*";
            this.ofdTest.RestoreDirectory = true;
            this.ofdTest.ShowHelp = true;
            this.ofdTest.Title = "Select test file...";
            this.ofdTest.FileOk += new System.ComponentModel.CancelEventHandler(this.ofdTest_FileOk);
            // 
            // grpFileName
            // 
            this.grpFileName.Controls.Add(this.btnSelectFileName);
            this.grpFileName.Controls.Add(this.txtFileName);
            this.grpFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpFileName.Location = new System.Drawing.Point(12, 12);
            this.grpFileName.Name = "grpFileName";
            this.grpFileName.Size = new System.Drawing.Size(970, 47);
            this.grpFileName.TabIndex = 0;
            this.grpFileName.TabStop = false;
            this.grpFileName.Text = "&Input File Name";
            // 
            // btnSelectFileName
            // 
            this.btnSelectFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSelectFileName.Location = new System.Drawing.Point(934, 18);
            this.btnSelectFileName.Name = "btnSelectFileName";
            this.btnSelectFileName.Size = new System.Drawing.Size(30, 20);
            this.btnSelectFileName.TabIndex = 1;
            this.btnSelectFileName.Text = "&...";
            this.btnSelectFileName.UseVisualStyleBackColor = true;
            this.btnSelectFileName.Click += new System.EventHandler(this.btnSelectFileName_Click);
            // 
            // txtFileName
            // 
            this.txtFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFileName.Location = new System.Drawing.Point(6, 19);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.ReadOnly = true;
            this.txtFileName.Size = new System.Drawing.Size(922, 20);
            this.txtFileName.TabIndex = 0;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(792, 522);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(92, 34);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "&Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // sfdTest
            // 
            this.sfdTest.DefaultExt = "txt";
            this.sfdTest.Filter = "Text files|*.txt|All files|*.*";
            this.sfdTest.RestoreDirectory = true;
            this.sfdTest.ShowHelp = true;
            this.sfdTest.Title = "Save log file...";
            this.sfdTest.FileOk += new System.ComponentModel.CancelEventHandler(this.sfdTest_FileOk);
            // 
            // TestForm
            // 
            this.AcceptButton = this.btnRun;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(994, 568);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.grpFileName);
            this.Controls.Add(this.grpTest);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.prbProgress);
            this.Controls.Add(this.grpStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "TestForm";
            this.Text = "...";
            this.grpStatus.ResumeLayout(false);
            this.grpStatus.PerformLayout();
            this.grpTest.ResumeLayout(false);
            this.grpFileName.ResumeLayout(false);
            this.grpFileName.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.ProgressBar prbProgress;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox grpTest;
        private System.Windows.Forms.ListBox lstTest;
        private System.Windows.Forms.OpenFileDialog ofdTest;
        private System.Windows.Forms.GroupBox grpFileName;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Button btnSelectFileName;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.SaveFileDialog sfdTest;
    }
}

