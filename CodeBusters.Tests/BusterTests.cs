using System;
using CodeBusters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBusters.Tests
{
    [TestClass]
    public class BusterTests
    {
        private Buster buster;

        [TestInitialize]
        public void Initialize()
        {
             buster = new Buster(1, new Point(0, 0), 1, State.Idle, -1);
        }

        #region StunRecovery
        [TestMethod]
        public void InitialStunRecoveryShouldBeZero()
        {
            var stunRecovery = buster.StunRecovery;

            Assert.AreEqual(0, stunRecovery);
        }

        [TestMethod]
        public void StunRecoveryShouldBeTwentyAfterStun()
        {
            buster.Stun(0);
            var stunRecovery = buster.StunRecovery;

            Assert.AreEqual(20, stunRecovery);
        }
        #endregion

        #region HasReachedCorner
        [TestMethod]
        public void HasReachedCornerShouldBeTrueWhenInEitherBase()
        {
            buster.Point = Constants.Team0Base;
            Assert.IsTrue(buster.HasReachedACorner());

            buster.Point = Constants.Team1Base;
            Assert.IsTrue(buster.HasReachedACorner());
        }

        [TestMethod]
        public void HasReachedCornerShouldBeTrueWhenAtCornerOfMap()
        {
            buster.Point = Constants.BottomLeftCorner;
            Assert.IsTrue(buster.HasReachedACorner());

            buster.Point = Constants.TopRightCorner;
            Assert.IsTrue(buster.HasReachedACorner());
        }

        [TestMethod]
        public void HasReachedCornerShouldBeTrueWhenAlmostAtCorner()
        {
            var distance = Geometry.GetLegsOfRightTriangle(800);
            buster.Point = Constants.BottomLeftCorner.AddX(distance).AddY(-distance);
            Assert.IsTrue(buster.HasReachedACorner());
            
            buster.Point = Constants.TopRightCorner.AddX(-distance).AddY(distance);
            Assert.IsTrue(buster.HasReachedACorner());
        }

        [TestMethod]
        public void HasReachedCornerShouldBeFalseWhenAwayFromCorner()
        {
            var distance = Geometry.GetLegsOfRightTriangle(1761);
            buster.Point = Constants.BottomLeftCorner.AddX(distance).AddY(-distance);
            Assert.IsFalse(buster.HasReachedACorner());

            buster.Point = Constants.TopRightCorner.AddX(-distance).AddY(distance);
            Assert.IsFalse(buster.HasReachedACorner());
        }
        #endregion

        #region IsInRange
        [TestMethod]
        public void IsInRageShouldReturnTrueWhenStrictlyInRange()
        {
            buster.Point = Constants.Team0Base;
            var xAndY = Geometry.GetLegsOfRightTriangle(800);
            var otherPoint = new Point(xAndY, xAndY);
            Assert.IsTrue(buster.IsInRange(otherPoint, 1600));
        }

        [TestMethod]
        public void IsInRageShouldReturnTrueWhenAtEndOfRange()
        {
            buster.Point = Constants.Team0Base;
            var xAndY = Geometry.GetLegsOfRightTriangle(1600);
            var otherPoint = new Point(xAndY, xAndY);
            Assert.IsTrue(buster.IsInRange(otherPoint, 1600));
        }

        [TestMethod]
        public void IsInRageShouldReturnFalseWhenNotInRange()
        {
            buster.Point = Constants.Team0Base;
            var xAndY = Geometry.GetLegsOfRightTriangle(1600);
            var otherPoint = new Point(xAndY, xAndY);
            Assert.IsFalse(buster.IsInRange(otherPoint, 1599));
        }
        #endregion

        #region ShouldGoStraightToBase
        [TestMethod]
        public void ShouldGoStraightToBaseShouldBeTrueWhenOnAxis()
        {
            var targetX = Constants.Team1Base.X - (Constants.MaxGhostBustDistance * 2);
            var targetY = targetX;
            buster.Point = Constants.Team1Base.AddY(targetX);
            Assert.IsTrue(buster.ShouldGoStraightToBase(buster.TeamCoeff, targetX, targetY, GameState.Late));
        }
        #endregion
    }
}
