using NUnit.Framework;
using Stopka;

namespace Stopka.Tests
{
    public class BlockSlicerTests
    {
        private const float Tolerance = 0.1f;

        [Test]
        public void PerfectPlacement_ReturnsIsPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 0.05f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsTrue(result.IsPerfect);
            Assert.AreEqual(0f, result.NewCenter, 0.001f, "Should snap to prev center");
            Assert.AreEqual(3f, result.NewSize, 0.001f, "Should keep current size");
        }

        [Test]
        public void ExactAlignment_IsPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 0f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.IsPerfect);
        }

        [Test]
        public void OverhangPositiveSide_SlicesCorrectly()
        {
            // prev: [-1.5, 1.5], curr: [-0.5, 2.5] → overlap [-0.5, 1.5] size=2, cut [1.5, 2.5] size=1
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 1f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(0.5f, result.NewCenter, 0.001f);
            Assert.AreEqual(2f, result.NewSize, 0.001f);
            Assert.AreEqual(2f, result.CutCenter, 0.001f);
            Assert.AreEqual(1f, result.CutSize, 0.001f);
        }

        [Test]
        public void OverhangNegativeSide_SlicesCorrectly()
        {
            // prev: [-1.5, 1.5], curr: [-2.5, 0.5] → overlap [-1.5, 0.5] size=2, cut [-2.5, -1.5] size=1
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: -1f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(-0.5f, result.NewCenter, 0.001f);
            Assert.AreEqual(2f, result.NewSize, 0.001f);
            Assert.AreEqual(-2f, result.CutCenter, 0.001f);
            Assert.AreEqual(1f, result.CutSize, 0.001f);
        }

        [Test]
        public void NoOverlap_ReturnsNoOverlap()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 2f,
                currCenter: 5f, currSize: 2f,
                perfectTolerance: Tolerance);

            Assert.IsFalse(result.HasOverlap);
        }

        [Test]
        public void BarelyOverlapping_NotPerfect()
        {
            // prev: [-1, 1], curr: [0.5, 2.5] → overlap [0.5, 1.0] size=0.5
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 2f,
                currCenter: 1.5f, currSize: 2f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(0.75f, result.NewCenter, 0.001f);
            Assert.AreEqual(0.5f, result.NewSize, 0.001f);
        }

        [Test]
        public void SmallerCurrentBlock_OverhangSliced()
        {
            // prev: [-1.5, 1.5], curr: [0, 2] (size 2, offset 1) → overlap [0, 1.5] size=1.5, cut [1.5, 2] size=0.5
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: 1f, currSize: 2f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.HasOverlap);
            Assert.IsFalse(result.IsPerfect);
            Assert.AreEqual(0.75f, result.NewCenter, 0.001f);
            Assert.AreEqual(1.5f, result.NewSize, 0.001f);
            Assert.AreEqual(1.75f, result.CutCenter, 0.001f);
            Assert.AreEqual(0.5f, result.CutSize, 0.001f);
        }

        [Test]
        public void AtExactTolerance_IsPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: Tolerance, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsTrue(result.IsPerfect);
        }

        [Test]
        public void JustBeyondTolerance_IsNotPerfect()
        {
            var result = BlockSlicer.Slice(
                prevCenter: 0f, prevSize: 3f,
                currCenter: Tolerance + 0.01f, currSize: 3f,
                perfectTolerance: Tolerance);

            Assert.IsFalse(result.IsPerfect);
        }
    }
}
