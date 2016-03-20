using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

namespace EGIS.ShapeFileLib
{
    internal sealed class NativeMethods
    {
        private NativeMethods() { }

        /// <summary>
        /// constant representing the OPAQUE Background Mode
        /// </summary>
        public const int OPAQUE = 2;

        /// <summary>
        /// constant representing the TRANSPARENT Background Mode
        /// </summary>
        public const int TRANSPARENT = 1;

        // Pen Style constants
        public const int PS_SOLID = 0;
        public const int PS_DASH = 1;
        public const int PS_DOT = 2;
        public const int PS_DASHDOT = 3;
        public const int NULL_BRUSH = 5;

        //BitBlt constants
        public const int SRCCOPY = 0xcc0020;

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static unsafe extern int Polyline(IntPtr hdc, Point* points, int count);

        public static unsafe void DrawPolyline(IntPtr hdc, Point[] points, int count)
        {
            fixed (Point* ptr = points)
            {
                Polyline(hdc, ptr, count);
            }
        }

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern unsafe int Polygon(IntPtr hdc, Point* points, int count);

        public static unsafe void DrawPolygon(IntPtr hdc, Point[] points, int count)
        {
            fixed (Point* ptr = points)
            {
                Polygon(hdc, ptr, count);
            }
        }

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern int Ellipse(IntPtr hdc, int left, int top, int right, int bottom);

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr gdiobj);

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern IntPtr GetStockObject(int index);

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int rgbColor);

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern IntPtr CreateSolidBrush(int rgbColor);

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern int DeleteObject(IntPtr gdiobj);

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern int SetBkMode(IntPtr hdc, int mode);


        internal const uint PAGE_READONLY = 0x02;
        internal const uint PAGE_READWRITE = 0x04;


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
          uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
          uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateFileMapping(Microsoft.Win32.SafeHandles.SafeFileHandle hFile, IntPtr lpAttributes,
            uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        internal static IntPtr MapFile(System.IO.FileStream fs)
        {
            return CreateFileMapping(fs.SafeFileHandle, IntPtr.Zero, /*fs.CanWrite ? PAGE_READWRITE :*/ PAGE_READONLY, 0, 0, null);
        }

        internal enum FileMapAccess { FILE_MAP_WRITE = 0x02, FILE_MAP_READ = 0x04 };

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int UnmapViewOfFile(IntPtr lpBaseAddress);

        //HANDLE CreateFileMapping(HANDLE hFile,LPSECURITY_ATTRIBUTES lpAttributes, DWORD flProtect, DWORD dwMaximumSizeHigh, DWORD dwMaximumSizeLow, LPCTSTR lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int CloseHandle(IntPtr handle);

       
        class NativeGeomUtilWin32
        {
            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int SimplifyDouglasPeuckerInt(int* input, int inputCount, int tolerance, int* output, ref int outputCount);

            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int SimplifyDouglasPeuckerDbl(double* input, int inputCount, double tolerance, double* output, ref int outputCount);            
            
            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolygonRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY);

            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolyLineRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY);

            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]            
            internal static unsafe extern int PolygonPolygonIntersect(double* points1, int points1Count, double* points2, int points2Count);

            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolyLinePolygonIntersect(double* polyLinePoints, int polyLinePointsCount, double* polygonPoints, int polygonPointsCount);

            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int RectWithinPolygon(double rMinX, double rMinY, double rMaxX, double rMaxY, void* points, int pointCount);

            [DllImport("geomutil_lib.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolygonTouchesPolygon(double* points1, int points1Count, double* points2, int points2Count);
        }

        class NativeGeomUtilX64
        {
            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int SimplifyDouglasPeuckerInt(int* input, int inputCount, int tolerance, int* output, ref int outputCount);

            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int SimplifyDouglasPeuckerDbl(double* input, int inputCount, double tolerance, double* output, ref int outputCount);            

            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolygonRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY);

            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolyLineRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY);

            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]            
            internal static unsafe extern int PolygonPolygonIntersect(double* points1, int points1Count, double* points2, int points2Count);

            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolyLinePolygonIntersect(double* polyLinePoints, int polyLinePointsCount, double* polygonPoints, int polygonPointsCount);

            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int RectWithinPolygon(double rMinX, double rMinY, double rMaxX, double rMaxY, void* points, int pointCount);

            [DllImport("geomutil_libx64.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static unsafe extern int PolygonTouchesPolygon(double* points1, int points1Count, double* points2, int points2Count);
        }


        static bool IsWin32Process()
        {
            return (IntPtr.Size == 4);
        }

        internal static unsafe int SimplifyDouglasPeuckerInt(int* input, int inputCount, int tolerance, int* output, ref int outputCount)
        {
            if (IsWin32Process())
            {
                return NativeGeomUtilWin32.SimplifyDouglasPeuckerInt(input, inputCount, tolerance, output, ref outputCount);
            }
            return NativeGeomUtilX64.SimplifyDouglasPeuckerInt(input, inputCount, tolerance, output, ref outputCount);

        }

        internal static unsafe int SimplifyDouglasPeuckerDbl(double* input, int inputCount, double tolerance, double* output, ref int outputCount)
        {
            if (IsWin32Process())
            {
                return NativeGeomUtilWin32.SimplifyDouglasPeuckerDbl(input, inputCount, tolerance, output, ref outputCount);
            }
            return NativeGeomUtilX64.SimplifyDouglasPeuckerDbl(input, inputCount, tolerance, output, ref outputCount);
        }


        internal static unsafe bool PolygonRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY)
        {
            if (IsWin32Process())
            {
                return (NativeGeomUtilWin32.PolygonRectIntersect(points, pointCount, rMinX, rMinY, rMaxX, rMaxY) != 0);
            }
            return (NativeGeomUtilX64.PolygonRectIntersect(points, pointCount, rMinX, rMinY, rMaxX, rMaxY) != 0);
        }

        internal static unsafe bool PolyLineRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY)
        {
            if (IsWin32Process())
            {
                int c = NativeGeomUtilWin32.PolyLineRectIntersect(points, pointCount, rMinX, rMinY, rMaxX, rMaxY);
                //Console.Out.WriteLine("c = " + c);
                return c != 0;
            }
            return NativeGeomUtilX64.PolyLineRectIntersect(points, pointCount, rMinX, rMinY, rMaxX, rMaxY) != 0;
        }

        internal static unsafe bool PolygonPolygonIntersect(double* points1, int points1Count, double* points2, int points2Count)
        {
            if (IsWin32Process())
            {
                return NativeGeomUtilWin32.PolygonPolygonIntersect(points1, points1Count, points2, points2Count) != 0;
            }
            return NativeGeomUtilX64.PolygonPolygonIntersect(points1, points1Count, points2, points2Count) != 0;
        }

        internal static unsafe bool PolygonPolygonIntersect(PointD[] points1, int points1Count, PointD[] points2, int points2Count)
        {
            fixed (PointD* points1Ptr = points1)
            fixed (PointD* points2Ptr = points2)
            {
                return PolygonPolygonIntersect((double*)points1Ptr, points1Count, (double*)points2Ptr, points2Count);
            }
        }

        
        internal static unsafe bool PolyLinePolygonIntersect(double* polyLinePoints, int polyLinePointsCount, double* polygonPoints, int polygonPointsCount)
        {
            if (IsWin32Process())
            {
                return NativeGeomUtilWin32.PolyLinePolygonIntersect(polyLinePoints, polyLinePointsCount, polygonPoints, polygonPointsCount) != 0;
            }
            return NativeGeomUtilX64.PolyLinePolygonIntersect(polyLinePoints, polyLinePointsCount, polygonPoints, polygonPointsCount) != 0;
        }

        internal static unsafe bool PolyLinePolygonIntersect(PointD[] polyLinePoints, int polyLinePointsCount, PointD[] polygonPoints, int polygonPointsCount)
        {
            fixed (PointD* points1Ptr = polyLinePoints)
            fixed (PointD* points2Ptr = polygonPoints)
            {
                return PolyLinePolygonIntersect((double*)points1Ptr, polyLinePointsCount, (double*)points2Ptr, polygonPointsCount);
            }
        }


        internal static unsafe int SimplifyDouglasPeucker(Point[] input, int inputCount, int tolerance, Point[] output, ref int outputCount)
        {
            fixed (Point* inputPtr = input)
            {
                fixed (Point* outputPtr = output)
                {
                    return SimplifyDouglasPeuckerInt((int*)inputPtr, inputCount, tolerance, (int*)outputPtr, ref outputCount);
                }
            }
        }

        internal static unsafe int SimplifyDouglasPeucker(PointD[] input, int inputCount, double tolerance, PointD[] output, ref int outputCount)
        {
            fixed (PointD* inputPtr = input)
            {
                fixed (PointD* outputPtr = output)
                {
                    return SimplifyDouglasPeuckerDbl((double*)inputPtr, inputCount, tolerance, (double*)outputPtr, ref outputCount);
                }
            }
        }


        internal static unsafe bool PolygonRectIntersect(double[] data, int dataLength, double rMinX, double rMinY, double rMaxX, double rMaxY)
        {
            fixed (double* ptr = data)
            {
                return PolygonRectIntersect(ptr, dataLength >> 1, rMinX, rMinY, rMaxX, rMaxY);
            }
        }


        internal static unsafe bool PolyLineRectIntersect(double[] data, int dataLength, double rMinX, double rMinY, double rMaxX, double rMaxY)
        {
            fixed (double* ptr = data)
            {
                return PolyLineRectIntersect(ptr, dataLength >> 1, rMinX, rMinY, rMaxX, rMaxY);
            }
        }

        internal static unsafe bool RectWithinPolygon(double[] data, int dataLength, double rMinX, double rMinY, double rMaxX, double rMaxY)
        {
            fixed (double* ptr = data)
            {
                return RectWithinPolygon(rMinX, rMinY, rMaxX, rMaxY, ptr, dataLength >> 1);
            }
        }

        internal static unsafe bool RectWithinPolygon(double rMinX, double rMinY, double rMaxX, double rMaxY, void* points, int pointCount)
        {
            if (IsWin32Process())
            {
                return (NativeGeomUtilWin32.RectWithinPolygon(rMinX, rMinY, rMaxX, rMaxY, points, pointCount) != 0);
            }
            return (NativeGeomUtilX64.RectWithinPolygon(rMinX, rMinY, rMaxX, rMaxY, points, pointCount) != 0);
        }

        internal static unsafe bool PolygonTouchesPolygon(PointD[] points1, int points1Count, PointD[] points2, int points2Count)
        {
            fixed (PointD* points1Ptr = points1)
            fixed (PointD* points2Ptr = points2)
            {
                return PolygonTouchesPolygon((double*)points1Ptr, points1Count, (double*)points2Ptr, points2Count);
            }
        }
        
        internal static unsafe bool PolygonTouchesPolygon(double* points1, int points1Count, double* points2, int points2Count)
        {
            if (IsWin32Process())
            {
                return (NativeGeomUtilWin32.PolygonTouchesPolygon(points1, points1Count, points2, points2Count) != 0);
            }
            return (NativeGeomUtilX64.PolygonTouchesPolygon(points1, points1Count, points2, points2Count) != 0);
        }
        
    }

}
