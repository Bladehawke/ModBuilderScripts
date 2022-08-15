//=============================================================================

public class KeepGifting : cmk.NMS.Script.ModClass
{
  protected int OptionCost = 5;
  protected int OptionReward = 5;
  protected bool RemoveOption = true;
  protected bool KeepOpen = true;

  protected override void Execute()
  {

    GcCostTable();
    GcRewardTable();
    GcAlienPuzzleTable();
  }

  protected void GcCostTable()
  {
    var mbin = ExtractMbin<GcCostTable>(
      "METADATA/REALITY/TABLES/COSTTABLE.MBIN"
    );

    var costChanges = new(string oldID, string newID) [] {
      new("GEKGIFT1", "GEKGIFT_C"),
      new("KORVAXGIFT1", "KORVAXGIFT_C"),
      new("VYKGIFT1", "VYKEENGIFT_C")
    };

    var table = mbin.ItemCostsTable;

    foreach (var change in costChanges) {
      var source = table.Find(NAME => NAME.Id == change.oldID);

      var target = CloneMbin(source);
      target.Id = change.newID;
      target.RemoveOptionIfCantAfford = RemoveOption;
      (target.Cost as GcCostProduct).Amount = OptionCost;

      table.Add(target);

    }


  }

  protected void GcRewardTable()
  {
    var mbin = ExtractMbin<GcRewardTable>(
      "METADATA/REALITY/TABLES/REWARDTABLE.MBIN"
    );

    var rewardChanges = new (string OLD, string NEW) [] {
      new("SEC_STDLOW_TRA", "SEC_CUSTOM_TRA"),
      new("SEC_STDLOW_EXP", "SEC_CUSTOM_EXP"),
      new("SEC_STDLOW_WAR", "SEC_CUSTOM_WAR"),
    };

    var table = mbin.InteractionTable;

    foreach( var change in rewardChanges ) {
      var source = table.Find(NAME => NAME.Id == change.OLD);

      var target = CloneMbin(source);

      target.Id = change.NEW;

      var reward = target.List.List.FindFirst<GcRewardTableItem>().Reward as GcRewardStanding;

      reward.AmountMin = OptionCost;
      reward.AmountMax = OptionCost;

      table.Add(target);
    }
  }

  protected void GcAlienPuzzleTable()
  {
    var mbin = ExtractMbin<GcAlienPuzzleTable>(
      "METADATA/REALITY/TABLES/NMS_DIALOG_GCALIENPUZZLETABLE.MBIN"
    );

    var dialogChanges = new(string ID, string OLD, string NEW, string REWARD) [] {
      new("?TRA_NPC_SECONDARY", "GEKGIFT1", "GEKGIFT_C", "SEC_CUSTOM_TRA"),
      new("TRA_NPC_SECONDARY", "GEKGIFT1", "GEKGIFT_C", "SEC_CUSTOM_TRA"),
      new("EXP_NPC_SECONDARY", "KORVAXGIFT1", "KORVAXGIFT_C", "SEC_CUSTOM_EXP"),
      new("WAR_NPC_SECONDARY", "VYKGIFT1", "VYKEENGIFT_C", "SEC_CUSTOM_WAR")
    };

    var table = mbin.Table;

    foreach( var change in dialogChanges) {
      var entries = table.FindAll(NAME => NAME.Id == change.ID);
      foreach (var entry in entries) {
        addOption(entry, change);
      }
      
      if (change.ID != "?TRA_NPC_SECONDARY") {
        var outlaws = table.FindAll(NAME => NAME.Id == "OUTLAW_SECONDARY");
        foreach(var outlaw in outlaws) {
          addOption(outlaw, change);
        }
      }
    
    }
    
  }

  protected void addOption(GcAlienPuzzleEntry entry, (string ID, string OLD, string NEW, string REWARD) change)
  {
    var option = entry.Options.Find(NAME => NAME.Cost == change.OLD);
    if(option == null)
      return;
    option.KeepOpen = true;
    var target = CloneMbin(option);

    target.Cost = change.NEW;
    target.Rewards[0] = change.REWARD;
    target.KeepOpen = KeepOpen;
    entry.Options.Add(target);

    option = entry.Options.Find(NAME => NAME.Name == "ALL_REQUEST_LEAVE");
    target = CloneMbin(option);
    entry.Options.Remove(option);
    entry.Options.Add(target);
  }
  //...........................................................
}

//=============================================================================
