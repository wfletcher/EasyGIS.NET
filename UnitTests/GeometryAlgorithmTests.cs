using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EGIS.ShapeFileLib;

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
	}
}
