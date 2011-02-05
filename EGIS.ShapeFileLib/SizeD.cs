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
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace EGIS.ShapeFileLib
{
    /// <summary>Stores an ordered pair of double-precision floating-point numbers, typically the width and height of a rectangle.</summary>
    /// <remarks>
    /// This structure is essentially the same as the System.Drawing.SizeF structure but uses double-precision floating point numbers, which are
    /// needed for accuracy in GIS applications
    /// </remarks>
    /// <filterpriority>1</filterpriority>
    [Serializable, StructLayout(LayoutKind.Sequential), TypeConverter(typeof(SizeDConverter)), ComVisible(true)]
    public struct SizeD
    {
        /// <summary>Initializes a new instance of the <see cref="T:EGIS.ShapeFileLib.SizeD"></see> class.</summary>
        /// <filterpriority>1</filterpriority>
        public static readonly SizeD Empty;
        private double width;
        private double height;
        /// <summary>Initializes a new instance of the <see cref="T:EGIS.ShapeFileLib.SizeD"></see> class from the specified existing <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</summary>
        /// <param name="size">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> from which to create the new <see cref="T:EGIS.ShapeFileLib.SizeD"></see>. </param>
        public SizeD(SizeD size)
        {
            this.width = size.width;
            this.height = size.height;
        }

        /// <summary>Initializes a new instance of the <see cref="T:EGIS.ShapeFileLib.SizeD"></see> class from the specified <see cref="T:EGIS.ShapeFileLib.PointD"></see>.</summary>
        /// <param name="pt">The <see cref="T:EGIS.ShapeFileLib.PointD"></see> from which to initialize this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>. </param>
        public SizeD(PointD pt)
        {
            this.width = pt.X;
            this.height = pt.Y;
        }

        /// <summary>Initializes a new instance of the <see cref="T:EGIS.ShapeFileLib.SizeD"></see> class from the specified dimensions.</summary>
        /// <param name="width">The width component of the new <see cref="T:EGIS.ShapeFileLib.SizeD"></see>. </param>
        /// <param name="height">The height component of the new <see cref="T:EGIS.ShapeFileLib.SizeD"></see>. </param>
        public SizeD(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        /// <summary>Adds the width and height of one <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure to the width and height of another <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure.</summary>
        /// <returns>A <see cref="T:EGIS.ShapeFileLib.Size"></see> structure that is the result of the addition operation.</returns>
        /// <param name="sz2">The second <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to add. </param>
        /// <param name="sz1">The first <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to add. </param>
        /// <filterpriority>3</filterpriority>
        public static SizeD operator +(SizeD sz1, SizeD sz2)
        {
            return Add(sz1, sz2);
        }

        /// <summary>Subtracts the width and height of one <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure from the width and height of another <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure.</summary>
        /// <returns>A <see cref="T:EGIS.ShapeFileLib.SizeD"></see> that is the result of the subtraction operation.</returns>
        /// <param name="sz2">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> on the right side of the subtraction operator. </param>
        /// <param name="sz1">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> on the left side of the subtraction operator. </param>
        /// <filterpriority>3</filterpriority>
        public static SizeD operator -(SizeD sz1, SizeD sz2)
        {
            return Subtract(sz1, sz2);
        }

        /// <summary>Tests whether two <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structures are equal.</summary>
        /// <returns>This operator returns true if sz1 and sz2 have equal width and height; otherwise, false.</returns>
        /// <param name="sz2">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure on the right of the equality operator. </param>
        /// <param name="sz1">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure on the left side of the equality operator. </param>
        /// <filterpriority>3</filterpriority>
        public static bool operator ==(SizeD sz1, SizeD sz2)
        {
            return ((sz1.Width == sz2.Width) && (sz1.Height == sz2.Height));
        }

        /// <summary>Tests whether two <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structures are different.</summary>
        /// <returns>This operator returns true if sz1 and sz2 differ either in width or height; false if sz1 and sz2 are equal.</returns>
        /// <param name="sz2">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure on the right of the inequality operator. </param>
        /// <param name="sz1">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure on the left of the inequality operator. </param>
        /// <filterpriority>3</filterpriority>
        public static bool operator !=(SizeD sz1, SizeD sz2)
        {
            return !(sz1 == sz2);
        }

        /// <summary>Converts the specified <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to a <see cref="T:EGIS.ShapeFileLib.PointD"></see>.</summary>
        /// <returns>The <see cref="T:EGIS.ShapeFileLib.PointD"></see> structure to which this operator converts.</returns>
        /// <param name="size">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure to be converted</param>
        /// <filterpriority>3</filterpriority>
        public static explicit operator PointD(SizeD size)
        {
            return new PointD(size.Width, size.Height);
        }

        /// <summary>Gets a value indicating whether this <see cref="T:EGIS.ShapeFileLib.SizeD"></see> has zero width and height.</summary>
        /// <returns>This property returns true when this <see cref="T:EGIS.ShapeFileLib.SizeD"></see> has both a width and height of zero; otherwise, false.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public bool IsEmpty
        {
            get
            {
                return ((this.width == 0f) && (this.height == 0f));
            }
        }
        /// <summary>Gets or sets the horizontal component of this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</summary>
        /// <returns>The horizontal component of this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</returns>
        /// <filterpriority>1</filterpriority>
        public double Width
        {
            get
            {
                return this.width;
            }
            set
            {
                this.width = value;
            }
        }
        /// <summary>Gets or sets the vertical component of this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</summary>
        /// <returns>The vertical component of this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</returns>
        /// <filterpriority>1</filterpriority>
        public double Height
        {
            get
            {
                return this.height;
            }
            set
            {
                this.height = value;
            }
        }
        /// <summary>Adds the width and height of one <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure to the width and height of another <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure.</summary>
        /// <returns>A <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure that is the result of the addition operation.</returns>
        /// <param name="sz2">The second <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to add.</param>
        /// <param name="sz1">The first <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to add.</param>
        public static SizeD Add(SizeD sz1, SizeD sz2)
        {
            return new SizeD(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        /// <summary>Subtracts the width and height of one <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure from the width and height of another <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure.</summary>
        /// <returns>The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> that is a result of the subtraction operation.</returns>
        /// <param name="sz2">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure on the right side of the subtraction operator. </param>
        /// <param name="sz1">The <see cref="T:EGIS.ShapeFileLib.SizeD"></see> structure on the left side of the subtraction operator. </param>
        public static SizeD Subtract(SizeD sz1, SizeD sz2)
        {
            return new SizeD(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        /// <summary>Tests to see whether the specified object is a <see cref="T:EGIS.ShapeFileLib.SizeD"></see> with the same dimensions as this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</summary>
        /// <returns>This method returns true if obj is a <see cref="T:EGIS.ShapeFileLib.SizeD"></see> and has the same width and height as this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>; otherwise, false.</returns>
        /// <param name="obj">The <see cref="T:System.Object"></see> to test. </param>
        /// <filterpriority>1</filterpriority>
        public override bool Equals(object obj)
        {
            if (!(obj is SizeD))
            {
                return false;
            }
            SizeD ef = (SizeD)obj;
            return (((ef.Width == this.Width) && (ef.Height == this.Height)) && ef.GetType().Equals(base.GetType()));
        }

        /// <summary>Returns a hash code for this <see cref="T:EGIS.ShapeFileLib.Size"></see> structure.</summary>
        /// <returns>An integer value that specifies a hash value for this <see cref="T:EGIS.ShapeFileLib.Size"></see> structure.</returns>
        /// <filterpriority>1</filterpriority>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>Converts a <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to a <see cref="T:EGIS.ShapeFileLib.PointD"></see>.</summary>
        /// <returns>Returns a <see cref="T:EGIS.ShapeFileLib.PointD"></see> structure.</returns>
        /// <filterpriority>1</filterpriority>
        public PointD ToPointD()
        {
            return (PointD)this;
        }

        /// <summary>Converts a <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to a <see cref="T:EGIS.ShapeFileLib.Size"></see>.</summary>
        /// <returns>Returns a <see cref="T:System.Drawing.Size"></see> structure.</returns>
        /// <filterpriority>1</filterpriority>
        public System.Drawing.Size ToSize()
        {
            return System.Drawing.Size.Truncate(new System.Drawing.SizeF((float)this.width, (float)this.height));
        }

        /// <summary>Creates a human-readable string that represents this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</summary>
        /// <returns>A string that represents this <see cref="T:EGIS.ShapeFileLib.SizeD"></see>.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode" /></PermissionSet>
        public override string ToString()
        {
            return ("{Width=" + this.width.ToString(System.Globalization.CultureInfo.CurrentCulture) + ", Height=" + this.height.ToString(System.Globalization.CultureInfo.CurrentCulture) + "}");
        }

        static SizeD()
        {
            Empty = new SizeD();
        }
    }
}
