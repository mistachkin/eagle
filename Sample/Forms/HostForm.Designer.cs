/*
 * HostForm.Designer.cs --
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
    public sealed partial class HostForm
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
            this.components = new Container();

            ///////////////////////////////////////////////////////////////////

            ComponentResourceManager resources =
                new ComponentResourceManager(typeof(HostForm));

            ///////////////////////////////////////////////////////////////////

            this.notHost = new NotifyIcon(this.components);
            this.grpLog = new GroupBox();
            this.txtLog = new TextBox();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();

            ///////////////////////////////////////////////////////////////////
            //
            // notHost
            //
            this.notHost.Icon = ((Icon)(resources.GetObject("$this.Icon")));
            this.notHost.Text = "Host Form";

            ///////////////////////////////////////////////////////////////////
            //
            // grpLog
            //
            this.grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Font = new Font("Microsoft Sans Serif", 9.75F,
                FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.grpLog.Location = new Point(12, 12);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new Size(351, 193);
            this.grpLog.TabIndex = 0;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "&Log";

            ///////////////////////////////////////////////////////////////////
            //
            // txtLog
            //
            this.txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            this.txtLog.Font = new Font("Courier New", 8.25F,
                FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.Location = new Point(9, 24);
            this.txtLog.MaxLength = 0;
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Both;
            this.txtLog.Size = new Size(333, 160);
            this.txtLog.TabIndex = 0;

            ///////////////////////////////////////////////////////////////////
            //
            // HostForm
            //
            this.ClientSize = new Size(375, 217);
            this.Controls.Add(this.grpLog);
            this.KeyPreview = true;
            this.Icon = ((Icon)(resources.GetObject("$this.Icon")));
            this.Name = "HostForm";
            this.Text = "Host Form";
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.ResumeLayout(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private IContainer components = null;
        private NotifyIcon notHost;
        private GroupBox grpLog;
        private TextBox txtLog;
        #endregion
    }
}
