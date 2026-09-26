//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Networking;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillResolverTests
    {
        private INpcCombatCatalog _catalog = null!;
        private IReadOnlyDictionary<short, SkillDto> _skills = null!;

        [TestInitialize]
        public void Setup()
        {
            _skills = new Dictionary<short, SkillDto>
            {
                [100] = new SkillDto { SkillVNum = 100, CastId = 1, UpgradeSkill = 0, Cooldown = 50, HitType = (byte)TargetHitType.SingleTargetHit },
                [200] = new SkillDto { SkillVNum = 200, CastId = 2, UpgradeSkill = 0, Cooldown = 50, HitType = (byte)TargetHitType.AoeTargetHit },
            };
            _catalog = new NpcCombatCatalog(new(), new(), new());
        }

        [TestMethod]
        public void UnknownCasterReturnsNull()
        {
            var resolver = new SkillResolver(_skills, _catalog);
            Assert.IsNull(resolver.Resolve(new Mock<IAliveEntity>().Object, 1));
        }

        [TestMethod]
        public void CharacterResolvesByCastId()
        {
            var character = new Mock<ICharacterEntity>();
            var skills = new ConcurrentDictionary<short, CharacterSkill>();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });
            skills.TryAdd(200, new CharacterSkill { SkillVNum = 200, Skill = _skills[200] });
            character.SetupGet(c => c.Skills).Returns(skills);

            var resolver = new SkillResolver(_skills, _catalog);
            var resolved = resolver.Resolve(character.Object, 2);

            Assert.IsNotNull(resolved);
            Assert.AreEqual((short)200, resolved!.SkillVnum);
            Assert.AreEqual(TargetHitType.AoeTargetHit, resolved.HitType);
        }

        [TestMethod]
        public void CharacterWithoutMatchingCastIdReturnsNull()
        {
            var character = new Mock<ICharacterEntity>();
            var skills = new ConcurrentDictionary<short, CharacterSkill>();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });
            character.SetupGet(c => c.Skills).Returns(skills);

            var resolver = new SkillResolver(_skills, _catalog);
            Assert.IsNull(resolver.Resolve(character.Object, 999));
        }

        private SkillResolver ResolverForAMonsterWith(NpcMonsterDto mob, params short[] linkedSkills)
        {
            var links = new List<NpcMonsterSkillDto>();
            foreach (var skill in linkedSkills)
            {
                links.Add(new NpcMonsterSkillDto { NpcMonsterVNum = mob.NpcMonsterVNum, SkillVNum = skill });
            }

            return new SkillResolver(_skills, new NpcCombatCatalog(links, new(), new()));
        }

        private static INonPlayableEntity Monster(NpcMonsterDto mob)
        {
            var monster = new Mock<INonPlayableEntity>();
            monster.SetupGet(m => m.NpcMonster).Returns(mob);
            return monster.Object;
        }

        [TestMethod]
        public void AMonstersBasicAttackIsBuiltFromItsOwnStatsNotFromItsFirstSkill()
        {
            var mob = new NpcMonsterDto { NpcMonsterVNum = 7, BasicRange = 3, BasicArea = 1, BasicCooldown = 12, AttackClass = 1 };
            var resolver = ResolverForAMonsterWith(mob, 100, 200);

            var basic = resolver.Resolve(Monster(mob), 0)!;

            Assert.AreEqual((short)0, basic.SkillVnum);
            Assert.AreEqual((byte)3, basic.Range);
            Assert.AreEqual((byte)1, basic.TargetRange);
            Assert.AreEqual((short)12, basic.Cooldown);
            Assert.AreEqual((byte)1, basic.Type);
        }

        [TestMethod]
        public void AMonsterSkillIsFoundByItsVnum()
        {
            var mob = new NpcMonsterDto { NpcMonsterVNum = 7 };
            var resolver = ResolverForAMonsterWith(mob, 100, 200);

            Assert.AreEqual((short)200, resolver.Resolve(Monster(mob), 200)!.SkillVnum);
            Assert.AreEqual(TargetHitType.AoeTargetHit, resolver.Resolve(Monster(mob), 200)!.HitType);
        }

        [TestMethod]
        public void AMonsterCannotUseASkillItDoesNotKnow()
        {
            var mob = new NpcMonsterDto { NpcMonsterVNum = 7 };
            var resolver = ResolverForAMonsterWith(mob, 100);

            Assert.IsNull(resolver.Resolve(Monster(mob), 200));
        }

        [TestMethod]
        public void ABasicSkillColumnThatHappensToNameASkillIsNotUsedAsOne()
        {
            var mob = new NpcMonsterDto { NpcMonsterVNum = 7, BasicSkill = 200, BasicRange = 2 };
            var resolver = ResolverForAMonsterWith(mob);

            var basic = resolver.Resolve(Monster(mob), 0)!;

            Assert.AreEqual((short)0, basic.SkillVnum);
            Assert.AreEqual((byte)2, basic.Range);
        }
    }
}
