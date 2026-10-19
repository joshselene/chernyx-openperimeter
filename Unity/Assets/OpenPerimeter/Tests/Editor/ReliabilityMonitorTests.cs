using NUnit.Framework;
using OpenPerimeter.Reliability;

namespace OpenPerimeter.Tests
{
    [TestFixture]
    public class ReliabilityMonitorTests
    {
        private ReliabilityMonitor monitor;

        [SetUp]
        public void Setup()
        {
            monitor = new ReliabilityMonitor();
        }

        [Test]
        public void FixationLoss_CalculatesPercentageCorrectly()
        {
            // 2 misses out of 10 presentations = 20%
            for (int i = 0; i < 8; i++) monitor.RecordBlindSpotTrial(false);
            for (int i = 0; i < 2; i++) monitor.RecordBlindSpotTrial(true);

            Assert.AreEqual(20.0f, monitor.GetFixationLossPercent(), 0.01f);
        }

        [Test]
        public void FalsePositive_CalculatesPercentageCorrectly()
        {
            // 1 click out of 10 catch trials = 10%
            for (int i = 0; i < 9; i++) monitor.RecordFalsePositiveTrial(false);
            monitor.RecordFalsePositiveTrial(true);

            Assert.AreEqual(10.0f, monitor.GetFalsePositivePercent(), 0.01f);
        }

        [Test]
        public void IsTestReliable_PassesWhenWithinStudyCriteria()
        {
            // FL = 10% (<20%), FP = 5% (<15%), FN = 5% (<15%)
            for (int i = 0; i < 9; i++) monitor.RecordBlindSpotTrial(false);
            monitor.RecordBlindSpotTrial(true); // 10%

            for (int i = 0; i < 19; i++) monitor.RecordFalsePositiveTrial(false);
            monitor.RecordFalsePositiveTrial(true); // 5%

            for (int i = 0; i < 19; i++) monitor.RecordFalseNegativeTrial(true);
            monitor.RecordFalseNegativeTrial(false); // 5%

            bool reliable = monitor.IsTestReliable(out string reason);
            Assert.IsTrue(reliable);
            Assert.IsEmpty(reason);
        }

        [Test]
        public void IsTestReliable_FailsWhenFixationLossExceeds20Percent()
        {
            // FL = 3 out of 10 = 30% (>20%)
            for (int i = 0; i < 7; i++) monitor.RecordBlindSpotTrial(false);
            for (int i = 0; i < 3; i++) monitor.RecordBlindSpotTrial(true);

            bool reliable = monitor.IsTestReliable(out string reason);
            Assert.IsFalse(reliable);
            StringAssert.Contains("Fixation Loss", reason);
        }
    }
}
