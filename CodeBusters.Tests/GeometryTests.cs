using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBusters.Tests
{
    [TestClass]
    public class GeometryTests
    {
        [TestInitialize]
        public void Initialize()
        {

        }

        [TestMethod]
        public void IsSamePointShouldReturnTrueWhenPointsAreTheSame()
        {
            var p1 = new Point(800, 800);
            var p2 = new Point(800, 800);
            var result = Geometry.IsSamePoint(p1, p2);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsSamePointShouldReturnFalseWhenPointsAreDifferent()
        {
            var p1 = new Point(600, 800);
            var p2 = new Point(800, 800);
            var result = Geometry.IsSamePoint(p1, p2);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetLegsOfRightTriangleShouldWorkCorrectly()
        {
            var result = Geometry.GetLegsOfRightTriangle(800);

            Assert.AreEqual(565, result);
        }

        [TestMethod]
        public void GetHypothenuseOfRightTriangleShouldWorkCorrectly()
        {
            var result = Geometry.GetHypothenuseOfRightTriangle(800);

            Assert.AreEqual(1131, result);
        }

        [TestMethod]
        public void GetPointAlongLineShouldWorkCorrectly()
        {
            var basePoint = new Point(0, 0);
            var currentPoint = new Point(0, 1600);
            var result = Geometry.GetPointAlongLine(currentPoint, basePoint, 800);
            var expectedResult = new Point(0, 800);

            Assert.IsTrue(Geometry.IsSamePoint(expectedResult, result));
        }

        [TestMethod]
        public void GetPointAlongLineShouldWorkCorrectly2()
        {
            var basePoint = new Point(0, 0);
            var currentPoint = new Point(1600, 1600);
            var result = Geometry.GetPointAlongLine(currentPoint, basePoint, 800);
            var expectedResult = new Point(565, 565);

            Assert.IsTrue(Geometry.IsSamePoint(expectedResult, result));
        }
    }
}
