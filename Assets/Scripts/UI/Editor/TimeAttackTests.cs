using NUnit.Framework;

namespace Needle.UI.Editor.Tests
{
    public class TimeAttackTests
    {
        [Test]
        public void Format_Zero_IsZeroMinutesAndSeconds()
        {
            Assert.AreEqual("00:00.00", TimeAttackClock.Format(0f));
        }

        [Test]
        public void Format_TruncatesToCentiseconds()
        {
            Assert.AreEqual("01:23.45", TimeAttackClock.Format(83.456f));
        }

        [Test]
        public void TrySubmit_FirstScore_AlwaysSaves()
        {
            var service = new TimeAttackRecordService(new MemoryTimeAttackStore());
            Assert.IsTrue(service.TrySubmit(12.5f));
            Assert.IsTrue(service.TryGetBest(out float best));
            Assert.AreEqual(12.5f, best, 0.0001f);
        }

        [Test]
        public void TrySubmit_WorseTime_DoesNotOverwrite()
        {
            var service = new TimeAttackRecordService(new MemoryTimeAttackStore());
            Assert.IsTrue(service.TrySubmit(10f));
            Assert.IsFalse(service.TrySubmit(11f));
            Assert.IsTrue(service.TryGetBest(out float best));
            Assert.AreEqual(10f, best, 0.0001f);
        }

        [Test]
        public void TrySubmit_BetterTime_Overwrites()
        {
            var service = new TimeAttackRecordService(new MemoryTimeAttackStore());
            Assert.IsTrue(service.TrySubmit(10f));
            Assert.IsTrue(service.TrySubmit(9.5f));
            Assert.IsTrue(service.TryGetBest(out float best));
            Assert.AreEqual(9.5f, best, 0.0001f);
        }

        sealed class MemoryTimeAttackStore : ITimeAttackStore
        {
            float _best;
            bool _hasBest;

            public bool TryGetBest(out float seconds)
            {
                seconds = _best;
                return _hasBest;
            }

            public void SetBest(float seconds)
            {
                _best = seconds;
                _hasBest = true;
            }
        }
    }
}
