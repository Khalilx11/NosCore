//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using System.Linq;

namespace NosCore.Parser.Validation
{
    public class DataValidationService(
        IDao<ItemDto, short> itemDao, IDao<SkillDto, short> skillDao, IDao<CardDto, short> cardDao,
        IDao<BCardDto, int> bCardDao, IDao<NpcMonsterDto, short> npcMonsterDao,
        IDao<NpcMonsterSkillDto, long> npcMonsterSkillDao, IDao<DropDto, short> dropDao, IDao<MapDto, short> mapDao,
        IDao<MapMonsterDto, int> mapMonsterDao, IDao<MapNpcDto, int> mapNpcDao, IDao<PortalDto, int> portalDao,
        ILogger<DataValidationService> logger)
    {
        private const int ExamplesPerGroup = 5;

        public void Run()
        {
            var data = new ImportedData(
                itemDao.LoadAll().ToList(), skillDao.LoadAll().ToList(), cardDao.LoadAll().ToList(),
                bCardDao.LoadAll().ToList(), npcMonsterDao.LoadAll().ToList(), npcMonsterSkillDao.LoadAll().ToList(),
                dropDao.LoadAll().ToList(), mapDao.LoadAll().ToList(), mapMonsterDao.LoadAll().ToList(),
                mapNpcDao.LoadAll().ToList(), portalDao.LoadAll().ToList());

            logger.LogInformation(
                "Validating {Items} items, {Skills} skills, {Cards} cards, {BCards} bcards, {Monsters} monsters, {Links} monster skills, " +
                "{Drops} drops, {Maps} maps, {MapMonsters} monster spawns, {MapNpcs} npc spawns, {Portals} portals",
                data.Items.Count, data.Skills.Count, data.Cards.Count, data.BCards.Count, data.NpcMonsters.Count,
                data.NpcMonsterSkills.Count, data.Drops.Count, data.Maps.Count, data.MapMonsters.Count, data.MapNpcs.Count,
                data.Portals.Count);

            var issues = DataValidator.Validate(data);
            if (issues.Count == 0)
            {
                logger.LogInformation("No invalid records found");
                return;
            }

            foreach (var group in issues.GroupBy(i => (i.Table, i.Kind)).OrderBy(g => g.Key.Table).ThenBy(g => g.Key.Kind))
            {
                logger.LogWarning("{Table}: {Count} x {Kind}", group.Key.Table, group.Count(), group.Key.Kind);
                foreach (var issue in group.Take(ExamplesPerGroup))
                {
                    logger.LogWarning("  {Table} {Key}: {Message}", issue.Table, issue.Key, issue.Message);
                }
            }
        }
    }
}
