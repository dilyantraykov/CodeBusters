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

        [TestMethod]
        public void HasReachedCornerShouldBeTrueWhenInEitherBase()
        {
            buster.Point = Constants.Team0Base;
            Assert.IsTrue(buster.HasReachedACorner());

            buster.Point = Constants.Team1Base;
            Assert.IsTrue(buster.HasReachedACorner());
        }
    }
}
