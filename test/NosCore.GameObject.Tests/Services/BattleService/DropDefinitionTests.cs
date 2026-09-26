//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Chances in the data are hundred-thousandths: five values on three monsters read 14%, 15%, 1.75%,
    // 50% and 0.6% for 14000, 15000, 1750, 50000 and 600.
    [TestClass]
    public class DropDefinitionTests
    {
        [DataTestMethod]
        [DataRow(14000, 0.14)]
        [DataRow(15000, 0.15)]
        [DataRow(1750, 0.0175)]
        [DataRow(50000, 0.50)]
        [DataRow(600, 0.006)]
        public void AChanceIsItsShareOfOneHundredThousand(int chance, double expected)
        {
            var landing = 0;
            var drop = new DropDefinition(1, 1, chance);

            for (var roll = 0; roll < DropDefinition.ChanceScale; roll++)
            {
                if (drop.Lands(roll, 1))
                {
                    landing++;
                }
            }

            Assert.AreEqual(expected, landing / (double)DropDefinition.ChanceScale, 1e-9);
        }

        [TestMethod]
        public void TheRollJustBelowTheChanceLandsAndTheOneAtItDoesNot()
        {
            var drop = new DropDefinition(1, 1, 14000);

            Assert.IsTrue(drop.Lands(13999, 1));
            Assert.IsFalse(drop.Lands(14000, 1));
        }

        [TestMethod]
        public void TheMapsDropRateMultipliesTheChance()
        {
            var drop = new DropDefinition(1, 1, 1000);

            Assert.IsFalse(drop.Lands(1000, 1));
            Assert.IsTrue(drop.Lands(1999, 2));
            Assert.IsFalse(drop.Lands(2000, 2));
        }

        [TestMethod]
        public void AChanceOfTheWholeScaleAlwaysLands()
        {
            var drop = new DropDefinition(1, 1, DropDefinition.ChanceScale);

            Assert.IsTrue(drop.Lands(DropDefinition.ChanceScale - 1, 1));
        }

        [TestMethod]
        public void NoChanceNeverLands()
        {
            Assert.IsFalse(new DropDefinition(1, 1, 0).Lands(0, 5));
        }

        [TestMethod]
        public void ADefinitionIsBuiltFromTheDataRow()
        {
            var drop = DropDefinition.From(new DropDto { VNum = 2002, Amount = 1, DropChance = 14000 });

            Assert.AreEqual(new DropDefinition(2002, 1, 14000), drop);
        }
    }
}
