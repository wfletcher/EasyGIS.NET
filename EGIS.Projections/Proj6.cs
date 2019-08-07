using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGIS.Projections
{
    internal class Proj6
    {
     
        public abstract class CRS : ICRS
        {
            public string WKT
            {
                get;
                internal set;
            }

            public string Name
            {
                get;
                internal set;
            }

            public string Id
            {
                get;
                internal set;
            }

            public string Authority
            {
                get;
                internal set;
            }

            public bool IsEquivalent(ICRS other)
            {
                if (other == null) return false;

                IntPtr pjThis = Proj6Native.proj_create(IntPtr.Zero, this.WKT);
                IntPtr pjOther = Proj6Native.proj_create(IntPtr.Zero, other.WKT);
                try
                {
                    if (pjThis != IntPtr.Zero && pjOther != IntPtr.Zero)
                    {
                        bool same = Proj6Native.proj_is_equivalent_to(pjThis, pjOther, Proj6Native.PJ_COMPARISON_CRITERION.PJ_COMP_EQUIVALENT_EXCEPT_AXIS_ORDER_GEOGCRS) != 0;
                        if (!same)
                        {
                            //proj_is_equivalent_to doesn't seem to compare different WKT representations
                            //convert both to ESRI WKT and compare
                            string wkt1 = Proj6Native.Proj_as_wkt(IntPtr.Zero, pjThis, Proj6Native.PJ_WKT_TYPE.PJ_WKT1_ESRI);
                            string wkt2 = Proj6Native.Proj_as_wkt(IntPtr.Zero, pjOther, Proj6Native.PJ_WKT_TYPE.PJ_WKT1_ESRI);
                           
                            same = string.Compare(wkt1, wkt2, StringComparison.OrdinalIgnoreCase) == 0;

                            if (!same)
                            {
                                IntPtr pjWkt1 = Proj6Native.proj_create(IntPtr.Zero, wkt1);
                                IntPtr pjWkt2 = Proj6Native.proj_create(IntPtr.Zero, wkt2);
                                try
                                {
                                    if (pjWkt1 != IntPtr.Zero && pjWkt2 != IntPtr.Zero)
                                    {
                                        same = Proj6Native.proj_is_equivalent_to(pjWkt1, pjWkt2, Proj6Native.PJ_COMPARISON_CRITERION.PJ_COMP_EQUIVALENT_EXCEPT_AXIS_ORDER_GEOGCRS) != 0;
                                    }

                                }
                                finally
                                {
                                    if (pjWkt1 != IntPtr.Zero) Proj6Native.proj_destroy(pjWkt1);
                                    if (pjWkt2 != IntPtr.Zero) Proj6Native.proj_destroy(pjWkt2);
                                }

                            }

                        }
                        return same;
                        
                    }
                }
                finally
                {
                    if (pjThis != IntPtr.Zero) Proj6Native.proj_destroy(pjThis);
                    if (pjOther != IntPtr.Zero) Proj6Native.proj_destroy(pjOther);
                }
                return false;

            }

            public CRSBoundingBox AreaOfUse
            {
                get;
                internal set;
            }

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(Authority)) return string.Format("{0} [{1}:{2}]", Name, Authority, Id);
                if( !string.IsNullOrEmpty(Id)) return string.Format("{0} [{1}]", Name, Id);
                return Name;
            }

            public static CRS FromWKT(string wkt)
            {
                IntPtr p = Proj6Native.proj_create(IntPtr.Zero, wkt);
                
                //this code returns null?
                //IntPtr p = Proj6Native.Proj_create_from_wkt(IntPtr.Zero, wkt);

                try
                {
                    Proj6Native.PJ_TYPE pType = Proj6Native.proj_get_type(p);
                    //Console.Out.WriteLine("pType=" + pType);
                    string name = Proj6Native.GetName(p);
                    string authName = Proj6Native.GetAuthName(p);
                    string id = Proj6Native.ProjGetIdCode(p);

                    CRSBoundingBox areaOfUse = new CRSBoundingBox()
                    {
                        WestLongitudeDegrees = -1000,
                        NorthLatitudeDegrees = -1000,
                        EastLongitudeDegrees = -1000,
                        SouthLatitudeDegrees = -1000
                    };
 
                    Proj6Native.proj_get_area_of_use(IntPtr.Zero, p, 
                        ref areaOfUse.WestLongitudeDegrees, 
                        ref areaOfUse.SouthLatitudeDegrees,
                        ref areaOfUse.EastLongitudeDegrees,
                        ref areaOfUse.NorthLatitudeDegrees, 
                        IntPtr.Zero);



                    //string axisName;
                    //string axisAbbrev;
                    //string axisDirection;
                    //double unit_conv_factor=1;
                    //string unit_name;
                    //string unit_auth_name;
                    //string unit_code;

                    //int axisCount = Proj6Native.proj_cs_get_axis_count(IntPtr.Zero, p);

                    //if (axisCount > 0)
                    //{
                    //    if (Proj6Native.Proj_cs_get_axis_info(IntPtr.Zero, p, 0, out axisName, out axisAbbrev, out axisDirection, out unit_conv_factor, out unit_name, out unit_auth_name, out unit_code))
                    //    {
                    //        Console.Out.WriteLine(axisName);
                    //    }
                    //}

                    if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS)
                    {
                        return new GeographicCRS()
                        {
                            Id = id,
                            Name = name,
                            Authority = authName,
                            WKT = wkt,
                            AreaOfUse = areaOfUse
                        };
                    }
                    else if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_PROJECTED_CRS)
                    {
                        
                        return new ProjectedCRS()
                        {
                            Id = id,
                            Name = name,
                            Authority = authName,
                            WKT = wkt,
                            UnitsToMeters = 1,
                            AreaOfUse = areaOfUse
                        };
                        
                    }
                    else if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS)
                    {
                        if (wkt.IndexOf("PROJECTION", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return new ProjectedCRS()
                            {
                                Id = id,
                                Name = name,
                                Authority = authName,
                                WKT = wkt,
                                UnitsToMeters = 1,
                                AreaOfUse = areaOfUse
                            };
                        }
                        else
                        {
                            return new GeographicCRS()
                            {
                                Id = id,
                                Name = name,
                                Authority = authName,
                                WKT = wkt,
                                AreaOfUse = areaOfUse
                            };
                        }                                                
                    }
                    else
                    {
                        //Console.Out.WriteLine("pType = " + pType);
                    }

                    return null;
                }
                finally
                {
                    if(p != IntPtr.Zero) Proj6Native.proj_destroy(p);
                }

            }

            
        }

        public class GeographicCRS : CRS, IGeographicCRS
        {            
        }

        public class ProjectedCRS : CRS, IProjectedCRS
        {
            public double UnitsToMeters
            {
                get;
                internal set;
            }
        }


        public class CoordinateTransformation : ICoordinateTransformation
        {
            IntPtr pjNative = IntPtr.Zero;

            public CoordinateTransformation(ICRS source, ICRS target)
            {
                this.SourceCRS = source;
                this.TargetCRS = target;

                pjNative = Proj6Native.proj_create_crs_to_crs(IntPtr.Zero, source.WKT, target.WKT, IntPtr.Zero);

                if (pjNative == IntPtr.Zero)
                {
                    throw new Exception("Could not create coordinate transformation");
                }
                IntPtr pn = Proj6Native.proj_normalize_for_visualization(IntPtr.Zero, pjNative);
                Proj6Native.proj_destroy(pjNative);
                pjNative = IntPtr.Zero;                
                if (pn == IntPtr.Zero)
                {
                    throw new Exception("Could not create coordinate transformation - proj_normalize_for_visualization returned zero");
                }
                pjNative = pn;
            }

            public ICRS SourceCRS
            {
                get;
                private set;            
            }

            public ICRS TargetCRS
            {
                get;
                private set;            
            }

            public int Transform(double[] points, int pointCount, TransformDirection direction= TransformDirection.Forward)
            {
                switch (direction)
                {
                    case TransformDirection.Forward:
                        return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_FWD, points, pointCount);
                    case TransformDirection.Inverse:
                        return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_INV, points, pointCount);
                    default:
                        return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_IDENT, points, pointCount);
                }
            }

            public unsafe int Transform(double[] points, int startIndex, int pointCount, TransformDirection direction = TransformDirection.Forward)
            {
                fixed (double* ptr = points)
                {
                    switch (direction)
                    {
                        case TransformDirection.Forward:
                            return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_FWD, ptr+startIndex, pointCount);
                        case TransformDirection.Inverse:
                            return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_INV, ptr + startIndex, pointCount);
                        default:
                            return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_IDENT, ptr + startIndex, pointCount);
                    }
                }

            }

            public unsafe int Transform(double* points, int pointCount, TransformDirection direction = TransformDirection.Forward)
            {
                
                switch (direction)
                {
                    case TransformDirection.Forward:
                        return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_FWD, points, pointCount);
                    case TransformDirection.Inverse:
                        return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_INV, points, pointCount);
                    default:
                        return Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_IDENT, points, pointCount);
                }

            }



            public void Dispose()
            {
                if (pjNative != IntPtr.Zero)
                {
                    Proj6Native.proj_destroy(pjNative);
                }
                pjNative = IntPtr.Zero;
            }
        }

    }

    

}
