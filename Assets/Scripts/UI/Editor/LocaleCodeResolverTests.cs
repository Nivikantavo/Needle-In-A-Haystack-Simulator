using NUnit.Framework;

namespace Needle.UI.Editor.Tests
{
    public class LocaleCodeResolverTests
    {
        static readonly string[] Available = { "en", "es", "pt", "it", "de", "fr", "tr", "ru", "id" };

        [Test]
        public void Resolve_SupportedPlatformLanguage_UsesThatCode()
        {
            Assert.AreEqual("ru", LocaleCodeResolver.Resolve("ru", Available));
        }

        [Test]
        public void Resolve_UnknownPlatformLanguage_FallsBackToEnglish()
        {
            Assert.AreEqual("en", LocaleCodeResolver.Resolve("ja", Available));
        }

        [Test]
        public void Resolve_EmptyPlatformLanguage_FallsBackToEnglish()
        {
            Assert.AreEqual("en", LocaleCodeResolver.Resolve(null, Available));
            Assert.AreEqual("en", LocaleCodeResolver.Resolve(" ", Available));
        }

        [Test]
        public void Resolve_RegionalVariant_MatchesLanguage()
        {
            Assert.AreEqual("pt", LocaleCodeResolver.Resolve("pt-BR", Available));
            Assert.AreEqual("en", LocaleCodeResolver.Resolve("en_US", Available));
        }

        [Test]
        public void Resolve_IsCaseInsensitive()
        {
            Assert.AreEqual("de", LocaleCodeResolver.Resolve("DE", Available));
        }

        [Test]
        public void ResolveInitial_SavedLocale_WinsOverPlatform()
        {
            Assert.AreEqual("ru", LocaleCodeResolver.ResolveInitial("ru", "de", Available));
        }

        [Test]
        public void ResolveInitial_MissingSaved_UsesPlatform()
        {
            Assert.AreEqual("de", LocaleCodeResolver.ResolveInitial(null, "de", Available));
        }

        [Test]
        public void ResolveInitial_InvalidSaved_UsesPlatformThenEnglish()
        {
            Assert.AreEqual("de", LocaleCodeResolver.ResolveInitial("ja", "de", Available));
            Assert.AreEqual("en", LocaleCodeResolver.ResolveInitial("ja", "zh", Available));
        }
    }
}
