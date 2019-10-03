using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AltLib;

namespace AltCmdTests
{
    [TestClass]
    public class AltRefTests
    {
        [TestMethod]
        public void LocalRoundTrip()
        {
            RoundTrip(new LocalRef(123));
            RoundTrip(new LocalRef(123, 456));
            RoundTrip(new LocalRef(123, 456, "Local"));
        }

        [TestMethod]
        public void ParentRoundTrip()
        {
            RoundTrip(new ParentRef(123));
            RoundTrip(new ParentRef(123, 456));
            RoundTrip(new ParentRef(123, 456, "Parent"));
        }

        [TestMethod]
        public void AbsoluteRoundTrip()
        {
            Guid g = Guid.NewGuid();
            RoundTrip(new AbsoluteRef(g, 123));
            RoundTrip(new AbsoluteRef(g, 123, 999));
            RoundTrip(new AbsoluteRef(g, 123, 999, "Absolute"));
        }

        void RoundTrip(AltRef r)
        {
            var s = r.ToString();
            var r2 = AltRef.Create(s);
            var s2 = r2.ToString();
            Assert.AreEqual<string>(s, s2);
        }

        [TestMethod]
        public void ParseBad()
        {
            Assert.IsFalse(CanParse(null));
            Assert.IsFalse(CanParse(""));
            Assert.IsFalse(CanParse("123"));
            Assert.IsFalse(CanParse("123]"));
            Assert.IsFalse(CanParse("[123a]"));
            Assert.IsFalse(CanParse("{.}[123]"));
            Assert.IsFalse(CanParse("{abc}[123]"));
        }

        bool CanParse(string s)
        {
            return AltRef.TryParse(s, out AltRef result);
        }
    }
}
