//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.SkillService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillRequirementsTests
    {
        private static SkillDto Skill(short vnum, byte skillClass, byte levelMinimum = 0) =>
            new() { SkillVNum = vnum, Class = skillClass, LevelMinimum = levelMinimum };

        [DataTestMethod]
        [DataRow(200)]
        [DataRow(209)]
        [DataRow(210)]
        public void AnAdventurerHoldsItsListedSkills(int vnum)
        {
            Assert.IsTrue(SkillRequirements.IsLearnableBy(Skill((short)vnum, 0), CharacterClassType.Adventurer));
        }

        [DataTestMethod]
        [DataRow(211)]
        [DataRow(212)]
        [DataRow(300)]
        [DataRow(1000)]
        public void AnAdventurerDoesNotHoldTheRestOfClassZero(int vnum)
        {
            Assert.IsFalse(SkillRequirements.IsLearnableBy(Skill((short)vnum, 0), CharacterClassType.Adventurer));
        }

        [TestMethod]
        public void OtherClassesHoldTheSkillsOfTheirOwnClassOnly()
        {
            var archerSkill = Skill(400, (byte)CharacterClassType.Archer);

            Assert.IsTrue(SkillRequirements.IsLearnableBy(archerSkill, CharacterClassType.Archer));
            Assert.IsFalse(SkillRequirements.IsLearnableBy(archerSkill, CharacterClassType.Mage));
            Assert.IsFalse(SkillRequirements.IsLearnableBy(archerSkill, CharacterClassType.Swordsman));
        }

        [TestMethod]
        public void ASkillIsUnlockedFromItsJobLevel()
        {
            var skill = Skill(400, 2, levelMinimum: 20);

            Assert.IsFalse(SkillRequirements.IsUnlockedAt(skill, 19));
            Assert.IsTrue(SkillRequirements.IsUnlockedAt(skill, 20));
            Assert.IsTrue(SkillRequirements.IsUnlockedAt(skill, 80));
        }

        [TestMethod]
        public void LearningNeedsBothTheClassAndTheJobLevel()
        {
            var skill = Skill(400, (byte)CharacterClassType.Mage, levelMinimum: 10);

            Assert.IsTrue(SkillRequirements.CanLearn(skill, CharacterClassType.Mage, 10));
            Assert.IsFalse(SkillRequirements.CanLearn(skill, CharacterClassType.Mage, 9));
            Assert.IsFalse(SkillRequirements.CanLearn(skill, CharacterClassType.Archer, 80));
        }

        [DataTestMethod]
        [DataRow(31, false)]
        [DataRow(32, true)]
        [DataRow(84, true)]
        public void SpecialistSkillsStartAtClassThirtyTwo(int skillClass, bool specialist)
        {
            Assert.AreEqual(specialist, SkillRequirements.IsSpecialistSkill(Skill(800, (byte)skillClass)));
        }
    }
}
