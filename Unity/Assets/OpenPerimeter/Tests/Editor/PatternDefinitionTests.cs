using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using OpenPerimeter.Patterns;

namespace OpenPerimeter.Tests
{
    [TestFixture]
    public class PatternDefinitionTests
    {
        [Test]
        public void Generate24_2_Generates54ActivePoints()
        {
            var pointsOD = TestPatternDefinition.Generate24_2(EyeSide.OD);
            Assert.AreEqual(54, pointsOD.Count);
        }

        [Test]
        public void Generate10_2_Generates68Points()
        {
            var pointsOD = TestPatternDefinition.Generate10_2(EyeSide.OD);
            Assert.AreEqual(68, pointsOD.Count);
        }

        [Test]
        public void Generate24_2_MirrorsHorizontalCoordinatesForLeftEye()
        {
            var pointsOD = TestPatternDefinition.Generate24_2(EyeSide.OD);
            var pointsOS = TestPatternDefinition.Generate24_2(EyeSide.OS);

            Assert.AreEqual(pointsOD.Count, pointsOS.Count);
            for (int i = 0; i < pointsOD.Count; i++)
            {
                Assert.AreEqual(-pointsOD[i].xDeg, pointsOS[i].xDeg, 0.001f);
                Assert.AreEqual(pointsOD[i].yDeg, pointsOS[i].yDeg, 0.001f);
            }
        }

        [Test]
        public void GetPhysiologicalBlindSpotCenter_MirrorsCorrectly()
        {
            Vector2 odCenter = TestPatternDefinition.GetPhysiologicalBlindSpotCenter(EyeSide.OD);
            Vector2 osCenter = TestPatternDefinition.GetPhysiologicalBlindSpotCenter(EyeSide.OS);

            Assert.AreEqual(15.0f, odCenter.x, 0.01f);
            Assert.AreEqual(-15.0f, osCenter.x, 0.01f);
            Assert.AreEqual(odCenter.y, osCenter.y, 0.01f);
        }
    }
}
