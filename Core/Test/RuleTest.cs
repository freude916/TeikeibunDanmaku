using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.Timepoints;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.Core.Test;

public static class RuleTest
{
    public static void TestCardRules()
    {
        var rules = new List<Rule>
        {
            CardRuleFactory.CreateRewardSeenRule(ModelDb.GetId<PerfectedStrike>().Entry, ["零秒猜出二层先古之民会给什么遗物"]),
            CardRuleFactory.CreateRewardSeenRule(ModelDb.GetId<MoltenFist>().Entry, ["cos突破极限有没有懂的"]),
            CardRuleFactory.CreateRewardSeenRule(ModelDb.GetId<StoneArmor>().Entry, ["不拿散装护喉甲？"]),
            CardRuleFactory.CreateSpecialCardRule(ConditionType.Eq,"CostsX", true, ["别带我们112345节奏"]),
            CardRuleFactory.CreateSpecialCardRule(ConditionType.Gt, "Damage", 10, ["我说1费打${Damage}特别强"]),
        };

        RuleRuntime.ConfigureRules(rules);
        RuleRuntime.ExportConfiguredRulesToDefaultFile("rules.test.export.jsonc");

        var rulesDirectory = Path.Combine(ModPathResolver.ResolveModDirectory(), "rules");
        var importedRules = RuleJsoncIo.ImportFromDirectory(rulesDirectory, new TimepointStateResolver());
        RuleRuntime.ConfigureRules(importedRules);
    }
}
