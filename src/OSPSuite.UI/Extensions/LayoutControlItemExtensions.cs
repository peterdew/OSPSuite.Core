﻿using System.Drawing;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraLayout;
using OSPSuite.Presentation.Extensions;

namespace OSPSuite.UI.Extensions
{
   public static class LayoutControlItemExtensions
   {
      public static void InitializeAsHeader(this LayoutControlItem layoutControlItem, UserLookAndFeel lookAndFeel, string text)
      {
         var currentSkin = CommonSkins.GetSkin(lookAndFeel);
         Color color = currentSkin.TranslateColor(SystemColors.ControlText);
         layoutControlItem.AppearanceItemCaption.ForeColor = color;
         layoutControlItem.TextVisible = !string.IsNullOrEmpty(text);
         layoutControlItem.Text = text.FormatForLabel();
      }

      public static void AdjustLargeButtonSize(this LayoutControlItem layoutControlItem, LayoutControl layoutControl)
      {
         layoutControlItem.AdjustSize(layoutControl, UIConstants.Size.LARGE_BUTTON_WIDTH, UIConstants.Size.LARGE_BUTTON_HEIGHT);
      }

      public static void AdjustLongButtonSize(this LayoutControlItem layoutControlItem, LayoutControl layoutControl)
      {
         layoutControlItem.AdjustSize(layoutControl, UIConstants.Size.LARGE_BUTTON_WIDTH, UIConstants.Size.BUTTON_HEIGHT);
      }

      public static void AdjustTallButtonSize(this LayoutControlItem layoutControlItem, LayoutControl layoutControl)
      {
         layoutControlItem.AdjustSize(layoutControl, UIConstants.Size.BUTTON_WIDTH, UIConstants.Size.LARGE_BUTTON_HEIGHT);
      }

      /// <summary>
      /// Sets the layout control item size to custom and define the min and max width
      /// </summary>
      public static void AdjustSize(this LayoutControlItem layoutControlItem, LayoutControl layoutControl, int width, int height)
      {
         var size = new Size(width, height);
         layoutControl.BeginUpdate();
         layoutControlItem.SizeConstraintsType = SizeConstraintsType.Custom;
         layoutControlItem.MaxSize = size;
         layoutControlItem.MinSize = size;
         //In case the control size became smaller that the previous min size, the max size is ignored. So we set it again 
         layoutControlItem.MaxSize = size;
         layoutControlItem.Size = size;
         layoutControl.EndUpdate();
      }

      /// <summary>
      /// Only sets the control item height. Width will be calculated dynamically
      /// </summary>
      public static void AdjustControlHeight(this LayoutControlItem layoutControlItem, LayoutControl layoutControl, int height)
      {
         //using 0 allows the control width to be calculated dynamically
         layoutControlItem.AdjustControlSize(layoutControl, 0, height);
      }

      /// <summary>
      /// Only sets the control item width. Height will be calculated dynamically
      /// </summary>
      public static void AdjustControlWidth(this LayoutControlItem layoutControlItem, LayoutControl layoutControl, int width)
      {
         //using 0 allows the control width to be calculated dynamically
         layoutControlItem.AdjustControlSize(layoutControl, width, 0);
      }

      /// <summary>
      /// Sets the size of the inner control 
      /// </summary>
      public static void AdjustControlSize(this LayoutControlItem layoutControlItem, LayoutControl layoutControl, int width, int height)
      {
         var size = new Size(width, height);
         layoutControl.BeginUpdate();
         layoutControlItem.SizeConstraintsType = SizeConstraintsType.Custom;
         layoutControlItem.ControlMaxSize = size;
         layoutControlItem.ControlMinSize = size;
         //In case the control size became smaller that the previous min size, the max size is ignored. So we set it again 
         layoutControlItem.ControlMaxSize = size;
         layoutControl.EndUpdate();
      }

      public static void AdjustButtonSize(this LayoutControlItem layoutControlItem, LayoutControl layoutControl)
      {
         layoutControlItem.AdjustSize(layoutControl, UIConstants.Size.BUTTON_WIDTH, UIConstants.Size.BUTTON_HEIGHT);
      }

      public static void AdjustButtonSizeWithImageOnly(this LayoutControlItem layoutControlItem, LayoutControl layoutControl)
      {
         layoutControlItem.AdjustSize(layoutControl, UIConstants.Size.BUTTON_HEIGHT, UIConstants.Size.BUTTON_HEIGHT);
      }
   }
}