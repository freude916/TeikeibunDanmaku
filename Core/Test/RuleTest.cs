using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using TeikeibunDanmaku.Core.Rules;

namespace TeikeibunDanmaku.Core.Test;

public static class RuleTest
{
    public static void TestCardRules()
    {

        var rules = new List<Rule>();
        rules.Add(CardRuleFactory.CreateRewardSeenRule(ModelDb.Card<PerfectedStrike>(), ["零秒猜出二层先古之民会给什么遗物"]));
        rules.Add(CardRuleFactory.CreateRewardSeenRule(ModelDb.Card<MoltenFist>(), ["cos突破极限有没有懂的"]));
        rules.Add(CardRuleFactory.CreateRewardSeenRule(ModelDb.Card<StoneArmor>(), ["不拿散装护喉甲？"]));
        
        RuleRuntime.ConfigureRules(rules);
    }
}
