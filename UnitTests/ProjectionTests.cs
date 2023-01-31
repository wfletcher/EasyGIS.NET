using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using EGIS.Projections;
using EGIS.ShapeFileLib;

namespace UnitTests
{
	[TestFixture]
	internal class ProjectionTests
	{
		[Test]
		public void LoadLibraryAndConfirmProjDbFound()
		{
			int geoCount = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GeographicCoordinateSystems.Count;
			int projCount = EGIS.Projections.CoordinateReferenceSystemFactory.Default.ProjectedCoordinateSystems.Count;

			Assert.NotZero(geoCount);
			Assert.NotZero(projCount);

		}

		[Test]
		public void TransformWgs84ToPseudoMercRoundTrip()
		{
			const double Delta = 0.00000001;
			ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
			ICRS pseudoMerc = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);

			using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(wgs84, pseudoMerc))
			{
				PointD wgs84Pt = new PointD(145.0, -37.5);
				PointD pseudoMercPt = transformation.Transform(wgs84Pt);

				PointD roundTripPt = transformation.Transform(pseudoMercPt,TransformDirection.Inverse);

				Assert.AreEqual(wgs84Pt.X, roundTripPt.X, Delta);
				Assert.AreEqual(wgs84Pt.Y, roundTripPt.Y, Delta);
			}
		}

		[Test]
		public void TransformGDA94ToGDA2020RoundTrip()
		{
			const double Delta = 0.00001; //100th mm
			ICRS gda94Nsw = CoordinateReferenceSystemFactory.Default.GetCRSById(3308);
			ICRS gda2020Nsw = CoordinateReferenceSystemFactory.Default.GetCRSById(8058);

			using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(gda94Nsw, gda2020Nsw))
			{
				PointD gda94Pt = new PointD(9689658, 4424809);
				PointD gda2020 = transformation.Transform(gda94Pt);

				double dist = Math.Sqrt(Math.Pow(gda94Pt.X - gda2020.X, 2) + Math.Pow(gda94Pt.Y - gda2020.Y, 2));

				Assert.AreEqual(dist, 1.492, 0.001);
				PointD roundTripPt = transformation.Transform(gda2020, TransformDirection.Inverse);

				Assert.AreEqual(gda94Pt.X, roundTripPt.X, Delta);
				Assert.AreEqual(gda94Pt.Y, roundTripPt.Y, Delta);
			}
		}

		[Test]
		public void TransformWgs84To27700RoundTrip()
		{
			const double Delta = 0.00000001;
			ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
			ICRS uk27700 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);

			using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(wgs84, uk27700))
			{
				PointD wgs84Pt = new PointD(1,51);
				PointD txPt = transformation.Transform(wgs84Pt);

				Assert.IsFalse(double.IsNaN(txPt.X));
				Assert.IsFalse(double.IsNaN(txPt.Y));

				PointD roundTripPt = transformation.Transform(txPt, TransformDirection.Inverse);

				Assert.AreEqual(wgs84Pt.X, roundTripPt.X, Delta);
				Assert.AreEqual(wgs84Pt.Y, roundTripPt.Y, Delta);
			}
		}



	}
}
