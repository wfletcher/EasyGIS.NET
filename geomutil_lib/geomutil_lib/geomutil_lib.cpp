// psimpl_lib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "geomutil_lib.h"
#include "psimpl.h"

using namespace boost::geometry;


EXTERN_C GEOMUTIL_LIB_API int SimplifyDouglasPeuckerDbl(double* input, int inputCount, double tolerance, double* output, int &outputCount)
{
	double* outputEnd = psimpl::simplify_douglas_peucker<2>(input, input +(inputCount*2), tolerance, output);
	outputCount = (outputEnd-output)/2;
	return 0;
}

EXTERN_C GEOMUTIL_LIB_API int SimplifyDouglasPeuckerInt(int* input, int inputCount, int tolerance, int* output, int &outputCount)
{
	int* outputEnd = psimpl::simplify_douglas_peucker<2>(input, input +(inputCount*2), tolerance, output);
	outputCount = (outputEnd-output)/2;
	return 0;
}

EXTERN_C GEOMUTIL_LIB_API bool PolygonRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY)
{
	typedef boost::geometry::model::d2::point_xy<double> Point2D;
	typedef boost::geometry::model::ring<Point2D> Ring2D;
	typedef boost::geometry::model::box<Point2D> Box2D;
	typedef boost::geometry::model::polygon<Point2D> Polygon2D;

	Box2D box(Point2D(rMinX,rMinY), Point2D(rMaxX,rMaxY));
	Ring2D ring((Point2D*)points, (Point2D*)points+pointCount);
	Polygon2D boxPolygon;
	boost::geometry::assign(boxPolygon, box);
	boost::geometry::correct(boxPolygon); //closing point

	return boost::geometry::intersects(boxPolygon, ring);
}



class Rect
{
public:

	Rect(double x, double y, double w, double h):rectX(x), rectY(y), rectWidth(w), rectHeight(h){}
	
	enum OutCode {OUT_LEFT=1, OUT_TOP=2, OUT_RIGHT=4, OUT_BOTTOM=8};
	double rectX;
	double rectY;
	double rectWidth;
	double rectHeight;

	static int outcode(double pX, double pY, const Rect & r)
	{
        int out = 0;
        if (r.rectWidth <= 0) {
			out |= Rect::OUT_LEFT | Rect::OUT_RIGHT;
        } else if (pX < r.rectX) {
			out |= Rect::OUT_LEFT;
        } else if (pX > r.rectX + r.rectWidth) {
			out |= Rect::OUT_RIGHT;
        }
        if (r.rectHeight <= 0) {
			out |= Rect::OUT_TOP | Rect::OUT_BOTTOM;
        } else if (pY < r.rectY) {
			out |= Rect::OUT_TOP;
        } else if (pY > r.rectY + r.rectHeight) {
			out |= Rect::OUT_BOTTOM;
        }
        return out;
    }

    static bool intersectsLine(double lineX1, double lineY1, double lineX2, double lineY2, const Rect &r)
	{
        int out1, out2;
        if ((out2 = outcode(lineX2, lineY2, r)) == 0) {
            return true;
        }
        while ((out1 = outcode(lineX1, lineY1, r)) != 0) {
            if ((out1 & out2) != 0) {
                return false;
            }
			if ((out1 & (Rect::OUT_LEFT | Rect::OUT_RIGHT)) != 0) {
                double x = r.rectX;
				if ((out1 & Rect::OUT_RIGHT) != 0) {
                    x += r.rectWidth;
                }
                lineY1 = lineY1 + (x - lineX1) * (lineY2 - lineY1) / (lineX2 - lineX1);
                lineX1 = x;
            } else {
                double y = r.rectY;
				if ((out1 & Rect::OUT_BOTTOM) != 0) {
                    y += r.rectHeight;
                }
                lineX1 = lineX1 + (y - lineY1) * (lineX2 - lineX1) / (lineY2 - lineY1);
                lineY1 = y;
            }
        }
        return true;
    }
};

	

EXTERN_C GEOMUTIL_LIB_API bool PolyLineRectIntersect(void* points, int pointCount, double rMinX, double rMinY, double rMaxX, double rMaxY)
{
	struct Point
	{
		double x;
		double y;
	};

	if(pointCount <2) return false;
	Rect r(rMinX, rMinY, rMaxX-rMinX, rMaxY-rMinY);
	Point* pPtr = (Point*)points;
	for(int n=0;n<pointCount-1;++n)
	{
		if(Rect::intersectsLine(pPtr[n].x, pPtr[n].y, pPtr[n+1].x, pPtr[n+1].y, r)) return true;		
	}
	return false;

}
