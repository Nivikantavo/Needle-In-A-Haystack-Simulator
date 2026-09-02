using NUnit.Framework;

namespace Needle.UI.Editor.Tests
{
    public class DeviceInputPolicyTests
    {
        [Test]
        public void Mobile_UsesTouch()
        {
            Assert.IsTrue(DeviceInputPolicy.UsesTouchControls("mobile"));
            Assert.IsTrue(DeviceInputPolicy.UsesTouchControls("Mobile"));
        }

        [Test]
        public void Tablet_UsesTouch()
        {
            Assert.IsTrue(DeviceInputPolicy.UsesTouchControls("tablet"));
        }

        [Test]
        public void Desktop_DoesNotUseTouch()
        {
            Assert.IsFalse(DeviceInputPolicy.UsesTouchControls("desktop"));
            Assert.IsFalse(DeviceInputPolicy.UsesTouchControls("Desktop"));
        }

        [Test]
        public void Tv_DoesNotUseTouch()
        {
            Assert.IsFalse(DeviceInputPolicy.UsesTouchControls("tv"));
        }

        [Test]
        public void Empty_DoesNotUseTouch()
        {
            Assert.IsFalse(DeviceInputPolicy.UsesTouchControls(null));
            Assert.IsFalse(DeviceInputPolicy.UsesTouchControls(" "));
        }
    }
}
