#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2020 Winston Fletcher.
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
using System.Diagnostics;

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

                lock (Proj6Native._sync)
                {

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
                                string wkt1 = Proj6Native.Proj_as_wkt(IntPtr.Zero, pjThis, PJ_WKT_TYPE.PJ_WKT1_ESRI);
                                string wkt2 = Proj6Native.Proj_as_wkt(IntPtr.Zero, pjOther, PJ_WKT_TYPE.PJ_WKT1_ESRI);

                                if (wkt1 == null || wkt2 == null)
                                {
                                    wkt1 = Proj6Native.Proj_as_wkt(IntPtr.Zero, pjThis, PJ_WKT_TYPE.PJ_WKT2_2018_SIMPLIFIED);
                                    wkt2 = Proj6Native.Proj_as_wkt(IntPtr.Zero, pjOther, PJ_WKT_TYPE.PJ_WKT2_2018_SIMPLIFIED);
                                }

                                same = string.Equals(wkt1, wkt2, StringComparison.OrdinalIgnoreCase);

                                if (!same && !(wkt1==null || wkt2==null))
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
                }
                return false;

            }

            public CRSBoundingBox AreaOfUse
            {
                get;
                internal set;
            }

            public bool IsDeprecated
            {
                get;
                internal set;
            }

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(Authority)) return string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0} [{1}:{2}]{3}", Name, Authority, Id, IsDeprecated?"[DEPRECATED]":"");
                if( !string.IsNullOrEmpty(Id)) return string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0} [{1}]", Name, Id);
                return Name;
            }

            public string GetWKT(PJ_WKT_TYPE wktType, bool indentText)
            {
                lock (Proj6Native._sync)
                {
                    IntPtr pj = Proj6Native.proj_create(IntPtr.Zero, this.WKT);
                    try
                    {
                        if (pj != IntPtr.Zero)
                        {
                            return Proj6Native.Proj_as_wkt(IntPtr.Zero, pj, wktType, indentText);
                        }
                    }
                    finally
                    {
                        if (pj != IntPtr.Zero) Proj6Native.proj_destroy(pj);
                    }
                }

                return "";

            }

            public static int IdentificationConfidenceThreshold = 70;

            public static CRS FromWKT(string wkt, bool identify = false)
            {                
                lock (Proj6Native._sync)
                {
                    
                    //although using proj_context_create will make proj6 thread safe
                    //it is expensive (10x). Faster to use .net lock and just use the default context
                    IntPtr context = IntPtr.Zero;// Proj6Native.proj_context_create();
                    try
                    {

                        IntPtr p = Proj6Native.proj_create(context, wkt);

                        CRSBoundingBox areaOfUse = new CRSBoundingBox()
                        {
                            WestLongitudeDegrees = -1000,
                            NorthLatitudeDegrees = -1000,
                            EastLongitudeDegrees = -1000,
                            SouthLatitudeDegrees = -1000
                        };

                        if (p != IntPtr.Zero)
                        {
                            int res = Proj6Native.proj_get_area_of_use(context, p,
                                ref areaOfUse.WestLongitudeDegrees,
                                ref areaOfUse.SouthLatitudeDegrees,
                                ref areaOfUse.EastLongitudeDegrees,
                                ref areaOfUse.NorthLatitudeDegrees,
                                IntPtr.Zero);
                            Debug.WriteLine(res);
                        }
                        //this code returns null?
                        //IntPtr p = Proj6Native.Proj_create_from_wkt(IntPtr.Zero, wkt);						
                        if (p != IntPtr.Zero && identify)
                        {
                            string name = Proj6Native.GetAuthName(p);
                           // if (string.IsNullOrEmpty(name)) name = "EPSG";
                            int confidence = 0;
                            IntPtr p2 = Proj6Native.Proj_identify(context, p, name, out confidence);
                            System.Diagnostics.Debug.WriteLine("confidence:" + confidence);
                            if (p2 != IntPtr.Zero && confidence >= IdentificationConfidenceThreshold)
                            {
                                Proj6Native.proj_destroy(p);
                                p = p2;
                            }
                            else
                            {
                                if (p2 != IntPtr.Zero)
                                {
                                    Proj6Native.proj_destroy(p2);
                                }
                            }

                        }
                        if (p == IntPtr.Zero)
                        {
                            Console.Error.WriteLine("Could not create crs from " + wkt);
                            return null; 
                        }
                        
                        try
                        {
                            Proj6Native.PJ_TYPE pType = Proj6Native.proj_get_type(p);
                            //Console.Out.WriteLine("pType=" + pType);
                            string name = Proj6Native.GetName(p);
                            string authName = Proj6Native.GetAuthName(p);
                            string id = Proj6Native.ProjGetIdCode(p);

                            int dep = Proj6Native.proj_is_deprecated(p);
                            bool isDeprecated = dep != 0;

                            if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS && string.IsNullOrEmpty(id))
                            {
                                IntPtr srcCrs = Proj6Native.proj_get_source_crs(context, p);
                                if (srcCrs != IntPtr.Zero)
                                {
                                    try
                                    {
                                        authName = Proj6Native.GetAuthName(srcCrs);
                                        id = Proj6Native.ProjGetIdCode(srcCrs);
                                    }
                                    finally
                                    {
                                        Proj6Native.proj_destroy(srcCrs);
                                    }
                                }
                            }

                            CRSBoundingBox identifiedAreaOfUse = new CRSBoundingBox()
                            {
                                WestLongitudeDegrees = -1000,
                                NorthLatitudeDegrees = -1000,
                                EastLongitudeDegrees = -1000,
                                SouthLatitudeDegrees = -1000
                            };

                            int areaOfUseResult = Proj6Native.proj_get_area_of_use(context, p,
                                ref identifiedAreaOfUse.WestLongitudeDegrees,
                                ref identifiedAreaOfUse.SouthLatitudeDegrees,
                                ref identifiedAreaOfUse.EastLongitudeDegrees,
                                ref identifiedAreaOfUse.NorthLatitudeDegrees,
                                IntPtr.Zero);
                            if(areaOfUseResult != 0)
                            {
                                areaOfUse = identifiedAreaOfUse;
                            }


                            if (identify)
                            {
                                string axisName;
                                string axisAbbrev;
                                string axisDirection;
                                double unit_conv_factor = 1;
                                string unit_name;
                                string unit_auth_name;
                                string unit_code;

                                //int axisCount = Proj6Native.proj_cs_get_axis_count(IntPtr.Zero, p);

                                //if (axisCount > 0)
                                //{
                                //    if (Proj6Native.Proj_cs_get_axis_info(IntPtr.Zero, p, 0, out axisName, out axisAbbrev, out axisDirection, out unit_conv_factor, out unit_name, out unit_auth_name, out unit_code))
                                //    {
                                //        Console.Out.WriteLine(axisName);
                                //    }
                                //}


                                IntPtr pCrs = Proj6Native.proj_crs_get_coordinate_system(context, p);
                                if (pCrs != IntPtr.Zero)
                                {
                                    int axisCount = Proj6Native.proj_cs_get_axis_count(context, pCrs);

                                    if (axisCount > 0)
                                    {
                                        if (Proj6Native.Proj_cs_get_axis_info(context, pCrs, 0, out axisName, out axisAbbrev, out axisDirection, out unit_conv_factor, out unit_name, out unit_auth_name, out unit_code))
                                        {
                                            //System.Diagnostics.Debug.WriteLine(axisName);
                                            // System.Diagnostics.Debug.WriteLine("unit_conv_factor:" + unit_conv_factor);
                                        }
                                    }

                                    Proj6Native.proj_destroy(pCrs);
                                }
                            }
                            if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS)
                            {
                                return new GeographicCRS()
                                {
                                    Id = id,
                                    Name = name,
                                    Authority = authName,
                                    WKT = wkt,
                                    AreaOfUse = areaOfUse,
                                    IsDeprecated = isDeprecated
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
                                    AreaOfUse = areaOfUse,
                                    IsDeprecated = isDeprecated
                                };

                            }
                            else if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS || pType == Proj6Native.PJ_TYPE.PJ_TYPE_COMPOUND_CRS)
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
                                        AreaOfUse = areaOfUse,
                                        IsDeprecated = isDeprecated
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
                                        AreaOfUse = areaOfUse,
                                        IsDeprecated = isDeprecated
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
                            if (p != IntPtr.Zero) Proj6Native.proj_destroy(p);
                        }
                        
                    }
                    catch
                    {
                        return null;
                    }
                    finally
                    {
                        if (context != IntPtr.Zero) Proj6Native.proj_context_destroy(context);
                    }
                }                
            }


            public double Distance(double x1, double y1, double x2, double y2)
            {

                lock (Proj6Native._sync)
                {
                    IntPtr pj = Proj6Native.proj_create(IntPtr.Zero, this.WKT);
                    try
                    {
                        if (pj != IntPtr.Zero)
                        {
                            return Proj6Native.Proj_lp_dist(pj, x1, y1, x2, y2);
                        }
                    }
                    finally
                    {
                        if (pj != IntPtr.Zero) Proj6Native.proj_destroy(pj);
                    }
                }
                return -1;
            }

            public Tuple<double,double> DistanceAndBearing(double x1, double y1, double x2, double y2)
            {

                lock (Proj6Native._sync)
                {
                    IntPtr pj = Proj6Native.proj_create(IntPtr.Zero, this.WKT);
                    try
                    {
                        if (pj != IntPtr.Zero)
                        {
                            return Proj6Native.Proj_geod(pj, x1, y1, x2, y2);
                        }
                    }
                    finally
                    {
                        if (pj != IntPtr.Zero) Proj6Native.proj_destroy(pj);
                    }
                }
                return new Tuple<double,double>(-1,0);
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

            internal CoordinateTransformation()
            {
            }

            public CoordinateTransformation(ICRS source, ICRS target)
            {
                this.SourceCRS = source;
                this.TargetCRS = target;

                //shoud we create a threading context here instead of the default IntPtr.Zero?
                lock (Proj6Native._sync)
                {
                    pjNative = Proj6Native.proj_create_crs_to_crs(IntPtr.Zero, source.WKT, target.WKT, IntPtr.Zero);

                    //if (pjNative == IntPtr.Zero)
                    //{
                    //    throw new InvalidOperationException("Could not create coordinate transformation");
                    //}

                    IntPtr pjBest = CreateMostRelevantTransformation();
                    if (pjBest != IntPtr.Zero)
                    {
                        if(pjNative != IntPtr.Zero) Proj6Native.proj_destroy(pjNative);
                        pjNative = pjBest;
                    }

                    if (pjNative == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Could not create coordinate transformation");
                    }

                    IntPtr pn = Proj6Native.proj_normalize_for_visualization(IntPtr.Zero, pjNative);
                    Proj6Native.proj_destroy(pjNative);
                    pjNative = IntPtr.Zero;
                    if (pn == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Could not create coordinate transformation - proj_normalize_for_visualization returned zero");
                    }
                    pjNative = pn;
                }
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
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {                
                if (pjNative != IntPtr.Zero)
                {
                    Proj6Native.proj_destroy(pjNative);
                }
                pjNative = IntPtr.Zero;
            }

            /// <summary>
            /// Clones the CoordinateTransformation. 
            /// </summary>
            /// <returns></returns>
            /// <remarks>
            /// <para>This method returns a new CoordinateTransformation which, like the source CoordinateTransformation, must be Disposed.</para>
            /// </remarks>
            internal CoordinateTransformation Clone()
            {
                CoordinateTransformation copy = new CoordinateTransformation();
                copy.SourceCRS = this.SourceCRS;
                copy.TargetCRS = this.TargetCRS;
                copy.pjNative = Proj6Native.proj_clone(IntPtr.Zero, this.pjNative);
                return copy;
            }


            private IntPtr CreateMostRelevantTransformation()
            {
                //return IntPtr.Zero;
                lock (Proj6Native._sync)
                {
                    IntPtr result = IntPtr.Zero;
                    IntPtr factoryContext = IntPtr.Zero;
                    IntPtr operationsList = IntPtr.Zero;
                    IntPtr pjSource = IntPtr.Zero, pjTarget = IntPtr.Zero;


                    const bool LoadFromCode = true;

                    if (LoadFromCode && !string.IsNullOrEmpty(this.SourceCRS.Id))
                    {
                        pjSource = Proj6Native.proj_create_from_database(IntPtr.Zero, this.SourceCRS.Authority, this.SourceCRS.Id, Proj6Native.PJ_CATEGORY.PJ_CATEGORY_CRS, 0, null);
                    }
                    else
                    {
                        pjSource = Proj6Native.proj_create(IntPtr.Zero, this.SourceCRS.WKT);
                    }
                    //IntPtr pjTarget = Proj6Native.proj_create(IntPtr.Zero, this.TargetCRS.WKT);
                    if (LoadFromCode && !string.IsNullOrEmpty(this.TargetCRS.Id))
                    {
                        pjTarget = Proj6Native.proj_create_from_database(IntPtr.Zero, this.TargetCRS.Authority, this.TargetCRS.Id, Proj6Native.PJ_CATEGORY.PJ_CATEGORY_CRS, 0, null);
                    }
                    else
                    {
                        pjTarget = Proj6Native.proj_create(IntPtr.Zero, this.TargetCRS.WKT);
                    }

                    try
                    {
                        if (pjSource != IntPtr.Zero && pjTarget != IntPtr.Zero)
                        {
                            factoryContext = Proj6Native.proj_create_operation_factory_context(IntPtr.Zero, null);
                            //Proj6Native.proj_operation_factory_context_set_allow_ballpark_transformations(IntPtr.Zero, factoryContext, 0);
                            //Proj6Native.proj_operation_factory_context_set_allow_use_intermediate_crs(IntPtr.Zero, factoryContext, Proj6Native.PROJ_INTERMEDIATE_CRS_USE.PROJ_INTERMEDIATE_CRS_USE_ALWAYS);

                            Proj6Native.proj_operation_factory_context_set_spatial_criterion(IntPtr.Zero, factoryContext, Proj6Native.PROJ_SPATIAL_CRITERION.PROJ_SPATIAL_CRITERION_PARTIAL_INTERSECTION);

                            //Proj6Native.proj_operation_factory_context_set_grid_availability_use(IntPtr.Zero, factoryContext, Proj6Native.PROJ_GRID_AVAILABILITY_USE.PROJ_GRID_AVAILABILITY_IGNORED);
                            //Proj6Native.proj_operation_factory_context_set_grid_availability_use(IntPtr.Zero, factoryContext, Proj6Native.PROJ_GRID_AVAILABILITY_USE.PROJ_GRID_AVAILABILITY_USED_FOR_SORTING);
                            Proj6Native.proj_operation_factory_context_set_grid_availability_use(IntPtr.Zero, factoryContext, Proj6Native.PROJ_GRID_AVAILABILITY_USE.PROJ_GRID_AVAILABILITY_DISCARD_OPERATION_IF_MISSING_GRID);

                            operationsList = Proj6Native.proj_create_operations(IntPtr.Zero, pjSource, pjTarget, factoryContext);

                            int count = Proj6Native.proj_list_get_count(operationsList);
                            if (count > 0)
                            {
                                result = Proj6Native.proj_list_get(IntPtr.Zero, operationsList, 0);
                                var info = Proj6Native.proj_pj_info(result);
                               // Console.Out.WriteLine(info.Description);
                                //Console.Out.WriteLine(info.Definition);
                            }
                        }                        
                    }
                    finally
                    {
                        if(operationsList != IntPtr.Zero) Proj6Native.proj_list_destroy(operationsList);
                        if (factoryContext != IntPtr.Zero) Proj6Native.proj_operation_factory_context_destroy(factoryContext);
                        if (pjSource != IntPtr.Zero) Proj6Native.proj_destroy(pjSource);
                        if (pjTarget != IntPtr.Zero) Proj6Native.proj_destroy(pjTarget);                        
                    }

                    return result;
                }
            }

        }

    }

    

}
