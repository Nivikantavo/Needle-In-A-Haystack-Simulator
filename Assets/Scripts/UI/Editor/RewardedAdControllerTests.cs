using NUnit.Framework;

namespace Needle.UI.Editor.Tests
{
    public class RewardedAdControllerTests
    {
        [Test]
        public void FormatLabel_SubstitutesCount()
        {
            Assert.AreEqual("remove 200 pieces", RewardedAdController.FormatLabel("remove {0} pieces", 200));
        }

        [Test]
        public void FormatLabel_EmptyTemplate_UsesFallback()
        {
            Assert.AreEqual("remove 50 pieces", RewardedAdController.FormatLabel(null, 50));
            Assert.AreEqual("remove 50 pieces", RewardedAdController.FormatLabel(string.Empty, 50));
        }
    }
}
