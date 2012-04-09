// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the GEOMUTIL_LIB_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// GEOMUTIL_LIB_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef GEOMUTIL_LIB_EXPORTS
#define GEOMUTIL_LIB_API __declspec(dllexport)
#else
#define GEOMUTIL_LIB_API __declspec(dllimport)
#endif

//conditionally setup for C++/C compilers
//
#ifdef __cplusplus
	#define EXTERN_C     extern "C"
#else
	#define EXTERN_C
#endif // __cplusplus


EXTERN_C GEOMUTIL_LIB_API int SimplifyDouglasPeuckerDbl(double* input, int inputCount, double tolerance, double* output, int &outputCount);

EXTERN_C GEOMUTIL_LIB_API int SimplifyDouglasPeuckerInt(int* input, int inputCount, int tolerance, int* output, int &outputCount);


EXTERN_C GEOMUTIL_LIB_API bool PolygonRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY);

EXTERN_C GEOMUTIL_LIB_API bool PolyLineRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY);
