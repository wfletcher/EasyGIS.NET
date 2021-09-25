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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EGIS.Projections
{
    internal class Proj6Native
    {

        internal static object _sync = new object();

        //const string ProjDllName = "proj_5_2.dll";
        const string ProjDllName = "proj_6_1.dll";


        #region dynamically load native x86/x64 dll

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return System.IO.Path.GetDirectoryName(path);
            }
        }

        static Proj6Native()
        {
            //System.Configuration.ConfigurationManager.AppSettings["Proj6Dir"];
            // register path to native dll                  
            //var startupPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var startupPath = AssemblyDirectory;

            if (!System.IO.Directory.Exists(System.IO.Path.Combine(startupPath, "Proj6")))
            {
                Console.Out.WriteLine("could not find {0}", System.IO.Path.Combine(startupPath, "Proj6"));
                startupPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Console.Out.WriteLine("startupPath is now: {0}", startupPath);
            }

            //are we running in web site?
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(startupPath, "Proj6")))
            {
                Console.Out.WriteLine("could not find {0}", System.IO.Path.Combine(startupPath, "Proj6"));
                startupPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,"bin");
                Console.Out.WriteLine("startupPath is now: {0}", startupPath);
            }

            //if (!System.IO.Directory.Exists(System.IO.Path.Combine(startupPath, "Proj6")))
            //{
            //    Console.Out.WriteLine("Could not find {0}", System.IO.Path.Combine(startupPath, "Proj6"));
            //    startupPath = AssemblyDirectory;
            //    Console.Out.WriteLine("startupPath is now: {0}", startupPath);
            //}

            Console.Out.WriteLine("proj6 startupPath:" + startupPath);

            var dllPath = string.Format(@"proj6/{0}/{1}", (Environment.Is64BitProcess ? "x64" : "x86"), "sqlite3.dll");
            LoadLibrary(System.IO.Path.Combine(startupPath, dllPath));

            dllPath = string.Format(@"proj6/{0}/{1}", (Environment.Is64BitProcess ? "x64" : "x86"), ProjDllName);
            LoadLibrary(System.IO.Path.Combine(startupPath, dllPath));

            string projDbPath = System.IO.Path.Combine(startupPath, "Proj6", "proj.db");
            proj_context_set_database_path(IntPtr.Zero, projDbPath, null, null);


        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpPathName);

        #endregion

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_context_create();

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void proj_context_destroy(IntPtr context);


        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_destroy(IntPtr PJ);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_create_crs_to_crs(IntPtr ctx, string source_crs, string target_crs, IntPtr area);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_create(IntPtr ctx, string definition);

        ///*
        // * If for the needs of your software, you want a uniform axis order 
        // * (and thus do not care about axis order mandated by the authority defining the CRS), 
        // * the proj_normalize_for_visualization() function can be used to modify the PJ* object returned by proj_create_crs_to_crs()
        // * so that it accepts as input and returns as output coordinates using the traditional GIS order, that is longitude, latitude (followed by elevation, time) for geographic CRS
        // * and easting, northing for most projected CRS.

        //    P_for_GIS = proj_normalize_for_visualization(C, P);
        //    if( 0 == P_for_GIS )  {
        //        fprintf(stderr, "Oops\n");
        //        return 1;
        //    }
        //    proj_destroy(P);
        //    P = P_for_GIS;
        // */

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_normalize_for_visualization(IntPtr ctx, IntPtr PJ);


        public enum PJ_DIRECTION
        {
            PJ_FWD = 1,   /* Forward    */
            PJ_IDENT = 0,   /* Do nothing */
            PJ_INV = -1    /* Inverse    */
        };


        struct PJ_COORD
        {            
            public double X;
            public double Y;
            double extra1;
            double extra2;


            public PJ_COORD(double x, double y)
            {
                X = x;
                Y = y;
                extra1 = 0;
                extra2 = 1;
            }

        }

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        
        static extern PJ_COORD proj_trans(IntPtr p, PJ_DIRECTION direction, PJ_COORD coord);


        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int proj_trans_generic(IntPtr P,
    PJ_DIRECTION direction,
    double* x, int sx, int nx,
    double* y, int sy, int ny,
    double* z, int sz, int nz,
    double* t, int st, int nt
);
        
        public static unsafe int proj_trans_generic(IntPtr p, PJ_DIRECTION direction, double[] points, int pointCount)
        {
            fixed (double* ptr = points)
            {
                return proj_trans_generic(p, direction, ptr, 16, pointCount,
                    ptr + 1, 16, pointCount,
                    null, 0, 0,
                    null, 0, 0);
            }
        }

        public static unsafe int proj_trans_generic(IntPtr p, PJ_DIRECTION direction, double* points, int pointCount)
        {           
            return proj_trans_generic(p, direction, points, 16, pointCount,
                points + 1, 16, pointCount,
                null, 0, 0,
                null, 0, 0);                    
        }

        public enum PJ_TYPE
        {
            PJ_TYPE_UNKNOWN,

            PJ_TYPE_ELLIPSOID,

            PJ_TYPE_PRIME_MERIDIAN,

            PJ_TYPE_GEODETIC_REFERENCE_FRAME,
            PJ_TYPE_DYNAMIC_GEODETIC_REFERENCE_FRAME,
            PJ_TYPE_VERTICAL_REFERENCE_FRAME,
            PJ_TYPE_DYNAMIC_VERTICAL_REFERENCE_FRAME,
            PJ_TYPE_DATUM_ENSEMBLE,

            /** Abstract type, not returned by proj_get_type() */
            PJ_TYPE_CRS,

            PJ_TYPE_GEODETIC_CRS,
            PJ_TYPE_GEOCENTRIC_CRS,

            /** proj_get_type() will never return that type, but
             * PJ_TYPE_GEOGRAPHIC_2D_CRS or PJ_TYPE_GEOGRAPHIC_3D_CRS. */
            PJ_TYPE_GEOGRAPHIC_CRS,

            PJ_TYPE_GEOGRAPHIC_2D_CRS,
            PJ_TYPE_GEOGRAPHIC_3D_CRS,
            PJ_TYPE_VERTICAL_CRS,
            PJ_TYPE_PROJECTED_CRS,
            PJ_TYPE_COMPOUND_CRS,
            PJ_TYPE_TEMPORAL_CRS,
            PJ_TYPE_ENGINEERING_CRS,
            PJ_TYPE_BOUND_CRS,
            PJ_TYPE_OTHER_CRS,

            PJ_TYPE_CONVERSION,
            PJ_TYPE_TRANSFORMATION,
            PJ_TYPE_CONCATENATED_OPERATION,
            PJ_TYPE_OTHER_COORDINATE_OPERATION,
        } ;


        public enum PJ_CATEGORY
        {
            PJ_CATEGORY_ELLIPSOID,
            PJ_CATEGORY_PRIME_MERIDIAN,
            PJ_CATEGORY_DATUM,
            PJ_CATEGORY_CRS,
            PJ_CATEGORY_COORDINATE_OPERATION
        };

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern PJ_TYPE proj_get_type(IntPtr PJ);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern byte* proj_get_name(IntPtr PJobj);

        public static unsafe string GetName(IntPtr PJobj)
        {
            byte* name = proj_get_name(PJobj);
            if (name == null) return null;
            return new string((sbyte*)name);
        }

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe byte* proj_get_id_auth_name(IntPtr PJobj, int index);

        public static unsafe string GetAuthName(IntPtr PJobj)
        {
            byte* name = proj_get_id_auth_name(PJobj, 0);
            if (name == null) return null;
            return new string((sbyte*)name);
        }

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe byte* proj_get_id_code(IntPtr PJobj, int index);

        public static unsafe string ProjGetIdCode(IntPtr PJobj)
        {
            byte* code = proj_get_id_code(PJobj, 0);
            if (code == null) return null;
            return new string((sbyte*)code);
        }

        public enum PJ_COMPARISON_CRITERION
        {
            /** All properties are identical. */
            PJ_COMP_STRICT,

            /** The objects are equivalent for the purpose of coordinate
            * operations. They can differ by the name of their objects,
            * identifiers, other metadata.
            * Parameters may be expressed in different units, provided that the
            * value is (with some tolerance) the same once expressed in a
            * common unit.
            */
            PJ_COMP_EQUIVALENT,

            /** Same as EQUIVALENT, relaxed with an exception that the axis order
            * of the base CRS of a DerivedCRS/ProjectedCRS or the axis order of
            * a GeographicCRS is ignored. Only to be used
            * with DerivedCRS/ProjectedCRS/GeographicCRS */
            PJ_COMP_EQUIVALENT_EXCEPT_AXIS_ORDER_GEOGCRS
        } ;

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int proj_is_equivalent_to(IntPtr PJobj, IntPtr PJother,
                                               PJ_COMPARISON_CRITERION criterion);



        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]        
        public static extern int proj_cs_get_axis_count(IntPtr ctx, IntPtr cs);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern int proj_cs_get_axis_info(IntPtr ctx,
                                       IntPtr cs, 
                                       int index,
                                       ref byte* out_name,
                                       ref byte* out_abbrev,
                                       ref byte* out_direction,
                                       ref double out_unit_conv_factor,
                                       ref byte* out_unit_name,
                                       ref byte* out_unit_auth_name,
                                       ref byte* out_unit_code);

        public static unsafe bool Proj_cs_get_axis_info(IntPtr ctx,
                                       IntPtr cs,
                                       int index,
                                       out string name,
                                       out string abbrev,
                                       out string direction,
                                       out double unit_conv_factor,
                                       out string unit_name,
                                       out string unit_auth_name,
                                       out string unit_code)
        {

            byte* out_name = null;
            byte* out_abbrev = null;
            byte* out_direction = null;
            double out_unit_conv_factor=-1;
            byte* out_unit_name=null;
            byte* out_unit_auth_name=null;
            byte* out_unit_code=null;

            int result = proj_cs_get_axis_info(ctx, cs, index, ref out_name, ref out_abbrev, ref out_direction,
                ref out_unit_conv_factor, ref out_unit_name, ref out_unit_auth_name, ref out_unit_code);

            if (result != 0)
            {
                name = new string((sbyte*)out_name);
                abbrev = new string((sbyte*)out_abbrev);
                direction = new string((sbyte*)out_direction);
                unit_conv_factor = out_unit_conv_factor;
                unit_name = new string((sbyte*)out_unit_name);
                unit_auth_name = new string((sbyte*)out_unit_auth_name);
                unit_code = new string((sbyte*)out_unit_code);
            }
            else
            {
                name = "";
                abbrev = "";
                direction = "";
                unit_conv_factor = 1;
                unit_name = "";
                unit_auth_name = "";
                unit_code = "";
            }
            return result != 0;
        }


        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        static extern double proj_lp_dist(IntPtr PJ, PJ_COORD a, PJ_COORD b);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        static extern double proj_lpz_dist(IntPtr P, PJ_COORD a, PJ_COORD b);

        public static double Proj_lp_dist(IntPtr PJ, double x0, double y0, double x1, double y1)
        {
            PJ_COORD p1 = new PJ_COORD(x0, y0);
            PJ_COORD p2 = new PJ_COORD(x1, y1);
            double d = proj_lpz_dist(PJ, p1, p2);
            return d;
        }

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        static extern double proj_xy_dist(PJ_COORD a, PJ_COORD b);

        public static double Proj_xy_dist(double x0, double y0, double x1, double y1)
        {
            PJ_COORD p1 = new PJ_COORD(x0, y0);
            PJ_COORD p2 = new PJ_COORD(x1, y1);
            double d = proj_xy_dist(p1, p2);
            return d;

        }

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int proj_context_set_database_path(IntPtr ctx, string dbPath, string auxDbPaths, string options);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_crs_get_geodetic_crs(IntPtr ctx, IntPtr crs);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_get_ellipsoid(IntPtr ctx, IntPtr pj);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_crs_get_coordinate_system(IntPtr ctx, IntPtr crs);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_crs_get_datum(IntPtr ctx, IntPtr crs);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr proj_create_from_database(IntPtr ctx, string auth_name, string code, PJ_CATEGORY category, int usePROJAlternativeGridNames, string options);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern byte** proj_get_authorities_from_database(IntPtr ctx);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern void proj_string_list_destroy(byte** list);

        public static unsafe List<string> Proj_get_authorities_from_database(IntPtr ctx)
        {
            List<string> result = new List<string>();

            byte** list = proj_get_authorities_from_database(ctx);
            try
            {
                byte** ptr = list;
                while (*ptr != null)
                {
                    
                    string s = new string((sbyte*)*ptr);
                    result.Add(s);
                    ptr++;
                }
            }
            finally
            {
                if (list != null)
                {
                    proj_string_list_destroy(list);
                }
            }

            return result;
        }

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern byte** proj_get_codes_from_database(IntPtr ctx, string auth_name, PJ_TYPE type, int allow_deprecated);

        public static unsafe List<string> Proj_get_codes_from_database(IntPtr ctx, string auth_name, PJ_TYPE type, int allow_deprecated)
        {
            List<string> result = new List<string>();

            byte** list = proj_get_codes_from_database(ctx, auth_name, type, allow_deprecated);
            if (list != null)
            {
                try
                {
                    byte** ptr = list;
                    while (*ptr != null)
                    {

                        string s = new string((sbyte*)*ptr);
                        result.Add(s);
                        ptr++;
                    }
                }
                finally
                {                   
                   proj_string_list_destroy(list);                   
                }
            }

            return result;
        }

        public enum PJ_WKT_TYPE
        {
            /** cf osgeo::proj::io::WKTFormatter::Convention::WKT2 */
            PJ_WKT2_2015,
            /** cf osgeo::proj::io::WKTFormatter::Convention::WKT2_SIMPLIFIED */
            PJ_WKT2_2015_SIMPLIFIED,
            /** cf osgeo::proj::io::WKTFormatter::Convention::WKT2_2018 */
            PJ_WKT2_2018,
            /** cf osgeo::proj::io::WKTFormatter::Convention::WKT2_2018_SIMPLIFIED */
            PJ_WKT2_2018_SIMPLIFIED,
            /** cf osgeo::proj::io::WKTFormatter::Convention::WKT1_GDAL */
            PJ_WKT1_GDAL,
            /** cf osgeo::proj::io::WKTFormatter::Convention::WKT1_ESRI */
            PJ_WKT1_ESRI
        }


        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern byte* proj_as_wkt(IntPtr ctx, IntPtr pj, PJ_WKT_TYPE type, byte** options);

        public static unsafe string Proj_as_wkt(IntPtr ctx, IntPtr pj, PJ_WKT_TYPE type, bool indentText = true)
        {
            if (indentText)
            {
                byte* wkt = proj_as_wkt(ctx, pj, type, null);
                return wkt != null ? new string((sbyte*)wkt) : null;
            }
            else
            {
                byte[] optionBytes = System.Text.Encoding.ASCII.GetBytes("MULTILINE=NO\0");
                fixed (byte* ptr = optionBytes)
                {
                    byte** options = stackalloc byte*[1];
                    options[0] = ptr;
                    byte* wkt = proj_as_wkt(ctx, pj, type, options);
                    return wkt != null ? new string((sbyte*)wkt) : null;
                    
                }
            }
        }

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]

        internal static extern int proj_get_area_of_use(IntPtr ctx, IntPtr pjObj, ref double out_west_lon_degree, ref double out_south_lat_degree, ref double out_east_lon_degree, ref double out_north_lat_degree, IntPtr out_area_name);


        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]

        internal static unsafe extern IntPtr proj_create_from_wkt(IntPtr ctx, string wkt, byte** options, byte** out_warnings, byte** out_grammar_errors);

        internal static unsafe IntPtr Proj_create_from_wkt(IntPtr ctx, string wkt)
        {
            return proj_create_from_wkt(ctx, wkt, null, null, null);
        }


        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]

        internal static extern int proj_list_get_count(IntPtr result);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr proj_list_get(IntPtr ctx, IntPtr result, int index);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void proj_list_destroy(IntPtr result);

        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern IntPtr proj_identify(IntPtr ctx, IntPtr pjObj, string auth_name, byte** options, int** out_confidence);


        internal static unsafe IntPtr Proj_identify(IntPtr ctx, IntPtr pjObj, string auth_name, out int confidence)
        {
            confidence = 0;
            int* confidenceArray = null;
            IntPtr pjObjList = proj_identify(ctx, pjObj, auth_name, null, &confidenceArray);

            if (pjObjList == IntPtr.Zero) return IntPtr.Zero;

            int count = proj_list_get_count(pjObjList);
            if (count == 0) return IntPtr.Zero;

            IntPtr result = proj_list_get(ctx, pjObjList, 0);
            confidence = confidenceArray[0];

            proj_int_list_destroy(confidenceArray);
            proj_list_destroy(pjObjList);
            return result;
        }


        [DllImport(ProjDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern void proj_int_list_destroy(int* list);



    }
}




