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
using System.Globalization;

namespace EGIS.ShapeFileLib
{
    /// <summary>Converts <see cref="T:EGIS.ShapeFileLib.SizeD"></see> objects from one type to another.</summary>
    public class SizeDConverter : TypeConverter
    {
        /// <summary>Returns a value indicating whether the converter can convert from the type specified to the <see cref="T:EGIS.ShapeFileLib.SizeD"></see> type, using the specified context.</summary>
        /// <returns>true to indicate the conversion can be performed; otherwise, false. </returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> through which additional context can be supplied.</param>
        /// <param name="sourceType">A <see cref="T:System.Type"></see> the represents the type you wish to convert from.</param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
        }

        /// <summary>Returns a value indicating whether the <see cref="T:EGIS.ShapeFileLib.SizeDConverter"></see> can convert a <see cref="T:EGIS.ShapeFileLib.SizeD"></see> to the specified type.</summary>
        /// <returns>true if this converter can perform the conversion otherwise, false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> through which additional context can be supplied.</param>
        /// <param name="destinationType">A <see cref="T:System.Type"></see> that represents the type you want to convert from.</param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return ((destinationType == typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor)) || base.CanConvertTo(context, destinationType));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;
            if (str == null)
            {
                return base.ConvertFrom(context, culture, value);
            }
            string str2 = str.Trim();
            if (str2.Length == 0)
            {
                return null;
            }
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }
            char ch = culture.TextInfo.ListSeparator[0];
            string[] strArray = str2.Split(new char[] { ch });
            double[] numArray = new double[strArray.Length];
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(double));
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = (double)converter.ConvertFromString(context, culture, strArray[i]);
            }
            if (numArray.Length != 2)
            {
                throw new ArgumentException("TextParseFailedFormat");
            }
            return new SizeD(numArray[0], numArray[1]);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }
            if ((destinationType == typeof(string)) && (value is SizeD))
            {
                SizeD ef = (SizeD)value;
                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }
                string separator = culture.TextInfo.ListSeparator + " ";
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(double));
                string[] strArray = new string[2];
                int num = 0;
                strArray[num++] = converter.ConvertToString(context, culture, ef.Width);
                strArray[num++] = converter.ConvertToString(context, culture, ef.Height);
                return string.Join(separator, strArray);
            }
            if ((destinationType == typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor)) && (value is SizeD))
            {
                SizeD ef2 = (SizeD)value;
                System.Reflection.ConstructorInfo constructor = typeof(SizeD).GetConstructor(new Type[] { typeof(double), typeof(double) });
                if (constructor != null)
                {
                    return new System.ComponentModel.Design.Serialization.InstanceDescriptor(constructor, new object[] { ef2.Width, ef2.Height });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Creates an instance of a <see cref="T:EGIS.ShapeFileLib.SizeD"></see> with the specified property values using the specified context.</summary>
        /// <returns>An <see cref="T:System.Object"></see> representing the new <see cref="T:EGIS.ShapeFileLib.SizeD"></see>, or null if the object cannot be created.</returns>
        /// <param name="propertyValues">An <see cref="T:System.Collections.IDictionary"></see> containing property names and values.</param>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> through which additional context can be supplied.</param>
        public override object CreateInstance(ITypeDescriptorContext context, System.Collections.IDictionary propertyValues)
        {
            return new SizeD((double)propertyValues["Width"], (double)propertyValues["Height"]);
        }

        /// <summary>Returns a value indicating whether changing a value on this object requires a call to the <see cref="EGIS.ShapeFileLib.SizeDConverter.CreateInstance"></see> method to create a new value.</summary>
        /// <returns>Always returns true.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context. This may be null.</param>
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>Retrieves a set of properties for the <see cref="T:EGIS.ShapeFileLib.SizeD"></see> type using the specified context and attributes.</summary>
        /// <returns>A <see cref="T:System.ComponentModel.PropertyDescriptorCollection"></see> containing the properties.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> through which additional context can be supplied.</param>
        /// <param name="attributes">An array of <see cref="T:System.Attribute"></see> objects that describe the properties.</param>
        /// <param name="value">The <see cref="T:System.Object"></see> to return properties for.</param>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(SizeD), attributes).Sort(new string[] { "Width", "Height" });
        }

        /// <summary>Returns whether the <see cref="T:EGIS.ShapeFileLib.SizeD"></see> type supports properties.</summary>
        /// <returns>Always returns true.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> through which additional context can be supplied.</param>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }


}
