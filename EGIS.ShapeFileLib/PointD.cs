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
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;


namespace EGIS.ShapeFileLib
{
    /// <summary>Represents an ordered pair of double-precision floating-point x- and y-coordinates that defines a point in a two-dimensional plane.</summary>
    /// <remarks>
    /// This structure is essentially the same as the System.Drawing.PointD structure but uses double-precision floating point numbers, which are
    /// needed for accuracy in GIS applications
    /// </remarks>
    [Serializable, StructLayout(LayoutKind.Sequential), ComVisible(true)]
    public struct PointD
    {
        /// <summary>Represents a new instance of the <see cref="EGIS.ShapeFileLib.PointD"></see> class with member data left uninitialized.</summary>
        /// <filterpriority>1</filterpriority>
        public static readonly PointD Empty;
        private double x;
        private double y;
        /// <summary>Initializes a new instance of the <see cref="EGIS.ShapeFileLib.PointD"></see> class with the specified coordinates.</summary>
        /// <param name="y">The vertical position of the point. </param>
        /// <param name="x">The horizontal position of the point. </param>
        public PointD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>Gets a value indicating whether this <see cref="EGIS.ShapeFileLib.PointD"></see> is empty.</summary>
        /// <returns>true if both <see cref="EGIS.ShapeFileLib.PointD.X"></see> and <see cref="EGIS.ShapeFileLib.PointD.Y"></see> are 0; otherwise, false.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public bool IsEmpty
        {
            get
            {
                return ((this.x == 0) && (this.y == 0));
            }
        }
        /// <summary>Gets or sets the x-coordinate of this <see cref="EGIS.ShapeFileLib.PointD"></see>.</summary>
        /// <returns>The x-coordinate of this <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <filterpriority>1</filterpriority>
        public double X
        {
            get
            {
                return this.x;
            }
            set
            {
                this.x = value;
            }
        }
        /// <summary>Gets or sets the y-coordinate of this <see cref="EGIS.ShapeFileLib.PointD"></see>.</summary>
        /// <returns>The y-coordinate of this <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <filterpriority>1</filterpriority>
        public double Y
        {
            get
            {
                return this.y;
            }
            set
            {
                this.y = value;
            }
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by a given <see cref="System.Drawing.Size"></see>.</summary>
        /// <returns>Returns the translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">A <see cref="System.Drawing.Size"></see> that specifies the pair of numbers to add to the coordinates of pt. </param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate. </param>
        /// <filterpriority>3</filterpriority>
        public static PointD operator +(PointD pt, Size sz)
        {
            return Add(pt, sz);
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by the negative of a given <see cref="System.Drawing.Size"></see>.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">A <see cref="EGIS.ShapeFileLib.PointD"></see> to compare. </param>
        /// <param name="pt">A <see cref="EGIS.ShapeFileLib.PointD"></see> to compare. </param>
        /// <filterpriority>3</filterpriority>
        public static PointD operator -(PointD pt, Size sz)
        {
            return Subtract(pt, sz);
        }

        /// <summary>Translates the <see cref="EGIS.ShapeFileLib.PointD"></see> by the specified <see cref="System.Drawing.SizeF"></see>.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="System.Drawing.SizeF"></see> that specifies the numbers to add to the x- and y-coordinates of the <see cref="EGIS.ShapeFileLib.PointD"></see>.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD operator +(PointD pt, SizeF sz)
        {
            return Add(pt, sz);
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by the negative of a specified <see cref="System.Drawing.SizeF"></see>. </summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="System.Drawing.SizeF"></see> that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD operator -(PointD pt, SizeF sz)
        {
            return Subtract(pt, sz);
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by a given <see cref="System.Drawing.Size"></see>.</summary>
        /// <returns>Returns the translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">A <see cref="System.Drawing.Size"></see> that specifies the pair of numbers to add to the coordinates of pt. </param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate. </param>
        /// <filterpriority>3</filterpriority>
        public static PointD operator +(PointD pt, SizeD sz)
        {
            return Add(pt, sz);
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by the negative of a given <see cref="EGIS.ShapeFileLib.SizeD"></see>.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="EGIS.ShapeFileLib.SizeD"></see> that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD operator -(PointD pt, SizeD sz)
        {
            return Subtract(pt, sz);
        }

        /// <summary>Compares two <see cref="EGIS.ShapeFileLib.PointD"></see> structures. The result specifies whether the values of the <see cref="EGIS.ShapeFileLib.PointD.X"></see> and <see cref="EGIS.ShapeFileLib.PointD.Y"></see> properties of the two <see cref="EGIS.ShapeFileLib.PointD"></see> structures are equal.</summary>
        /// <returns>true if the <see cref="EGIS.ShapeFileLib.PointD.X"></see> and <see cref="EGIS.ShapeFileLib.PointD.Y"></see> values of the left and right <see cref="EGIS.ShapeFileLib.PointD"></see> structures are equal; otherwise, false.</returns>
        /// <param name="right">A <see cref="EGIS.ShapeFileLib.PointD"></see> to compare. </param>
        /// <param name="left">A <see cref="EGIS.ShapeFileLib.PointD"></see> to compare. </param>
        /// <filterpriority>3</filterpriority>
        public static bool operator ==(PointD left, PointD right)
        {
            return ((left.X == right.X) && (left.Y == right.Y));
        }

        /// <summary>Determines whether the coordinates of the specified points are not equal.</summary>
        /// <returns>true to indicate the <see cref="EGIS.ShapeFileLib.PointD.X"></see> and <see cref="EGIS.ShapeFileLib.PointD.Y"></see> values of left and right are not equal; otherwise, false. </returns>
        /// <param name="right">A <see cref="EGIS.ShapeFileLib.PointD"></see> to compare.</param>
        /// <param name="left">A <see cref="EGIS.ShapeFileLib.PointD"></see> to compare.</param>
        /// <filterpriority>3</filterpriority>
        public static bool operator !=(PointD left, PointD right)
        {
            return !(left == right);
        }

        /// <summary>Translates a given <see cref="EGIS.ShapeFileLib.PointD"></see> by the specified <see cref="System.Drawing.Size"></see>.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="System.Drawing.Size"></see> that specifies the numbers to add to the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD Add(PointD pt, Size sz)
        {
            return new PointD(pt.X + sz.Width, pt.Y + sz.Height);
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by the negative of a specified size.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="System.Drawing.Size"></see> that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD Subtract(PointD pt, Size sz)
        {
            return new PointD(pt.X - sz.Width, pt.Y - sz.Height);
        }

        /// <summary>Translates a given <see cref="EGIS.ShapeFileLib.PointD"></see> by a specified <see cref="System.Drawing.SizeF"></see>.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="System.Drawing.SizeF"></see> that specifies the numbers to add to the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD Add(PointD pt, SizeF sz)
        {
            return new PointD(pt.X + sz.Width, pt.Y + sz.Height);
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by the negative of a specified size.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="System.Drawing.SizeF"></see> that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD Subtract(PointD pt, SizeF sz)
        {
            return new PointD(pt.X - sz.Width, pt.Y - sz.Height);
        }

        /// <summary>Translates a given <see cref="EGIS.ShapeFileLib.PointD"></see> by a specified <see cref="EGIS.ShapeFileLib.SizeD"></see>.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="EGIS.ShapeFileLib.SizeD"></see> that specifies the numbers to add to the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD Add(PointD pt, SizeD sz)
        {
            return new PointD(pt.X + sz.Width, pt.Y + sz.Height);
        }

        /// <summary>Translates a <see cref="EGIS.ShapeFileLib.PointD"></see> by the negative of a specified size.</summary>
        /// <returns>The translated <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <param name="sz">The <see cref="EGIS.ShapeFileLib.SizeD"></see> that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <param name="pt">The <see cref="EGIS.ShapeFileLib.PointD"></see> to translate.</param>
        public static PointD Subtract(PointD pt, SizeD sz)
        {
            return new PointD(pt.X - sz.Width, pt.Y - sz.Height);
        }



        /// <summary>Specifies whether this <see cref="EGIS.ShapeFileLib.PointD"></see> contains the same coordinates as the specified <see cref="System.Object"></see>.</summary>
        /// <returns>This method returns true if obj is a <see cref="EGIS.ShapeFileLib.PointD"></see> and has the same coordinates as this <see cref="System.Drawing.Point"></see>.</returns>
        /// <param name="obj">The <see cref="System.Object"></see> to test. </param>
        /// <filterpriority>1</filterpriority>
        public override bool Equals(object obj)
        {
            if (!(obj is PointD))
            {
                return false;
            }
            PointD tp = (PointD)obj;
            return (((tp.X == this.X) && (tp.Y == this.Y)) && tp.GetType().Equals(base.GetType()));
        }

        /// <summary>Returns a hash code for this <see cref="EGIS.ShapeFileLib.PointD"></see> structure.</summary>
        /// <returns>An integer value that specifies a hash value for this <see cref="EGIS.ShapeFileLib.PointD"></see> structure.</returns>
        /// <filterpriority>1</filterpriority>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>Converts this <see cref="EGIS.ShapeFileLib.PointD"></see> to a human readable string.</summary>
        /// <returns>A string that represents this <see cref="EGIS.ShapeFileLib.PointD"></see>.</returns>
        /// <filterpriority>1</filterpriority>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{X={0}, Y={1}}}", new object[] { this.x, this.y });
        }

        static PointD()
        {
            Empty = new PointD();
        }

        /// <summary>
        /// converts a PointF structure to a PointD structure
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static implicit operator PointD(System.Drawing.PointF p)
        {
            return new PointD((double)p.X, (double)p.Y);            
        }



    }


}
