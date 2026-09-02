using NUnit.Framework;

namespace Needle.UI.Editor.Tests
{
    public class GameplayCursorPolicyTests
    {
        [Test]
        public void Menu_DoesNotLock()
        {
            Assert.IsFalse(GameplayCursorPolicy.ShouldLock(false, false, false, false, false, true));
        }

        [Test]
        public void Pause_DoesNotLock()
        {
            Assert.IsFalse(GameplayCursorPolicy.ShouldLock(true, true, false, true, false, true));
        }

        [Test]
        public void Victory_DoesNotLock()
        {
            Assert.IsFalse(GameplayCursorPolicy.ShouldLock(true, true, true, true, false, true));
        }

        [Test]
        public void Gameplay_PointerOverUi_DoesNotLock()
        {
            Assert.IsFalse(GameplayCursorPolicy.ShouldLock(true, false, false, true, true, true));
        }

        [Test]
        public void Gameplay_ClickOnUi_DoesNotLock()
        {
            Assert.IsFalse(GameplayCursorPolicy.ShouldLock(true, false, false, false, true, true));
        }

        [Test]
        public void Gameplay_UnlockedWithoutClick_StaysUnlocked()
        {
            Assert.IsFalse(GameplayCursorPolicy.ShouldLock(true, false, false, false, false, false));
        }

        [Test]
        public void Gameplay_ClickOutsideUi_Locks()
        {
            Assert.IsTrue(GameplayCursorPolicy.ShouldLock(true, false, false, false, false, true));
        }

        [Test]
        public void TouchControls_NeverLock()
        {
            Assert.IsFalse(GameplayCursorPolicy.ShouldLock(true, false, false, true, false, true, true));
        }
    }
}
