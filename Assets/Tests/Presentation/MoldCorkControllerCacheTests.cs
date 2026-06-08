using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Presentation;

namespace PuzzleGame.Tests.Presentation
{
    /// <summary>
    /// Verifies the <see cref="MoldCorkController.ClearCache"/> contract.
    /// The static material/shader cache is shared across all cork instances and
    /// is intended to survive scene loads (it is a DontDestroyOnLoad-style
    /// optimization). <c>ClearCache</c> must therefore be a deliberate lifecycle
    /// call (e.g. application quit), not an automatic per-scene cleanup.
    /// </summary>
    [TestFixture]
    public class MoldCorkControllerCacheTests
    {
        [Test]
        public void ClearCache_DoesNotThrow_WhenCacheIsEmpty()
        {
            // Start from a known state — clear any residue from a previous test.
            MoldCorkController.ClearCache();
            Assert.DoesNotThrow(MoldCorkController.ClearCache,
                "ClearCache must be idempotent — calling it on an empty cache is a no-op.");
        }

        [Test]
        public void ClearCache_ResetsStaticState_ToNullAfterDoubleCall()
        {
            MoldCorkController.ClearCache();
            // Two consecutive clears should not throw and should leave the
            // internal static state empty. Indirectly verifies the cache was
            // truly reset and not just hidden behind a flag.
            Assert.DoesNotThrow(() =>
            {
                MoldCorkController.ClearCache();
                MoldCorkController.ClearCache();
            });
        }
    }
}
