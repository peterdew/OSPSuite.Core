﻿namespace OSPSuite.UI.Views.Commands
{
   partial class LabelView
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
         _screenBinder.Dispose();
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.layoutControl = new OSPSuite.UI.Controls.UxLayoutControl();
         this.tbComments = new DevExpress.XtraEditors.MemoEdit();
         this.tbLabel = new DevExpress.XtraEditors.TextEdit();
         this.layoutControlGroup = new DevExpress.XtraLayout.LayoutControlGroup();
         this.layoutItemLabel = new DevExpress.XtraLayout.LayoutControlItem();
         this.layoutItemComments = new DevExpress.XtraLayout.LayoutControlItem();
         ((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutControl)).BeginInit();
         this.layoutControl.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.tbComments.Properties)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.tbLabel.Properties)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutItemLabel)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutItemComments)).BeginInit();
         this.SuspendLayout();
         // 
         // layoutControl
         // 
         this.layoutControl.AllowCustomization = false;
         this.layoutControl.Controls.Add(this.tbComments);
         this.layoutControl.Controls.Add(this.tbLabel);
         this.layoutControl.Dock = System.Windows.Forms.DockStyle.Fill;
         this.layoutControl.Location = new System.Drawing.Point(0, 0);
         this.layoutControl.Name = "layoutControl";
         this.layoutControl.Root = this.layoutControlGroup;
         this.layoutControl.Size = new System.Drawing.Size(562, 318);
         this.layoutControl.TabIndex = 38;
         this.layoutControl.Text = "uxLayoutControl1";
         // 
         // tbComment
         // 
         this.tbComments.Location = new System.Drawing.Point(12, 52);
         this.tbComments.Name = "tbComments";
         this.tbComments.Size = new System.Drawing.Size(538, 254);
         this.tbComments.StyleController = this.layoutControl;
         this.tbComments.TabIndex = 5;
         // 
         // tbLabel
         // 
         this.tbLabel.Location = new System.Drawing.Point(112, 12);
         this.tbLabel.Name = "tbLabel";
         this.tbLabel.Size = new System.Drawing.Size(438, 20);
         this.tbLabel.StyleController = this.layoutControl;
         this.tbLabel.TabIndex = 4;
         // 
         // layoutControlGroup
         // 
         this.layoutControlGroup.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
         this.layoutControlGroup.GroupBordersVisible = false;
         this.layoutControlGroup.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutItemLabel,
            this.layoutItemComments});
         this.layoutControlGroup.Location = new System.Drawing.Point(0, 0);
         this.layoutControlGroup.Name = "layoutControlGroup";
         this.layoutControlGroup.Size = new System.Drawing.Size(562, 318);
         this.layoutControlGroup.TextVisible = false;
         // 
         // layoutItemLabel
         // 
         this.layoutItemLabel.Control = this.tbLabel;
         this.layoutItemLabel.Location = new System.Drawing.Point(0, 0);
         this.layoutItemLabel.Name = "layoutItemLabel";
         this.layoutItemLabel.Size = new System.Drawing.Size(542, 24);
         this.layoutItemLabel.TextSize = new System.Drawing.Size(97, 13);
         // 
         // layoutItemComment
         // 
         this.layoutItemComments.Control = this.tbComments;
         this.layoutItemComments.Location = new System.Drawing.Point(0, 24);
         this.layoutItemComments.Name = "layoutItemComments";
         this.layoutItemComments.Size = new System.Drawing.Size(542, 274);
         this.layoutItemComments.TextLocation = DevExpress.Utils.Locations.Top;
         this.layoutItemComments.TextSize = new System.Drawing.Size(97, 13);
         // 
         // LabelView
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Caption = "LabelView";
         this.ClientSize = new System.Drawing.Size(562, 364);
         this.Controls.Add(this.layoutControl);
         this.Name = "LabelView";
         this.Text = "LabelView";
         this.Controls.SetChildIndex(this.layoutControl, 0);
         ((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutControl)).EndInit();
         this.layoutControl.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.tbComments.Properties)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.tbLabel.Properties)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutItemLabel)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.layoutItemComments)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private Controls.UxLayoutControl layoutControl;
      private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup;
      private DevExpress.XtraEditors.MemoEdit tbComments;
      private DevExpress.XtraEditors.TextEdit tbLabel;
      private DevExpress.XtraLayout.LayoutControlItem layoutItemLabel;
      private DevExpress.XtraLayout.LayoutControlItem layoutItemComments;
   }
}