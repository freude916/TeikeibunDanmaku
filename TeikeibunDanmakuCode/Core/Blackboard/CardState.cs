using MegaCrit.Sts2.Core.Models;

namespace TeikeibunDanmaku.Core.Blackboard;

public class CardState: IBoardState
{
    [DataField] public required string ModelId { get; init; }
    
    [DataField] public required string Title { get; init; }
    
    [DataField] public required bool CostsX { get; init; }
    [DataField] public required int EnergyCost { get; init; }
    
    [DataField] public required int EnergyEarned { get; init; }
    
    [DataField] public required int HpLoss { get; init; }
    
    [DataField] public required int Strength { get; init; }
    
    [DataField] public required int Dexterity { get; init; }
    
    [DataField] public required int Damage { get; init; }
    
    [DataField] public required int Block { get; init; }
    
    [DataField] public required int Repeat { get; init; }
    

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
