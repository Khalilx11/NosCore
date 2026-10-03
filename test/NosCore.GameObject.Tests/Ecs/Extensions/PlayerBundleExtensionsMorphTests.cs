//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Core.I18N;
using NosCore.Tests.Shared;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    // M33.6: GenerateIn/GenerateCInfo used to hard-code Morph/MorphUpgrade to 0 regardless of the
    // character's own real transformation state (TransformationService.ChangeSpAsync/ChangeVehicleAsync set
    // character.Morph/MorphUpgrade, already read correctly by GenerateCMode - these two methods were the
    // only ones that never read it back). A player who was already transformed before another player's
    // client first observed them via "in" rendered as untransformed until the next live "cmode" broadcast.
    [TestClass]
    public class PlayerBundleExtensionsMorphTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public void NormalCharacterStillReportsZeroMorphOnIn()
        {
            Assert.AreEqual(0, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.Morph);
            Assert.AreEqual(0, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.MorphUpgrade);
        }

        [TestMethod]
        public void TransformedCharacterReportsTheRealMorphOnIn()
        {
            // The exact assignment TransformationService.ChangeSpAsync performs (character.Morph =
            // sp.Item.Morph; character.MorphUpgrade = (byte)sp.Upgrade;) - this test does not call
            // TransformationService itself (that is a separate, already-covered path); it asserts that
            // whatever real value ends up on the character is the one GenerateIn now reports, which is
            // exactly the fix's own contract.
            _session.Character.Morph = 5;
            _session.Character.MorphUpgrade = 3;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!;
            Assert.AreEqual(5, sub.Morph);
            Assert.AreEqual(3, sub.MorphUpgrade);
        }

        [TestMethod]
        public void DeactivatedTransformationReportsZeroOnIn()
        {
            _session.Character.Morph = 5;
            _session.Character.MorphUpgrade = 3;

            // RemoveSpAsync's own exact assignment (character.Morph = 0; character.MorphUpgrade stays
            // untouched by RemoveSpAsync itself, but MorphDesign is zeroed - Morph alone is what GenerateIn
            // reports here).
            _session.Character.Morph = 0;

            Assert.AreEqual(0, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.Morph);
        }

        [TestMethod]
        public void ANegativeMorphClampsToZeroRatherThanWrapping()
        {
            // A negative Morph is a real, observed value elsewhere in Item.dat (M33.1.3's own forensics, on
            // an unrelated Fashion row) - this proves the server-side (byte) narrowing clamps rather than
            // wrapping a negative short into an unrelated, misleadingly valid positive byte.
            _session.Character.Morph = -1;

            Assert.AreEqual(0, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.Morph);
        }

        [TestMethod]
        public void AnOutOfRangeMorphClampsToTheByteMaximumRatherThanWrapping()
        {
            _session.Character.Morph = 1000;

            Assert.AreEqual(255, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.Morph);
        }

        [TestMethod]
        public void TransformedCharacterReportsTheRealMorphOnCInfo()
        {
            _session.Character.Morph = 7;
            _session.Character.MorphUpgrade = 2;

            var localizer = new Mock<IGameLanguageLocalizer>().Object;
            var packet = _session.Character.GenerateCInfo(localizer);

            Assert.AreEqual(7, packet.Morph);
            Assert.AreEqual(2, packet.MorphUpgrade);
        }

        [TestMethod]
        public void NormalCharacterStillReportsZeroMorphOnCInfo()
        {
            var localizer = new Mock<IGameLanguageLocalizer>().Object;
            var packet = _session.Character.GenerateCInfo(localizer);

            Assert.AreEqual(0, packet.Morph);
            Assert.AreEqual(0, packet.MorphUpgrade);
        }
    }
}
