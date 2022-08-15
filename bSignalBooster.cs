//=============================================================================

public class bSignalBooster : cmk.NMS.Script.ModClass
{
    protected override void Execute()
    {
        RewardTable();
        ScanTable();
        AlienPuzzle();
    }

    private readonly static List<string> eventsToRepeat = new () {
        new("DRONE_HIVE"),
        new("FACTORY_R" )
    };

    private readonly static List<ScanEvent> expectedScanEvents = new ()
    {
        new ( "SE_GRAVE",  BuildingClassEnum.GraveInCave, "UI_MP_PLANTKILL_GRAVE_OSD",   "SCAN_GRAVE",               "SCAN_GRAVE" ),
        new ( "SE_BASE",   BuildingClassEnum.Base,        "UI_RECOVER_BASE_OSD",         "UI_RECOVER_BASE_MARKER",   "UI_RECOVER_BASE_MARKER" ),
        new ( "SE_GLITCH", BuildingClassEnum.StoryGlitch, "NPC_COMM_WEEK_04_GLITCH_OSD", "BUILDING_GLITCHYSTORYBOX", "BUILDING_GLITCHYSTORYBOX" )
    };

    private readonly static List<NameEventPairs> expectedRewards = new()
    {
       // new ( "DISTRESS",  "CRASHED_SHIP" ),
        new ( "SHOP",      "TOOL_LOCATION" ),
        new ( "LIBRARY",   "PLANET_ARCHIVES" ),
        new ( "SE_GRAVE",  "R_GRAVE" ),
        new ( "SE_BASE",   "R_BASE" ),
        new ( "SE_GLITCH", "R_GLITCH" )
    };

   private readonly static List<NameEventPairs> scannerMenuItems = new ()
   {
       new( "UI_PORTAL_OPT", "REVEAL_PORTAL" ),              // Portal
       new( "BUILDING_DISTRESSSIGNAL_L", "CRASHED_SHIP"),    // Crashed Starship
       new( "NPC_TECHSHOP_CATEGORY_WEAP", "TOOL_LOCATION"),  // Multi-tool Location
       new( "?POWERSCANNER", null),                          // Select Next Scanner Options
       new( "BUILDING_FACTORY_L", "SEC_SCN_FACT"),           // Manufacturing Facility
       new( "UI_NAV_DROPPOD_NAME_L", "SCAN_1" ),             // Exosuit DropPod
       new( "UI_LIBRARY_ENTRANCE_DESC", "PLANET_ARCHIVES" ), // Planetary Archives
       new( "?ATOMICSCANNER", null),                         // Select Next Scanner Options
       new( "UI_CORE_ACT2_STEP8_MARKER", "SHOW_CRASH_SITE" ),// Crashed Freighter
       new( "UI_ABAND_EVENT_AREA20", "SEC_SCN_OBS" ),        // Observatory
       new( "NAV_DATA_OPTC", "RANDOM_SCAN_C" ),              // Scan Habitable Outposts
       new( "?NUCLEARSCANNER", null),                        // Select Next Scanner Options
       new( "UI_SENTINEL_HIVE_NAME", "R_SHOW_HIVE_ONLY"),    // Sentinel Pillar
       new( "SCAN_GRAVE", "R_GRAVE"),                        // Traveller Grave
       new( "UI_RECOVER_BASE_SUB", "R_BASE"),                // "Wild" Base computer
       new( "?HYDRO_SCANNER", null ),                        // Select next scanner Options
       new( "BUILDING_GLITCHYSTORYBOX", "R_GLITCH" )         // Settlement
   };

    protected void RewardTable()
    {
        var mbin = ExtractMbin<GcRewardTable>(
          "METADATA/REALITY/TABLES/REWARDTABLE.MBIN"
        );

        foreach (var reward in expectedRewards)
        {
            CreateReward(mbin, reward.Name, reward.Event);
        }
    }

    protected void ScanTable()
    {
        var mbin = ExtractMbin<GcScanEventTable>(
          "METADATA/SIMULATION/SCANNING/SCANEVENTTABLEPLANET.MBIN"
        );

        foreach (var scan in expectedScanEvents)
        {
            var target = mbin.Events.Find(EVENT => EVENT.Name == scan.Name);
            if (target != null) continue;  // no error, already exists

            var source = mbin.Events.Find(EVENT => EVENT.Name == "HARVESTER");
            target = CloneMbin(source);

            target.Name = scan.Name;
            target.BuildingClass.BuildingClass = scan.Class;
            target.OSDMessage = scan.OSD;
            target.MarkerLabel = scan.Label;
            target.TooltipMessage = scan.Tooltip;

            mbin.Events.Add(target);
        };

        RepeatEvents(mbin);
    }

    protected static void RepeatEvents(GcScanEventTable MBIN)
    {
        foreach (var name in eventsToRepeat) {
            var selected = MBIN.Events.Find(EVENT => EVENT.Name == name);
            selected.ReplaceEventIfAlreadyActive = true;
            selected.InterstellarOSDMessage = "SCANEVENT_ANOTHER_SYSTEM";
        }
    }
  protected GcGenericRewardTableEntry CreateReward(
    GcRewardTable MBIN,
    string TARGET_ID,        // "Portal",   add  to LIST
    string TARGET_EVENT     // "PORTAL",   add  to GcScanEventTable
   )
  {
    var clone = CloneMbin(MBIN.InteractionTable.Find(NAME => NAME.Id == "R_DIG_CLUE"));

    clone.Id = TARGET_ID;

    var LIST = clone.List.List;
    var rewardList = LIST.FindFirst<GcRewardTableItem>();
    var rewardItem = rewardList.Reward as GcRewardScanEvent;

    rewardItem.Event = TARGET_EVENT;

    MBIN.InteractionTable.Add(clone);

    clone.List.RewardChoice = RewardChoiceEnum.SelectAlways;
    rewardList.PercentageChance = 100;
    rewardItem.ScanEventTable = GcRewardScanEvent.ScanEventTableEnum.Planet;
    rewardItem.DoAerialScan = false;
    return clone;
  }

    protected void AlienPuzzle()
    {
        var mbin = ExtractMbin<GcAlienPuzzleTable>(
          "METADATA/REALITY/TABLES/NMS_DIALOG_GCALIENPUZZLETABLE.MBIN"
        );

        var scanner = mbin.Table.Find(ENTRY => ENTRY.Id == "SIGNALSCANNER");
        scanner.TextAlien = "";
        scanner.Options.Clear();
        GcAlienPuzzleOption option = null;

    foreach (var MenuItem in scannerMenuItems)
    {
      option = AddScannerOption(MenuItem);
      scanner.Options.AddUnique(option);

      if (MenuItem.Event == null)
      {
        scanner.CustomFreighterTextIndex = -1;

        var NextEntry = new GcAlienPuzzleEntry
        {
          ProgressionIndex = -1,
          MinProgressionForSelection = 0,
          Id = MenuItem.Name,
          Type = new GcInteractionType
          {
            InteractionType = GcInteractionType.InteractionTypeEnum.SignalScanner
          },

          Race = new GcAlienRace { AlienRace = AlienRaceEnum.None },
          Prop = new GcNPCPropTypes { NPCProp = GcNPCPropTypes.NPCPropEnum.DontCare },
          Options = new List<GcAlienPuzzleOption>()
        };

        mbin.Table.AddUnique(NextEntry);
        scanner = mbin.Table.Find(ENTRY => ENTRY.Id == MenuItem.Name);
      }
    }

    option = CreatePuzzleOption("ALL_REQUEST_LEAVE");
    option.Cost = "";

    scanner.Options.AddUnique(option);
    scanner.CustomFreighterTextIndex = -1;
  }

  protected static GcAlienPuzzleOption AddScannerOption(
    NameEventPairs menuItem
  )
  {
    if (menuItem.Event != null)
    {
      var target = CreatePuzzleOption(menuItem.Name);
      target.Rewards = new() { menuItem.Event };
      return (target);
    }
    else
    {
      var target = CreatePuzzleOption("More Options");
      target.NextInteraction = menuItem.Name;
      target.MarkInteractionComplete = false;
      target.KeepOpen = true;
      return (target);
    }
  }

  protected static GcAlienPuzzleOption CreatePuzzleOption(string NAME)
  {
    var option = new GcAlienPuzzleOption
    {
      Name = NAME,
      Cost = "C_ALLOWSCAN",
      DisplayCost = true,
      Prop = new GcNPCPropTypes { NPCProp = GcNPCPropTypes.NPCPropEnum.DontCare },
      MarkInteractionComplete = true,
      KeepOpen = false
    };

    return option;
  }

  //...........................................................

    protected class ScanEvent
    {
        public string Name { get; set; }
        public BuildingClassEnum Class { get; set; }
        public string OSD { get; set; }
        public string Label { get; set; }
        public string Tooltip { get; set; }

        public ScanEvent(string name, BuildingClassEnum CLASS, string oSD, string label, string tooltip)
        {
            Name = name;
            Class = CLASS;
            OSD = oSD;
            Label = label;
            Tooltip = tooltip;
        }
    }

    protected class NameEventPairs
    {
        public string Name { get; set; }
        public string Event { get; set; }

        public NameEventPairs(string NAME, string EVENT)
        {
            Name = NAME;
            Event = EVENT;
        }
    }
}

//=============================================================================
