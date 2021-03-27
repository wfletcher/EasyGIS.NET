#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.ShapeFileLib class library of Easy GIS .NET.
** 
** Easy GIS .NET is free software: you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License version 3 as
** published by the Free Software Foundation and appearing in the file
** lgpl-license.txt included in the packaging of this file.
**
** Easy GIS .NET is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License and
** GNU Lesser General Public License along with Easy GIS .NET.
** If not, see <http://www.gnu.org/licenses/>.
**
****************************************************************************/

#endregion


using System;
using System.Collections.Generic;
using System.Text;

namespace EGIS.ShapeFileLib
{
    internal sealed class TestCustomRenderSettings : ICustomRenderSettings
    {

        private string fieldName;
        private int fieldIndex = -1;
        private float maxValue;

        private RenderSettings renderSettings;

        private System.Drawing.Image customImage;

        #region ICustomRenderSettings Members

        public TestCustomRenderSettings(RenderSettings renderSettings, string fieldName, float maxValue)
        {
            this.maxValue = maxValue;
            this.renderSettings = renderSettings;
            this.fieldName = fieldName;
            string[] fieldNames = renderSettings.DbfReader.GetFieldNames();
            
            int index = fieldNames.Length-1;
            while (index >= 0)
            {
                if (string.Compare(fieldNames[index].Trim(), fieldName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    break;
                }
                index--;
            }
            fieldIndex = index;
            customImage = CreateCustomImage(renderSettings.GetImageSymbol());
        }

        private System.Drawing.Image CreateCustomImage(System.Drawing.Image img)
        {
            if(img == null) return null;
            System.Drawing.Image customImg = new System.Drawing.Bitmap(img);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(customImg))
            {
                g.DrawRectangle(System.Drawing.Pens.Red, 0, 0, customImg.Width, customImg.Height);
            }
            return customImg;
        }

        public System.Drawing.Color GetRecordFillColor(int recordNumber)
        {
            if (fieldIndex < 0) return renderSettings.FillColor; ;
            float f;
            if (float.TryParse(this.renderSettings.DbfReader.GetField(recordNumber, this.fieldIndex).Trim(), out f))
            {
                f /= maxValue;
                if (f >=1) f = 1;
                if (f < 0) f = 0;
                int c = (int)Math.Round(f * 255);
                return System.Drawing.Color.FromArgb(10, 10, c);

            }
            else
            {
                return renderSettings.FillColor;           
            }
        }

        private int rst = new Random().Next(2);
        public bool RenderShape(int recordNumber)
        {
            return (recordNumber % 2) == 0;
        }
        

        public bool UseCustomTooltips
        {
            get
            {
                return false;
            }
        }

        public string GetRecordToolTip(int recordNumber)
        {
            return null;
        }

        Random r = new Random();
        public System.Drawing.Color GetRecordOutlineColor(int recordNumber)
        {
            
            //if (r.Next(100) < 20)
            if((recordNumber%8)==0)
            {
                return System.Drawing.Color.FromArgb(200, 10, 10);
            }
            
            return renderSettings.OutlineColor;
        }

        public System.Drawing.Color GetRecordFontColor(int recordNumber)
        {
            if (recordNumber % 10 == 0)
            {
                return System.Drawing.Color.FromArgb(255, 100, 100);
            }

            return renderSettings.FontColor;
        }

        #endregion

        #region ICustomRenderSettings Members


        public bool UseCustomImageSymbols
        {
            //get { throw new Exception("The method or operation is not implemented."); }
            get
            {
                return this.renderSettings.GetImageSymbol() != null;
            }
        }

        public System.Drawing.Image GetRecordImageSymbol(int recordNumber)
        {
            //throw new Exception("The method or operation is not implemented.");
            if (recordNumber % 10 == 0)
            {
                return this.customImage;
            }
            return this.renderSettings.GetImageSymbol();
        }

        public int GetDirection(int recordNumber)
        {
            return 0;
        }

        #endregion
    }
}
