//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.SkillService
{
    // Who may hold a skill and from which job level. Skill classes 0-4 are the five character classes
    // (the Adventurer's own skills are listed below: class 0 is also where emotes, courses and passives
    // sit), 32 and above are specialist cards' skills, everything between belongs to monsters.
    public static class SkillRequirements
    {
        public const byte FirstSpecialistClass = 32;

        // 200 to 210 are the Adventurer's, 209 being the pet catcher every new character is given. 211 and
        // 212 are named as cheats in the data file. 300-306 look like upgraded forms of 200-206 with the
        // same cast ids, and nothing says how one replaces the other, so they stay out.
        private static readonly short[] AdventurerSkills = { 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210 };

        public static bool IsSpecialistSkill(SkillDto skill) => skill.Class >= FirstSpecialistClass;

        public static bool IsLearnableBy(SkillDto skill, CharacterClassType characterClass) =>
            characterClass == CharacterClassType.Adventurer
                ? AdventurerSkills.Contains(skill.SkillVNum)
                : skill.Class == (byte)characterClass;

        public static bool IsUnlockedAt(SkillDto skill, byte jobLevel) => skill.LevelMinimum <= jobLevel;

        public static bool CanLearn(SkillDto skill, CharacterClassType characterClass, byte jobLevel) =>
            IsLearnableBy(skill, characterClass) && IsUnlockedAt(skill, jobLevel);
    }
}
