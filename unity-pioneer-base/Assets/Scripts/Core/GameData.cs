// Pioneer Base - GameData.cs
// Port of js/data.js. Same ids, same values, same tags. Real values never change for balance.
using System;
using System.Collections.Generic;

namespace PioneerBase
{
    public class DataRow
    {
        public string id, group, label, unit, tag;
        public double value;
        public DataRow(string id, string group, string label, double value, string unit, string tag)
        { this.id = id; this.group = group; this.label = label; this.value = value; this.unit = unit; this.tag = tag; }
    }

    public class StormDef { public int day, alert; public string real; }
    public class DustDef { public int start, end, alert; }
    public class Scenario
    {
        public string name, title;
        public int nightStart, nightEnd;
        public List<StormDef> storms = new List<StormDef>();
        public DustDef dust;
    }
    public class Card { public string id, text, why, source, url; public int afterDay; public bool myth; }

    public static class GameData
    {
        public const string SourcesNote = "An independent student project for the NASA Space Apps Challenge 2026. Not affiliated with or endorsed by NASA.";

        public static readonly List<DataRow> Rows = new List<DataRow>
        {
            // Real
            new DataRow("sun_earth","Real values","Sunlight at Earth's distance",1361,"W/m2","Real"),
            new DataRow("mars_au","Real values","Mars distance from the Sun",1.524,"AU","Real"),
            new DataRow("lunar_night","Real values","Length of the lunar night",15,"turns (days)","Real"),
            new DataRow("dose_mars","Real values","Radiation on the Mars surface",0.67,"mSv/day","Real"),
            new DataRow("dose_moon","Radiation","Radiation on the Moon surface",1.37,"mSv/day","Simplified"),
            new DataRow("o2_person","Real values","Oxygen one astronaut breathes",0.84,"kg/day","Real"),
            new DataRow("food_person","Real values","Food one astronaut needs",1.8,"kg/day","Real"),
            new DataRow("iss_water","Real values","Water recovered on the ISS",98,"%","Real"),
            new DataRow("career_limit","Real values","NASA career radiation limit",600,"mSv","Real"),
            // Derived
            new DataRow("comms_moon","Derived values","Radio delay, Earth to Moon",1.3,"seconds","Derived"),
            new DataRow("comms_mars","Derived values","Radio delay, Earth to Mars (longest)",22,"minutes","Derived"),
            new DataRow("mars_sun_ratio","Derived values","Sunlight at Mars compared with the Moon",0.43,"fraction","Derived"),
            new DataRow("o2p_water_moon","Derived values","Water for 4 kg of oxygen (Moon plant)",4.5,"L","Derived"),
            new DataRow("crew_water","Derived values","Water the crew uses (before recycling)",12,"L/day","Derived"),
            // Simplified: stocks
            new DataRow("o2_start","Starting stocks","Oxygen at the start",20,"kg","Simplified"),
            new DataRow("o2_cap","Starting stocks","Oxygen tank size",40,"kg","Simplified"),
            new DataRow("water_start","Starting stocks","Water at the start",60,"L","Simplified"),
            new DataRow("water_cap","Starting stocks","Water tank size",120,"L","Simplified"),
            new DataRow("food_start","Starting stocks","Food at the start",150,"kg","Simplified"),
            new DataRow("food_cap","Starting stocks","Food store size",250,"kg","Simplified"),
            new DataRow("crates_start","Starting stocks","Supply crates",14,"crates","Simplified"),
            new DataRow("battery_start","Starting stocks","Battery charge at the start",5,"energy","Simplified"),
            new DataRow("health_start","Starting stocks","Crew health at the start",100,"points","Simplified"),
            // Power
            new DataRow("solar_moon","Power","Solar array output on the Moon",7,"energy/day","Simplified"),
            new DataRow("solar_mars","Power","Solar array output on Mars",3,"energy/day","Simplified"),
            new DataRow("solar_cost","Power","Solar array cost",2,"crates","Simplified"),
            new DataRow("solar_max","Power","Most solar arrays",6,"arrays","Simplified"),
            new DataRow("battery_per","Power","Battery size",10,"energy","Simplified"),
            new DataRow("battery_cost","Power","Battery cost",2,"crates","Simplified"),
            new DataRow("battery_max","Power","Most batteries",4,"batteries","Simplified"),
            new DataRow("reactor_out","Power","Fission reactor output",8,"energy/day","Simplified"),
            new DataRow("reactor_cost","Power","Fission reactor cost",5,"crates","Simplified"),
            new DataRow("dust_loss","Power","Mars dust loss",0.03,"per day","Simplified"),
            new DataRow("dust_floor","Power","Lowest dust factor",0.4,"fraction","Simplified"),
            new DataRow("dust_storm_mult","Power","Dust storm effect on solar",0.15,"fraction","Simplified"),
            // Systems
            new DataRow("hab_power","Systems","Habitat core power",4,"energy/day","Simplified"),
            new DataRow("hab_recycle","Systems","Habitat water recycling",0.9,"fraction","Simplified"),
            new DataRow("o2p_power_moon","Systems","Oxygen plant power (Moon)",4,"energy/day","Simplified"),
            new DataRow("o2p_power_mars","Systems","Oxygen plant power (Mars)",6,"energy/day","Simplified"),
            new DataRow("o2p_out","Systems","Oxygen plant output",4,"kg/day","Simplified"),
            new DataRow("lab_power","Systems","Science lab power",3,"energy/day","Simplified"),
            new DataRow("lab_science","Systems","Science per day",4,"points","Simplified"),
            new DataRow("gh_cost","Systems","Greenhouse cost",3,"crates","Simplified"),
            new DataRow("gh_max","Systems","Most greenhouses",3,"greenhouses","Simplified"),
            new DataRow("gh_power","Systems","Greenhouse power",3,"energy/day","Simplified"),
            new DataRow("gh_water","Systems","Greenhouse water",2,"L/day","Simplified"),
            new DataRow("gh_cycle","Systems","Crop growing time",10,"days","Simplified"),
            new DataRow("gh_yield","Systems","Harvest size",20,"kg","Simplified"),
            new DataRow("gh_o2","Systems","Greenhouse oxygen",0.3,"kg/day","Simplified"),
            new DataRow("drill_cost","Systems","Ice drill cost",2,"crates","Simplified"),
            new DataRow("drill_max","Systems","Most ice drills",3,"drills","Simplified"),
            new DataRow("drill_power","Systems","Ice drill power",3,"energy/day","Simplified"),
            new DataRow("drill_out","Systems","Ice drill water",6,"L/day","Simplified"),
            // Radiation
            new DataRow("shield_cost","Radiation","Shield layer cost",2,"crates","Simplified"),
            new DataRow("shield_max","Radiation","Most shield layers",3,"layers","Simplified"),
            new DataRow("shield_factor","Radiation","Shield layer effect",0.75,"per layer","Simplified"),
            new DataRow("storm_moon","Radiation","Solar storm dose on the Moon",12,"mSv","Simplified"),
            new DataRow("storm_mars","Radiation","Solar storm dose on Mars",4,"mSv","Simplified"),
            new DataRow("shelter_factor","Radiation","Shelter effect",0.1,"fraction","Simplified"),
            new DataRow("dose_limit","Radiation","Mission dose limit",40,"mSv","Simplified"),
            // Crew
            new DataRow("ration_full","Crew","Full rations: health",2,"per day","Simplified"),
            new DataRow("ration_lean","Crew","Lean rations (75% food): health",-1,"per day","Simplified"),
            new DataRow("ration_half","Crew","Half rations (50% food): health",-3,"per day","Simplified"),
            new DataRow("lean_factor","Crew","Lean ration size",0.75,"fraction","Simplified"),
            new DataRow("half_factor","Crew","Half ration size",0.5,"fraction","Simplified"),
            new DataRow("pen_o2","Crew","No oxygen: health",-30,"per day","Simplified"),
            new DataRow("pen_hab","Crew","Habitat core off: health",-15,"per day","Simplified"),
            new DataRow("pen_water","Crew","No water: health",-15,"per day","Simplified"),
            new DataRow("pen_food","Crew","No food: health",-10,"per day","Simplified"),
            // Tier 2
            new DataRow("repair_crates","Faults and emergency","Repair cost",1,"crate","Simplified"),
            new DataRow("repair_days","Faults and emergency","Repair without crates",3,"Maintenance days","Simplified"),
            new DataRow("emerg_water","Faults and emergency","Rocket fuel to water",40,"L","Simplified"),
            new DataRow("emerg_risk","Faults and emergency","Emergency accident chance",0.2,"fraction","Simplified"),
            // Stars
            new DataRow("star2_science","Stars","Two stars: science",30,"points","Simplified"),
            new DataRow("star3_science","Stars","Three stars: science",45,"points","Simplified"),
            new DataRow("star3_health","Stars","Three stars: health",70,"points","Simplified"),
            new DataRow("star3_dose_moon","Stars","Three stars: dose (Moon)",35,"mSv","Simplified"),
            new DataRow("star3_dose_mars","Stars","Three stars: dose (Mars)",22,"mSv","Simplified"),
        };

        static readonly Dictionary<string, DataRow> byId = Build();
        static Dictionary<string, DataRow> Build()
        {
            var d = new Dictionary<string, DataRow>();
            foreach (var r in Rows) d[r.id] = r;
            return d;
        }

        public static double V(string id)
        {
            DataRow r;
            if (!byId.TryGetValue(id, out r)) throw new Exception("Unknown data id: " + id);
            return r.value;
        }
        public static string Tag(string id) { DataRow r; return byId.TryGetValue(id, out r) ? r.tag : "Simplified"; }

        public static readonly Dictionary<string, Scenario> Scenarios = new Dictionary<string, Scenario>
        {
            { "moon", new Scenario { name = "Moon", title = "Lunar outpost", nightStart = 11, nightEnd = 25,
                storms = new List<StormDef> {
                    new StormDef { day = 7, alert = 1, real = "11 May 2024" },
                    new StormDef { day = 20, alert = 1, real = "3 Oct 2024" } },
                dust = null } },
            { "mars", new Scenario { name = "Mars", title = "Martian outpost", nightStart = 0, nightEnd = 0,
                storms = new List<StormDef> { new StormDef { day = 23, alert = 1, real = "3 Oct 2024" } },
                dust = new DustDef { start = 12, end = 17, alert = 2 } } }
        };

        const string MarsFs = "https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html";
        public static readonly List<Card> Cards = new List<Card>
        {
            new Card { id="c1", afterDay=5, myth=true, text="A Martian dust storm has winds strong enough to flip over a rocket.",
                why="Myth! Mars air is under 1% as thick as Earth's, so even fast wind pushes very gently. The real danger is the fine dust: it blocks sunlight and covers solar panels.",
                source="NASA Mars Fact Sheet (surface pressure about 6 mb, Earth about 1013 mb)", url=MarsFs },
            new Card { id="c2", afterDay=9, myth=false, text="Dust on solar panels can end a Mars mission.",
                why="Reality! NASA's InSight lander ended its mission in 2022 when dust left its solar panels without enough power.",
                source="NASA, InSight mission retired", url="https://www.nasa.gov/missions/insight/nasa-retires-insight-mars-lander-mission-after-years-of-science/" },
            new Card { id="c3", afterDay=14, myth=false, text="The space station recycles almost all of its water.",
                why="Reality! NASA reported in 2023 that the station can recover about 98% of its water, even from urine and sweat.",
                source="NASA, water recovery milestone", url="https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/" },
            new Card { id="c4", afterDay=19, myth=false, text="You can make oxygen from the air on Mars.",
                why="Reality! A tool called MOXIE on the Perseverance rover made oxygen from Martian carbon dioxide. It made a small amount, as a test.",
                source="NASA MOXIE", url="https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/" },
            new Card { id="c5", afterDay=23, myth=true, text="The Moon has air that blocks space radiation, like Earth does.",
                why="Myth! NASA explains that the Moon has almost no air (only a very thin exosphere), so space radiation reaches the surface. That is why shielding matters.",
                source="NASA Moon Facts", url="https://science.nasa.gov/moon/facts/" },
            new Card { id="c6", afterDay=27, myth=true, text="A night on the Moon lasts only a few hours.",
                why="Myth! One lunar night lasts about 14.8 Earth days. Solar panels make no power the whole time.",
                source="NASA Moon Facts", url="https://science.nasa.gov/moon/facts/" },
        };
    }
}
