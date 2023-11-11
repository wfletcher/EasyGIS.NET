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

		/// <summary>
		/// tests that converting a point from GDA94 -> GDA2020 and back again via wgs84
		/// is the same.
		/// </summary>
		/// <remarks>
		/// This test passes in Proj9.0 but due to changes in 9.1 no longer passes.
		/// The issue with the change in proj is if a gda94 shapefile is displayed on the 
		/// map and the same shapefile converted to gda2020 is displayed on the map
		/// the same points from both shapefiles are displayed on top of each other if either CRS is selected,
		/// but if the map CRS is changed to wgs 84 the points are offset by approx 1.5m.
		/// </remarks>
		[Test]
		public void TestGDA94ToGDA2020RoundTripViaWgs84IsSame()
		{
			const double Delta = 0.00001; //0.01 mm
			ICRS gda94Nsw = CoordinateReferenceSystemFactory.Default.GetCRSById(3308);
			ICRS gda2020Nsw = CoordinateReferenceSystemFactory.Default.GetCRSById(8058);
			ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(4326);

			using (ICoordinateTransformation gda94ToGda2020Transform = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(gda94Nsw, gda2020Nsw))
			using (ICoordinateTransformation gda94ToWgs84Transform = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(gda94Nsw, wgs84))
			using (ICoordinateTransformation gda2020ToWgs84Transform = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(gda2020Nsw, wgs84))
			{
				PointD gda94Pt = new PointD(9689658, 4424809);
				PointD gda2020 = gda94ToGda2020Transform.Transform(gda94Pt);

				double dist = Math.Sqrt(Math.Pow(gda94Pt.X - gda2020.X, 2) + Math.Pow(gda94Pt.Y - gda2020.Y, 2));
				Assert.AreEqual(dist, 1.492, 0.001);//expected

				PointD wgs84Pt = gda2020ToWgs84Transform.Transform(gda2020);

				PointD gda94PtRoundTrip = gda94ToWgs84Transform.Transform(wgs84Pt, TransformDirection.Inverse);

				Assert.AreEqual(gda94Pt.X, gda94PtRoundTrip.X, Delta);
				Assert.AreEqual(gda94Pt.Y, gda94PtRoundTrip.Y, Delta);
			}
		}

		[Test]
		public void TransformWgs84To27700RoundTrip()
		{
			const double Delta = 0.0000001;
			ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
			ICRS uk27700 = CoordinateReferenceSystemFactory.Default.GetCRSById(27700);

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


		[Test]
		public void MeasureDistanceUsingUkNationalGrid27700()
		{
			const double Delta = 0.0000001;
			ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
			ICRS uk27700 = CoordinateReferenceSystemFactory.Default.GetCRSById(27700);

			using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(wgs84, uk27700))
			{
				double dist1, dist2, dist3, dist4,dist5;
				PointD wgs84PtA = new PointD(-5, 50);
				PointD wgs84PtB = new PointD(0, 51.5);

				PointD ptA = transformation.Transform(wgs84PtA);
				PointD ptB = transformation.Transform(wgs84PtB);
				
				Console.Out.WriteLine("wgs84PtA: " + wgs84PtA);
				Console.Out.WriteLine("wgs84Ptb: " + wgs84PtB);

				Console.Out.WriteLine("PtA: " + ptA);
				Console.Out.WriteLine("Ptb: " + ptB);


				dist1 = Math.Sqrt(Math.Pow(ptA.X-ptB.X,2) + Math.Pow(ptA.Y - ptB.Y, 2));

				dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
					ConversionFunctions.Wgs84RefEllipse,wgs84PtA.Y,wgs84PtA.X,
					wgs84PtB.Y,wgs84PtB.X);


				dist3 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(
					ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
					wgs84PtB.Y, wgs84PtB.X);

				dist4 = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
					ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
					wgs84PtB.Y, wgs84PtB.X).Item1;

				dist5 = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).Distance(wgs84,
					wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

				Console.Out.WriteLine("dist1:{0:0.000000}km ", dist1 / 1000 );
				Console.Out.WriteLine("dist2:{0:0.000000}km ", dist2 / 1000);
				Console.Out.WriteLine("dist3:{0:0.000000}km ", dist3 / 1000);
				Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
				Console.Out.WriteLine("dist5:{0:0.00000}km ", dist5 / 1000);


				wgs84PtA = new PointD(-5.539267, 50.118445);
				wgs84PtB = new PointD(-0.127724, 51.507407);

				ptA = transformation.Transform(wgs84PtA);
				ptB = transformation.Transform(wgs84PtB);

				Console.Out.WriteLine("wgs84PtA: " + wgs84PtA);
				Console.Out.WriteLine("wgs84Ptb: " + wgs84PtB);


				dist1 = Math.Sqrt(Math.Pow(ptA.X - ptB.X, 2) + Math.Pow(ptA.Y - ptB.Y, 2));

				dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
					ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
					wgs84PtB.Y, wgs84PtB.X);


				dist3 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(
					ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
					wgs84PtB.Y, wgs84PtB.X);

				dist4 = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
					ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
					wgs84PtB.Y, wgs84PtB.X).Item1;

				dist5 = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).Distance(wgs84,
					wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

				Console.Out.WriteLine("dist1:{0:0.000000}km ", dist1 / 1000);
				Console.Out.WriteLine("dist2:{0:0.000000}km ", dist2 / 1000);
				Console.Out.WriteLine("dist3:{0:0.000000}km ", dist3 / 1000);
				Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
				Console.Out.WriteLine("dist5:{0:0.00000}km ", dist5 / 1000);


				
			}
		}

		[Test]
		public void TestEllipsoidAndGeodesicDistanceCalculations()
		{
			const double Delta = 0.001; //1mm
			ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
			ICRS uk27700 = CoordinateReferenceSystemFactory.Default.GetCRSById(27700);

			
			double dist2, dist3, dist4, dist5;
			PointD wgs84PtA = new PointD(-5, 50);
			PointD wgs84PtB = new PointD(0, 51.5);

			Console.Out.WriteLine("wgs84PtA: " + wgs84PtA);
			Console.Out.WriteLine("wgs84Ptb: " + wgs84PtB);

			
			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);


			dist3 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X).Item1;

			dist5 = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).Distance(wgs84,
				wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("dist2:{0:0.000000}km ", dist2 / 1000);
			Console.Out.WriteLine("dist3:{0:0.000000}km ", dist3 / 1000);
			Console.Out.WriteLine("dist4:{0:0.00000000}km ", dist4 / 1000);
			Console.Out.WriteLine("dist5:{0:0.00000000}km ", dist5 / 1000);

			Assert.AreEqual(dist4, 390224.992585395, Delta); //Sql server stDistance = 390224.992585395


			wgs84PtA = new PointD(-5.539267, 50.118445);
			wgs84PtB = new PointD(-0.127724, 51.507407);


			Console.Out.WriteLine("wgs84PtA: " + wgs84PtA);
			Console.Out.WriteLine("wgs84Ptb: " + wgs84PtB);


			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);


			dist3 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X).Item1;

			dist5 = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).Distance(wgs84,
				wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("dist2:{0:0.000000}km ", dist2 / 1000);
			Console.Out.WriteLine("dist3:{0:0.000000}km ", dist3 / 1000);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", dist5 / 1000);

			Assert.AreEqual(dist4, 411386.590670174, Delta); //Sql StDistance


			wgs84PtA = new PointD(0, 0);
			wgs84PtB = new PointD(0, 1);

			var db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			double bearing = db.Item2;

			dist5 = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).Distance(wgs84,
				wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);


			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);

			Console.Out.WriteLine("dist5:{0:0.00000}km ", dist5 / 1000);


			wgs84PtA = new PointD(0, 0);
			wgs84PtB = new PointD(1, 0);

			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			bearing = db.Item2;

			var projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
				wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);

			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);

			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);




			wgs84PtA = new PointD(0, 0);
			wgs84PtB = new PointD(10, 10);

			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			bearing = db.Item2;

		
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
				wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);

			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);

			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);


			
			wgs84PtA = new PointD(145, -37);
			wgs84PtB = new PointD(150, 10);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);


			wgs84PtA = new PointD(145, -37);
			wgs84PtB = new PointD(100, -50);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);


			wgs84PtA = new PointD(0, 0);
			wgs84PtB = new PointD(90, 0);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);


			wgs84PtA = new PointD(-45, 0);
			wgs84PtB = new PointD(90, 0);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);


			wgs84PtA = new PointD(-90, 0);
			wgs84PtB = new PointD(90, 0);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);


			wgs84PtA = new PointD(0, 0);
			wgs84PtB = new PointD(180, 0);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);



			wgs84PtA = new PointD(-5, 70);
			wgs84PtB = new PointD(5, 75);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);



			wgs84PtA = new PointD(0, -90);
			wgs84PtB = new PointD(0, 90);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;
			bearing = db.Item2;
			projDistAndBearing = ((EGIS.Projections.CoordinateReferenceSystemFactory)EGIS.Projections.CoordinateReferenceSystemFactory.Default).DistanceAndBearing(wgs84,
			wgs84PtA.X, wgs84PtA.Y, wgs84PtB.X, wgs84PtB.Y);

			Console.Out.WriteLine("ptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("dist4:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("bearing:{0:0.00000}deg ", bearing);
			Console.Out.WriteLine("dist5:{0:0.00000}km ", projDistAndBearing.Item1 / 1000);
			Console.Out.WriteLine("proj bearing:{0:0.00000}deg ", projDistAndBearing.Item2);

			//confirm results same as proj results
			Assert.AreEqual(dist4, projDistAndBearing.Item1, Delta);
			Assert.AreEqual(bearing, projDistAndBearing.Item2, Delta);


			wgs84PtA = new PointD(140, -35);
			wgs84PtB = new PointD(141, -35);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Spherical dist:{0:0.00000}km ", dist2 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));

			//Sql Server STDistance result 91287.7884371744

			Assert.AreEqual(dist4, 91287.7884371744, 0.001);


			wgs84PtA = new PointD(140, -35);
			wgs84PtB = new PointD(141, -36);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Spherical dist:{0:0.00000}km ", dist2 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));

			wgs84PtA = new PointD(140.0, -35);
			wgs84PtB = new PointD(140.005, -35.02);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Spherical dist:{0:0.00000}km ", dist2 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));


			wgs84PtA = new PointD(0, 2);
			wgs84PtB = new PointD(1, 2);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Spherical dist:{0:0.00000}km ", dist2 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));

			wgs84PtA = new PointD(50, 0);
			wgs84PtB = new PointD(51, 0);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Spherical dist:{0:0.00000}km ", dist2 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));

			wgs84PtA = new PointD(10, 0);
			wgs84PtB = new PointD(55, 0);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Spherical dist:{0:0.00000}km ", dist2 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));


			wgs84PtA = new PointD(175, 10);
			wgs84PtB = new PointD(-175, 10);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);



			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));

			wgs84PtA = new PointD(175, 10);
			wgs84PtB = new PointD(185, 10);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);


			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));

			wgs84PtA = new PointD(0, 10);
			wgs84PtB = new PointD(355, 10);
			db = EGIS.ShapeFileLib.ConversionFunctions.GeodesicDistanceAndBearingBetweenLatLonPoints(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);

			dist4 = db.Item1;

			dist2 = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(
				ConversionFunctions.Wgs84RefEllipse, wgs84PtA.Y, wgs84PtA.X,
				wgs84PtB.Y, wgs84PtB.X);


			Console.Out.WriteLine("\nptA:" + wgs84PtA);
			Console.Out.WriteLine("ptB:" + wgs84PtB);
			Console.Out.WriteLine("Ellipsoid dist:{0:0.00000}km ", dist4 / 1000);
			Console.Out.WriteLine("Difference:{0:0.00000}m ", (dist4 - dist2));

			
		}

		[Test]
		public void TestWkt2018EsriIsEquivalent()
		{
			ICRS wgs84CRS = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);

			string wktEsri = wgs84CRS.GetWKT(PJ_WKT_TYPE.PJ_WKT1_ESRI, false);
			string wkt2018 = wgs84CRS.GetWKT(PJ_WKT_TYPE.PJ_WKT2_2018_SIMPLIFIED, false);

			ICRS crsA = CoordinateReferenceSystemFactory.Default.CreateCRSFromWKT(wktEsri);
			ICRS crsB = CoordinateReferenceSystemFactory.Default.CreateCRSFromWKT(wkt2018);

			bool equivalent = crsA.IsEquivalent(crsB);

			Assert.IsTrue(equivalent, "Expected result: CRS created from ESRI WKT format equivalent to CRS created from 2018 WKT2 format");
		}

		[Test]
		public void TestICRSNotIsEquivalentToNullCRS()
		{
			ICRS wgs84CRS = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
			
			bool equivalent = wgs84CRS.IsEquivalent(null);

			Assert.IsFalse(equivalent, "Expected result: CRS not equivalent to null CRS");
		}

        [Test]
        public void TransformWgs84ToNad83ArkNorthFtRoundTrip()
        {
            const double Delta = 0.0000001;
            ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
            // EPSG:3433  NAD83 / Arkansas North (ftUS)
            ICRS nad83ArkNFt = CoordinateReferenceSystemFactory.Default.GetCRSById(3433);

            using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(wgs84, nad83ArkNFt))
            {
                PointD wgs84Pt = new PointD(-90.5806563, 35.8019722);
                PointD arkansasPt = transformation.Transform(wgs84Pt);

				//Note that if a transformation grid is used then y coord may differ
				//by 2-3 feet. 
				//grids are automatically found in 
				//C:\OSGeo4W\share or ${LOCALAPPDATA}/proj
				//https://proj.org/en/9.3/resource_files.html
				//
				//Assert.True(Math.Abs(arkansasPt.X - 1733200.9) < 0.1);
				//Assert.True(Math.Abs(arkansasPt.Y - 537594.1) < 0.1);

				PointD roundTripPt = transformation.Transform(arkansasPt, TransformDirection.Inverse);

                Assert.True(Math.Abs(wgs84Pt.X - roundTripPt.X) < Delta);
                Assert.True(Math.Abs(wgs84Pt.Y - roundTripPt.Y) < Delta);
            }
        }

		[Test]
		public void TestEPSG32636AreaOfUse()
		{
			var crs = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(32636);

			Console.Out.WriteLine(crs.AreaOfUse);

            crs = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);

            Console.Out.WriteLine(crs.AreaOfUse);
            //EGIS.Projections.CoordinateReferenceSystemFactory.GetWgs84UtmEpsgCode(

            crs = CoordinateReferenceSystemFactory.Default.GetCRSById(3433);
            Console.Out.WriteLine(crs.AreaOfUse);


			int utmCode = EGIS.Projections.CoordinateReferenceSystemFactory.GetWgs84UtmEpsgCode(145, -37);
            crs = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(utmCode);

            Console.Out.WriteLine(crs.AreaOfUse);
        }

		[Test]
		public void TestTransformEPSG3052ToEPSG4326()
		{
			var crs = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(3052);			
			var crsWgs84 = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);

			Console.Out.WriteLine(crs.AreaOfUse);
			using (var transform = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(crsWgs84, crs))
			{
				var pt = new PointD(-20, 65);
				var pt2 = transform.Transform(pt);
				Console.Out.WriteLine(pt2);

				pt = new PointD(-24.66, 63.34);
				pt2 = transform.Transform(pt);
				Console.Out.WriteLine(pt2);

			}
		}

	}
}
