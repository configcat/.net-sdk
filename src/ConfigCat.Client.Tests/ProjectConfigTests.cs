using System;
using ConfigCat.Client.ConfigService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class ProjectConfigTests
    {
        [TestMethod]
        public void Equatable_SameETagAndJsonString_ShouldEqual()
        {
            const string jsonString = "{}";
            const string etag = "you have not enough minerals";

            var pc1 = new ProjectConfig(jsonString, DateTime.UtcNow, etag);
            var pc2 = new ProjectConfig(jsonString, DateTime.UtcNow, etag);

            Assert.IsTrue(pc1.Equals(pc2));
            Assert.AreEqual(pc1, pc2);
        }

        [TestMethod]
        public void Equatable_DifferentJsonString_ShouldNotEqual()
        {
            var pc1 = new ProjectConfig("pc1", DateTime.UtcNow, "etag1");
            var pc2 = new ProjectConfig("pc2", DateTime.UtcNow, "etag1");
            
            Assert.IsFalse(pc1.Equals(pc2));
            Assert.AreNotEqual(pc1, pc2);
        }

        [TestMethod]
        public void Equatable_DifferentEtag_ShouldNotEqual()
        {
            var pc1 = new ProjectConfig("pc1", DateTime.UtcNow, "etag1");
            var pc2 = new ProjectConfig("pc1", DateTime.UtcNow, "etag2");

            Assert.IsFalse(pc1.Equals(pc2));
            Assert.AreNotEqual(pc1, pc2);           
        }

        [TestMethod]
        public void Equatable_CompareWithNull_ShouldDifferent()
        {
            var pc1 = new ProjectConfig("pc1", DateTime.UtcNow, "etag1");            

            Assert.IsFalse(pc1.Equals(null));            
        }
    }
}


