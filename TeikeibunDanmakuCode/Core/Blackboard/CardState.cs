using MegaCrit.Sts2.Core.Models;

namespace TeikeibunDanmaku.Core.Blackboard;

public class CardState: IBoardState
{
    [DataField("卡牌ID")] public required string ModelId { get; init; }
    
    [DataField("卡牌名")] public required string Title { get; init; }
    
    [DataField("X费")] public required bool CostsX { get; init; }
    [DataField("能量消耗")] public required int EnergyCost { get; init; }
    
    [DataField("获得能量")] public required int EnergyEarned { get; init; }
    
    [DataField("生命损失")] public required int HpLoss { get; init; }
    
    [DataField("力量")] public required int Strength { get; init; }
    
    [DataField("敏捷")] public required int Dexterity { get; init; }
    
    [DataField("伤害")] public required int Damage { get; init; }
    
    [DataField("格挡")] public required int Block { get; init; }
    
    [DataField("多段")] public required int Repeat { get; init; }
    

    public static CardState FromCardModel(CardModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        
        return new CardState()
        {
            ModelId = model.Id.Entry,
            Title = model.TitleLocString.GetFormattedText(),
            CostsX = model.EnergyCost.CostsX,
            EnergyCost = model.EnergyCost.GetAmountToSpend(),
            EnergyEarned = model.DynamicVars.ContainsKey("Energy") ? model.DynamicVars.Energy.IntValue : 0,
            HpLoss = model.DynamicVars.ContainsKey("HpLoss") ? model.DynamicVars.HpLoss.IntValue : 0,
            Strength = model.DynamicVars.ContainsKey("Strength") ? model.DynamicVars.Strength.IntValue : 0,
            Dexterity = model.DynamicVars.ContainsKey("Dexterity") ? model.DynamicVars.Dexterity.IntValue : 0,
            Damage = model.DynamicVars.ContainsKey("Damage") ? model.DynamicVars.Damage.IntValue : 0,
            Block = model.DynamicVars.ContainsKey("Block") ? model.DynamicVars.Block.IntValue : 0,
            Repeat = model.DynamicVars.ContainsKey("Repeat") ? model.DynamicVars.Repeat.IntValue : 1,
        };
    }
}
