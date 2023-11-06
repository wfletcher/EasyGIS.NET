using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EGIS.ShapeFileLib;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using EGIS.Projections;

namespace UnitTests
{
	[TestFixture]
	public class GeometryAlgorithmTests
	{

		[Test]
		public void LineSegPointDistTest_PointAtEndOfSegment()
		{
			PointD p1 = new PointD() { X = 21.5868268173493, Y = 60.6802942786999 };
			PointD p2 = new PointD() { X = 21.5861235952584, Y = 60.680414857994 };
			PointD p3 = p1;// new PointD() { X = 21.5868268173493, Y = 60.6802942786999 };
									
			double d = GeometryAlgorithms.LineSegPointDist(ref p1, ref p2, ref p3);

			Assert.AreEqual(d, 0, double.Epsilon, "Distance should be zero"); 
			d = GeometryAlgorithms.LineSegPointDist(ref p2, ref p1, ref p3);
			
			Assert.AreEqual(d, 0, double.Epsilon, "Distance should be zero when segment points reversed");
		}

		[Test]
		public void LineSegPointDistTest_PerpindicularPoint()
		{
			PointD p1 = new PointD() { X = 0, Y = 0 };
			PointD p2 = new PointD() { X = 0, Y = 10 };
			PointD p3 = new PointD() { X = 5, Y = 0 };

			double d = GeometryAlgorithms.LineSegPointDist(ref p1, ref p2, ref p3);

			Assert.AreEqual(d, 5, double.Epsilon, "Distance should be 5");
			d = GeometryAlgorithms.LineSegPointDist(ref p2, ref p1, ref p3);

			Assert.AreEqual(d, 5, double.Epsilon, "Distance should be 5 when segment points reversed");
		}

[Test]
public void TestClosestPointOnPolyLine()
{
	const int Iterations = 25000;
	string testShapeFileName = System.IO.Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data", "pmar.shp");

	using (ShapeFile shapeFile = new ShapeFile(testShapeFileName))
	{
		//read the first record geometry

		var geometry = shapeFile.GetShapeDataD(0);

		var points = geometry[0];

		var testPoint = points[6];
		testPoint.X += 0.00008;
        testPoint.Y += 0.00005;

        Console.Out.WriteLine(points.Length);

		PolylineDistanceInfo pdi = PolylineDistanceInfo.Empty;


		DateTime tick = DateTime.Now;
		for (int n = 0; n < Iterations; ++n)
		{
			GeometryAlgorithms.ClosestPointOnPolyline(points, 0, points.Length, testPoint, out pdi);
		}
        DateTime tock = DateTime.Now;

		Console.Out.WriteLine("WGS84 ClosestPointOnPolyline time:{0}ms",  tock.Subtract(tick).TotalMilliseconds );
        Console.Out.WriteLine("pdi point index:{0}, tValue:{1:0.000}",  pdi.PointIndex, pdi.TVal);

		double distance = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(testPoint.Y, testPoint.X, pdi.PolylinePoint.Y, pdi.PolylinePoint.X).Item1;

		Console.Out.WriteLine("distance:" + distance);

        ICRS pseudoMerc = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);

        using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(shapeFile.CoordinateReferenceSystem, pseudoMerc))
        {
			var transformedPoints = (PointD[])points.Clone();
			transformation.Transform(transformedPoints, TransformDirection.Forward);

            var transformedTestPoint = transformation.Transform(testPoint, TransformDirection.Forward);


            tick = DateTime.Now;
            for (int n = 0; n < Iterations; ++n)
            {
                GeometryAlgorithms.ClosestPointOnPolyline(transformedPoints, 0, transformedPoints.Length, transformedTestPoint, out pdi);
            }
            tock = DateTime.Now;

            Console.Out.WriteLine("Projected Coords ClosestPointOnPolyline time:{0}ms", tock.Subtract(tick).TotalMilliseconds);
            Console.Out.WriteLine("pdi point index:{0}, tValue:{1:0.000}", pdi.PointIndex, pdi.TVal);
        }

		//repeat wgs84 test again
        tick = DateTime.Now;
        for (int n = 0; n < Iterations; ++n)
        {
            GeometryAlgorithms.ClosestPointOnPolyline(points, 0, points.Length, testPoint, out pdi);
        }
        tock = DateTime.Now;

        Console.Out.WriteLine("WGS84 ClosestPointOnPolyline time:{0}ms", tock.Subtract(tick).TotalMilliseconds);
        Console.Out.WriteLine("pdi point index:{0}, tValue:{1:0.000}", pdi.PointIndex, pdi.TVal);

        distance = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(testPoint.Y, testPoint.X, pdi.PolylinePoint.Y, pdi.PolylinePoint.X).Item1;
        Console.Out.WriteLine("distance:" + distance);
    }
}



    }
}
