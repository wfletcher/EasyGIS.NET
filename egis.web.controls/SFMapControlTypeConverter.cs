using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace EGIS.Web.Controls
{
    class SFMapControlTypeConverter : System.ComponentModel.TypeConverter
    {

        public override bool CanConvertFrom(ITypeDescriptorContext context,Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }


        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                Page page = HttpContext.Current.Handler as Page;
                if (page != null)
                {
                    return page.FindControl((string)value) as SFMap;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return ((SFMap)value).ID;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}
