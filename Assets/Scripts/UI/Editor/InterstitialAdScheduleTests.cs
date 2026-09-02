using NUnit.Framework;

namespace Needle.UI.Editor.Tests
{
    public class InterstitialAdScheduleTests
    {
        [Test]
        public void Tick_IntervalElapsed_EntersCountdownAtFive()
        {
            var schedule = new InterstitialAdSchedule();
            int shown = -1;
            schedule.CountdownChanged += seconds => shown = seconds;

            schedule.Tick(60f);

            Assert.AreEqual(InterstitialAdPhase.Countdown, schedule.Phase);
            Assert.AreEqual(5, schedule.CountdownRemaining);
            Assert.AreEqual(5, shown);
        }

        [Test]
        public void Tick_CountdownWholeSeconds_DecrementsDisplay()
        {
            var schedule = new InterstitialAdSchedule();
            int shown = -1;
            schedule.CountdownChanged += seconds => shown = seconds;
            schedule.Tick(60f);

            schedule.Tick(1f);

            Assert.AreEqual(InterstitialAdPhase.Countdown, schedule.Phase);
            Assert.AreEqual(4, schedule.CountdownRemaining);
            Assert.AreEqual(4, shown);
        }

        [Test]
        public void Tick_CountdownCompletes_RequestsShow()
        {
            var schedule = new InterstitialAdSchedule();
            bool showRequested = false;
            schedule.ShowRequested += () => showRequested = true;
            schedule.Tick(60f);

            schedule.Tick(5f);

            Assert.AreEqual(InterstitialAdPhase.Showing, schedule.Phase);
            Assert.IsTrue(showRequested);
        }

        [Test]
        public void NotifyAdFinished_ReturnsToWaiting()
        {
            var schedule = new InterstitialAdSchedule();
            schedule.Tick(60f);
            schedule.Tick(5f);

            schedule.NotifyAdFinished();

            Assert.AreEqual(InterstitialAdPhase.Waiting, schedule.Phase);
            schedule.Tick(59f);
            Assert.AreEqual(InterstitialAdPhase.Waiting, schedule.Phase);
        }

        [Test]
        public void Reset_ClearsCountdownWithoutShow()
        {
            var schedule = new InterstitialAdSchedule();
            bool showRequested = false;
            schedule.ShowRequested += () => showRequested = true;
            schedule.Tick(60f);

            schedule.Reset();
            schedule.Tick(1f);

            Assert.AreEqual(InterstitialAdPhase.Waiting, schedule.Phase);
            Assert.AreEqual(0, schedule.CountdownRemaining);
            Assert.IsFalse(showRequested);
        }

        [Test]
        public void Frozen_DoesNotAccumulateWaitingOrCountdown()
        {
            var schedule = new InterstitialAdSchedule();
            schedule.SetFrozen(true);
            schedule.Tick(60f);
            Assert.AreEqual(InterstitialAdPhase.Waiting, schedule.Phase);

            schedule.SetFrozen(false);
            schedule.Tick(60f);
            schedule.SetFrozen(true);
            schedule.Tick(5f);

            Assert.AreEqual(InterstitialAdPhase.Countdown, schedule.Phase);
            Assert.AreEqual(5, schedule.CountdownRemaining);
        }
    }
}
