// Pioneer Base - Sim.cs
// Port of js/sim.js. Pure simulation, no UnityEngine dependency.
// One call of RunDay() = one mission day, resolved in the same order as the web version.
using System;
using System.Collections.Generic;
using System.Linq;
using static PioneerBase.GameData;

namespace PioneerBase
{
    public class Rng
    {
        uint a;
        public Rng(int seed) { a = unchecked((uint)seed); }
        // mulberry32, same bits as the JS version
        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5u;
                uint t = (a ^ (a >> 15)) * (1u | a);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }

    public class Plan
    {
        public bool oxygen = true, lab = true, repair, emergency;
        public int greenhouse, drill;
        public string activity = "science", ration = "full";
        public List<string> build = new List<string>();
        public Plan Clone()
        {
            var p = (Plan)MemberwiseClone();
            p.build = new List<string>(build);
            return p;
        }
    }

    public class Fault
    {
        public string type, why; public int day, mDays; public int? fixedDay;
        public Fault Copy() { return (Fault)MemberwiseClone(); }
    }
    public class Pending { public string type; public int day; }
    public class BuildRec { public int day; public string type; }
    public class StormEvt { public int day, alert; public string real; }
    public class DustEvt { public int start, end, alert; }
    public class Env { public bool night, dustStorm, solarStorm; }

    public class HistoryEntry
    {
        public int day;
        public double supply, used, demand, battery, cap, o2, water, food, health, dose, science, dust, harvest;
        public List<string> events, shut;
        public string activity, ration;
        public bool night, dustStorm, solarStorm, noWater;
    }

    public class Ending { public string type, reason; public int day, stars; }

    public class GameState
    {
        public string site; public int seed, day = 1; public bool over; public Ending ending;
        public double o2, water, food, battery, health, dose, science;
        public int crates;
        public Dictionary<string, int> mod = new Dictionary<string, int>();
        public List<int> gh = new List<int>();
        public List<Pending> pending = new List<Pending>();
        public int dustDays;
        public List<StormEvt> storms = new List<StormEvt>();
        public DustEvt dust;
        public int faultDay; public double faultRoll, emerg, emergPick;
        public List<Fault> faults = new List<Fault>(), faultLog = new List<Fault>();
        public bool emergencyUsed; public int emergencyDay, repairs;
        public List<HistoryEntry> history = new List<HistoryEntry>();
        public List<BuildRec> builds = new List<BuildRec>();
        public Dictionary<string, int> first = new Dictionary<string, int>();
        public Dictionary<string, double> lost = new Dictionary<string, double> { { "o2", 0 }, { "hab", 0 }, { "water", 0 }, { "food", 0 }, { "ration", 0 } };
        public Dictionary<string, int> counts = new Dictionary<string, int> {
            { "full", 0 }, { "lean", 0 }, { "half", 0 }, { "science", 0 }, { "maintenance", 0 }, { "shelter", 0 },
            { "stormUnsheltered", 0 }, { "stormSheltered", 0 }, { "cleaned", 0 } };

        // Copy for previews. History is not copied (previews never read it).
        public GameState Clone()
        {
            var c = (GameState)MemberwiseClone();
            c.ending = ending; // null during play
            c.mod = new Dictionary<string, int>(mod);
            c.gh = new List<int>(gh);
            c.pending = pending.Select(p => new Pending { type = p.type, day = p.day }).ToList();
            c.storms = storms; // read only
            c.dust = dust;     // read only
            c.faults = faults.Select(CloneFault).ToList();
            c.faultLog = faultLog.Select(CloneFault).ToList();
            c.history = new List<HistoryEntry>();
            c.builds = new List<BuildRec>(builds);
            c.first = new Dictionary<string, int>(first);
            c.lost = new Dictionary<string, double>(lost);
            c.counts = new Dictionary<string, int>(counts);
            return c;
        }
        static Fault CloneFault(Fault f) { return f.Copy(); }
    }

    public class DayRecord
    {
        public int day;
        public List<string> events = new List<string>(), shut = new List<string>(), idle = new List<string>(),
            builtToday = new List<string>(), activated = new List<string>();
        public double harvest, solar, reactor, supply, dustFactor, demand, used, batteryBefore, foodNeed, o2prod, o2need,
            waterNetPlan, doseToday, science, healthDelta;
        public int litGh;
        public Dictionary<string, int> broken;
        public Env env;
        public bool emergency, cleaned, noWater, sheltered, o2Short, waterEmpty, foodEmpty, habOn, labOn, oxOn, repairedByCrew;
        public string emergencyDamage, faultNew, repaired, activity, ration;
    }

    public class PreviewResult
    {
        public DayRecord rec;
        public double supply, demand, used, doseToday, healthDelta;
        public List<string> shut, idle;
        public bool habOn;
        public double netO2, netWater, netFood, netBattery;
        public int? runoutO2, runoutWater, runoutFood, runoutBattery;
    }

    public class TypeInfo { public string type, name; public int cost, max; }

    public static class Sim
    {
        public const int MAX_DAY = 30;
        static readonly Dictionary<string, string[]> COSTS = new Dictionary<string, string[]> {
            { "solar", new[] { "solar_cost", "solar_max", "Solar array" } },
            { "battery", new[] { "battery_cost", "battery_max", "Battery" } },
            { "reactor", new[] { "reactor_cost", null, "Fission reactor" } },
            { "greenhouse", new[] { "gh_cost", "gh_max", "Greenhouse" } },
            { "drill", new[] { "drill_cost", "drill_max", "Ice drill" } },
            { "shield", new[] { "shield_cost", "shield_max", "Shield layer" } } };
        public static readonly string[] TYPES = { "solar", "battery", "reactor", "greenhouse", "drill", "shield" };

        static double Clamp(double x, double lo, double hi) { return Math.Max(lo, Math.Min(hi, x)); }
        public static int RoundJs(double x) { return (int)Math.Floor(x + 0.5); }

        public static TypeInfo GetTypeInfo(string type)
        {
            var c = COSTS[type];
            return new TypeInfo { type = type, name = c[2], cost = (int)V(c[0]), max = c[1] != null ? (int)V(c[1]) : (type == "reactor" ? 1 : 99) };
        }
        public static int Owned(GameState st, string type)
        {
            int n; st.mod.TryGetValue(type, out n);
            foreach (var p in st.pending) if (p.type == type) n++;
            return n;
        }
        public static bool CanBuild(GameState st, string type)
        {
            var i = GetTypeInfo(type);
            return st.crates >= i.cost && Owned(st, type) < i.max;
        }

        public static GameState NewGame(string site, int seed)
        {
            var r = new Rng(unchecked(seed * 7919 + (site == "mars" ? 17 : 3)));
            var sc = Scenarios[site];
            Func<int> jit = () => (int)Math.Floor(r.Next() * 3) - 1;
            var st = new GameState { site = site, seed = seed };
            foreach (var s in sc.storms) st.storms.Add(new StormEvt { day = s.day + jit(), alert = s.alert, real = s.real });
            if (sc.dust != null)
            {
                int j = jit();
                st.dust = new DustEvt { start = sc.dust.start + j, end = sc.dust.end + j, alert = sc.dust.alert };
            }
            st.faultDay = 8 + (int)Math.Floor(r.Next() * 6);
            st.faultRoll = r.Next();
            st.emerg = r.Next(); st.emergPick = r.Next();
            st.o2 = V("o2_start"); st.water = V("water_start"); st.food = V("food_start");
            st.crates = (int)V("crates_start"); st.battery = V("battery_start");
            st.health = V("health_start");
            st.mod["solar"] = site == "moon" ? 2 : 4; st.mod["battery"] = 1; st.mod["reactor"] = 0;
            st.mod["shield"] = 0; st.mod["greenhouse"] = 0; st.mod["drill"] = 0;
            return st;
        }

        public static Env EnvFor(GameState st, int d)
        {
            var sc = Scenarios[st.site];
            return new Env {
                night = st.site == "moon" && d >= sc.nightStart && d <= sc.nightEnd,
                dustStorm = st.dust != null && d >= st.dust.start && d <= st.dust.end,
                solarStorm = st.storms.Any(s => s.day == d)
            };
        }

        public class Alert { public string type, text; public int daysLeft; }
        static string PctStorm() { return RoundJs(V("dust_storm_mult") * 100) + "%"; }

        public static List<Alert> AlertsFor(GameState st, int d)
        {
            var o = new List<Alert>();
            foreach (var s in st.storms)
            {
                int left = s.day - d;
                if (left >= 0 && left <= s.alert)
                    o.Add(new Alert { type = "solar", daysLeft = left, text = left == 0
                        ? "Solar storm TODAY. Radiation is high. Sheltering cuts the dose to one tenth."
                        : "Solar storm tomorrow (day " + s.day + "). Plan to shelter." });
            }
            var du = st.dust;
            if (du != null)
            {
                int left2 = du.start - d;
                if (left2 >= 0 && left2 <= du.alert)
                    o.Add(new Alert { type = "dust", daysLeft = left2, text = left2 == 0
                        ? "Dust storm starts today. Solar power drops to " + PctStorm() + " for 6 days."
                        : "Dust storm in " + left2 + " day" + (left2 > 1 ? "s" : "") + " (day " + du.start + "). Solar power will drop to " + PctStorm() + " for 6 days." });
                else if (d >= du.start && d <= du.end)
                    o.Add(new Alert { type = "dust", daysLeft = 0, text = "Dust storm until day " + du.end + ". Solar power is only " + PctStorm() + " of normal." });
            }
            var sc = Scenarios[st.site];
            if (st.site == "moon")
            {
                if (d == sc.nightStart - 1) o.Add(new Alert { type = "night", daysLeft = 1, text = "Lunar night begins tomorrow. It lasts 15 days. Arrays make no power." });
                if (d >= sc.nightStart && d <= sc.nightEnd) o.Add(new Alert { type = "night", daysLeft = 0, text = "Lunar night until day " + sc.nightEnd + ". Arrays make no power." });
            }
            return o;
        }

        public static Plan NormalizePlan(GameState st, Plan plan)
        {
            plan = plan ?? new Plan();
            var p = new Plan {
                oxygen = plan.oxygen, lab = plan.lab,
                greenhouse = Math.Max(0, Math.Min(plan.greenhouse, st.mod["greenhouse"])),
                drill = Math.Max(0, Math.Min(plan.drill, st.mod["drill"])),
                activity = plan.activity ?? "science", ration = plan.ration ?? "full",
                build = new List<string>(plan.build ?? new List<string>()),
                repair = plan.repair, emergency = plan.emergency };
            if (p.activity != "science" && p.activity != "maintenance" && p.activity != "shelter") p.activity = "science";
            if (p.ration != "full" && p.ration != "lean" && p.ration != "half") p.ration = "full";
            return p;
        }

        static double RationFactor(string r) { return r == "full" ? 1 : (r == "lean" ? V("lean_factor") : V("half_factor")); }
        static double RationHealth(string r) { return r == "full" ? V("ration_full") : (r == "lean" ? V("ration_lean") : V("ration_half")); }
        static void Note(GameState st, string key, int day) { if (!st.first.ContainsKey(key)) st.first[key] = day; }

        static List<string> Installed(GameState st)
        {
            var c = new List<string> { "oxygen", "lab" };
            if (st.mod["drill"] > 0) c.Add("drill");
            if (st.mod["greenhouse"] > 0) c.Add("greenhouse");
            return c;
        }
        static string Fix(GameState st, int d)
        {
            var f = st.faults[0]; st.faults.RemoveAt(0); f.fixedDay = d; return f.type;
        }
        static string AddFault(GameState st, double roll, int d, string why)
        {
            var c = Installed(st);
            string type = c[Math.Min(c.Count - 1, (int)Math.Floor(roll * c.Count))];
            var f = new Fault { type = type, day = d, mDays = 0, why = why };
            st.faults.Add(f); st.faultLog.Add(f);
            Note(st, "fault", d);
            return type;
        }

        class Load { public string id; public int idx; public double p; }
        static int Has(List<Load> loads, string id) { return loads.Count(x => x.id == id); }

        public static DayRecord RunDay(GameState st, Plan planIn, bool preview = false)
        {
            if (st.over) return null;
            string site = st.site; int d = st.day;
            var plan = NormalizePlan(st, planIn);
            var rec = new DayRecord { day = d };

            // 0. pay for builds chosen today (they start working tomorrow)
            foreach (var type in plan.build)
            {
                if (TYPES.Contains(type) && CanBuild(st, type))
                {
                    st.crates -= GetTypeInfo(type).cost;
                    st.pending.Add(new Pending { type = type, day = d + 1 });
                    st.builds.Add(new BuildRec { day = d, type = type });
                    rec.builtToday.Add(type);
                }
            }

            // 1. modules built yesterday start working
            var keep = new List<Pending>();
            foreach (var p in st.pending)
            {
                if (p.day <= d)
                {
                    st.mod[p.type] += 1;
                    if (p.type == "greenhouse") st.gh.Add(0);
                    rec.activated.Add(p.type);
                }
                else keep.Add(p);
            }
            st.pending = keep;

            // 1b. emergency, faults, repairs
            if (plan.emergency && !st.emergencyUsed)
            {
                st.emergencyUsed = true; st.emergencyDay = d;
                st.water = Math.Min(V("water_cap"), st.water + V("emerg_water"));
                rec.emergency = true; Note(st, "emergency", d);
                if (!preview && st.emerg < V("emerg_risk")) rec.emergencyDamage = AddFault(st, st.emergPick, d, "emergency");
            }
            if (!preview && d == st.faultDay) rec.faultNew = AddFault(st, st.faultRoll, d, "wear");
            if (plan.repair && st.faults.Count > 0 && st.crates >= V("repair_crates"))
            {
                st.crates -= (int)V("repair_crates"); rec.repaired = Fix(st, d); st.repairs++;
            }
            var broken = new Dictionary<string, int> { { "oxygen", 0 }, { "lab", 0 }, { "drill", 0 }, { "greenhouse", 0 } };
            foreach (var f in st.faults) broken[f.type]++;
            rec.broken = broken;
            if (broken["oxygen"] > 0) plan.oxygen = false;
            if (broken["lab"] > 0) plan.lab = false;
            plan.greenhouse = Math.Min(plan.greenhouse, Math.Max(0, st.mod["greenhouse"] - broken["greenhouse"]));
            plan.drill = Math.Min(plan.drill, Math.Max(0, st.mod["drill"] - broken["drill"]));
            plan.greenhouse = Math.Min(plan.greenhouse, st.mod["greenhouse"]);
            plan.drill = Math.Min(plan.drill, st.mod["drill"]);

            // 2. environment
            var env = EnvFor(st, d);
            var sc = Scenarios[site];
            if (site == "moon" && d == sc.nightStart) { rec.events.Add("The lunar night begins."); Note(st, "night", d); }
            if (site == "moon" && d == sc.nightEnd + 1) rec.events.Add("Sunrise! The lunar night is over.");
            if (st.dust != null && d == st.dust.start) { rec.events.Add("A dust storm begins."); Note(st, "dustStorm", d); }
            if (st.dust != null && d == st.dust.end + 1) rec.events.Add("The dust storm is over.");
            if (env.solarStorm) rec.events.Add("A solar storm hits.");
            if (site == "mars" && plan.activity == "maintenance") { st.dustDays = 0; st.counts["cleaned"]++; rec.cleaned = true; }
            double dustFactor = site == "mars" ? Math.Max(V("dust_floor"), 1 - V("dust_loss") * st.dustDays) : 1;
            double sunFactor = site == "moon" ? (env.night ? 0 : 1) : 1;
            double stormMult = env.dustStorm ? V("dust_storm_mult") : 1;
            double perArray = site == "moon" ? V("solar_moon") : V("solar_mars");
            double solar = st.mod["solar"] * perArray * sunFactor * dustFactor * stormMult;
            double reactor = st.mod["reactor"] * V("reactor_out");
            double supply = solar + reactor;
            rec.solar = solar; rec.reactor = reactor; rec.supply = supply; rec.dustFactor = dustFactor; rec.env = env;

            // 3. energy: loads in shutdown order
            double o2pPower = site == "moon" ? V("o2p_power_moon") : V("o2p_power_mars");
            var loads = new List<Load>();
            if (plan.lab) loads.Add(new Load { id = "lab", p = V("lab_power") });
            for (int i = 0; i < plan.drill; i++) loads.Add(new Load { id = "drill", p = V("drill_power") });
            for (int g = 0; g < plan.greenhouse; g++) loads.Add(new Load { id = "greenhouse", idx = g, p = V("gh_power") });
            if (plan.oxygen) loads.Add(new Load { id = "oxygen", p = o2pPower });
            loads.Add(new Load { id = "habitat", p = V("hab_power") });
            double total = loads.Sum(l => l.p);
            rec.demand = total;
            double avail = supply + st.battery;
            bool shortage = total > supply;
            while (total > avail + 1e-9 && loads.Count > 0)
            {
                var l0 = loads[0]; loads.RemoveAt(0); total -= l0.p; rec.shut.Add(l0.id);
            }
            bool habOn = Has(loads, "habitat") > 0;

            // 4. water
            double water = st.water + Has(loads, "drill") * V("drill_out");
            double crewUse = V("crew_water") * (habOn ? (1 - V("hab_recycle")) : 1);
            bool waterEmpty = false;
            if (water >= crewUse) water -= crewUse; else { water = 0; waterEmpty = true; }
            bool oxOn = Has(loads, "oxygen") > 0;
            if (oxOn && site == "moon")
            {
                if (water >= V("o2p_water_moon")) water -= V("o2p_water_moon");
                else { oxOn = false; rec.idle.Add("oxygen"); rec.noWater = true; Note(st, "plantNoWater", d); }
            }
            var litGh = new List<int>();
            foreach (var l in loads)
            {
                if (l.id == "greenhouse")
                {
                    if (water >= V("gh_water")) { water -= V("gh_water"); litGh.Add(l.idx); }
                    else rec.idle.Add("greenhouse");
                }
            }
            bool labOn = Has(loads, "lab") > 0;
            double used = 0;
            if (labOn) used += V("lab_power");
            used += Has(loads, "drill") * V("drill_power");
            used += litGh.Count * V("gh_power");
            if (oxOn) used += o2pPower;
            if (habOn) used += V("hab_power");
            double cap = V("battery_per") * st.mod["battery"];
            rec.batteryBefore = st.battery;
            st.battery = Clamp(st.battery + supply - used, 0, cap);
            rec.used = used;
            water = Clamp(water, 0, V("water_cap"));

            // 5. oxygen and food
            double o2prod = (oxOn ? V("o2p_out") : 0) + litGh.Count * V("gh_o2");
            double o2need = 4 * V("o2_person");
            double o2 = st.o2 + o2prod;
            bool o2Short = false;
            if (o2 >= o2need) o2 -= o2need; else { o2 = 0; o2Short = true; }
            o2 = Math.Min(o2, V("o2_cap"));
            double food = st.food;
            foreach (int idx in litGh)
            {
                st.gh[idx] = st.gh[idx] + 1;
                if (st.gh[idx] >= V("gh_cycle")) { st.gh[idx] = 0; food += V("gh_yield"); rec.harvest += V("gh_yield"); }
            }
            food = Math.Min(food, V("food_cap"));
            double foodNeed = 4 * V("food_person") * RationFactor(plan.ration);
            bool foodEmpty = false;
            if (food >= foodNeed) food -= foodNeed; else { food = 0; foodEmpty = true; }
            rec.litGh = litGh.Count; rec.foodNeed = foodNeed; rec.o2prod = o2prod; rec.o2need = o2need;
            rec.waterNetPlan = (Has(loads, "drill") * V("drill_out")) - crewUse - (oxOn && site == "moon" ? V("o2p_water_moon") : 0) - litGh.Count * V("gh_water");

            // 6. radiation
            double bas = site == "moon" ? V("dose_moon") : V("dose_mars");
            bool sheltered = plan.activity == "shelter";
            double storm = env.solarStorm ? (site == "moon" ? V("storm_moon") : V("storm_mars")) * (sheltered ? V("shelter_factor") : 1) : 0;
            double doseToday = (bas + storm) * Math.Pow(V("shield_factor"), st.mod["shield"]);
            rec.doseToday = doseToday; rec.sheltered = sheltered;
            if (env.solarStorm) { if (sheltered) st.counts["stormSheltered"]++; else st.counts["stormUnsheltered"]++; }

            // 7. health and science
            double delta = 0; bool anyShort = o2Short || !habOn || waterEmpty || foodEmpty;
            if (o2Short) { delta += V("pen_o2"); st.lost["o2"] += -V("pen_o2"); Note(st, "o2Short", d); }
            if (!habOn) { delta += V("pen_hab"); st.lost["hab"] += -V("pen_hab"); Note(st, "habDown", d); }
            if (waterEmpty) { delta += V("pen_water"); st.lost["water"] += -V("pen_water"); Note(st, "waterEmpty", d); }
            if (foodEmpty) { delta += V("pen_food"); st.lost["food"] += -V("pen_food"); Note(st, "foodEmpty", d); }
            if (!foodEmpty)
            {
                double rh = RationHealth(plan.ration);
                if (plan.ration == "full") { if (!anyShort) delta += rh; }
                else { delta += rh; st.lost["ration"] += -rh; }
            }
            st.health = Clamp(st.health + delta, 0, V("health_start"));
            double sci = 0;
            if (labOn && plan.activity == "science") { sci = V("lab_science"); st.science += sci; }
            rec.science = sci; rec.healthDelta = delta;

            // commit stocks
            st.o2 = o2; st.water = water; st.food = food; st.dose += doseToday;
            st.counts[plan.ration]++; st.counts[plan.activity]++;

            if (shortage && rec.shut.Count > 0)
            {
                foreach (var s in rec.shut) Note(st, "shut_" + s, d);
                Note(st, "firstShutdown", d);
            }
            if (shortage && st.battery <= 0.0001) Note(st, "batteryEmpty", d);
            if (shortage) Note(st, "deficit", d);
            rec.o2Short = o2Short; rec.waterEmpty = waterEmpty; rec.foodEmpty = foodEmpty; rec.habOn = habOn; rec.labOn = labOn; rec.oxOn = oxOn;
            rec.activity = plan.activity; rec.ration = plan.ration;

            st.history.Add(new HistoryEntry {
                day = d, supply = supply, used = used, demand = rec.demand, battery = st.battery, cap = cap, o2 = st.o2, water = st.water, food = st.food,
                health = st.health, dose = st.dose, science = st.science, events = new List<string>(rec.events), shut = new List<string>(rec.shut),
                activity = plan.activity, ration = plan.ration, dust = dustFactor, night = env.night, dustStorm = env.dustStorm,
                solarStorm = env.solarStorm, harvest = rec.harvest, noWater = rec.noWater });

            if (plan.activity == "maintenance" && st.faults.Count > 0 && rec.repaired == null)
            {
                st.faults[0].mDays++;
                if (st.faults[0].mDays >= V("repair_days")) { rec.repaired = Fix(st, d); rec.repairedByCrew = true; st.repairs++; }
            }

            // 8. end conditions
            if (st.dose > V("dose_limit")) { st.over = true; st.ending = new Ending { type = "evac", reason = "dose", day = d }; }
            else if (st.health <= 0) { st.over = true; st.ending = new Ending { type = "evac", reason = "health", day = d }; }
            else if (d >= MAX_DAY) { st.over = true; st.ending = new Ending { type = "success", day = d }; }
            if (!st.over)
            {
                st.day = d + 1;
                if (site == "mars") st.dustDays += 1;
            }
            if (st.over && st.ending.type == "evac") st.ending.stars = 0;
            if (st.over && st.ending.type == "success") st.ending.stars = StarsFor(st);
            return rec;
        }

        public static int StarsFor(GameState st)
        {
            int stars = 1;
            if (st.science >= V("star2_science")) stars = 2;
            double doseMax = st.site == "moon" ? V("star3_dose_moon") : V("star3_dose_mars");
            if (stars == 2 && st.science >= V("star3_science") && st.health >= V("star3_health") && st.dose <= doseMax) stars = 3;
            return stars;
        }

        public static int? Runout(double stock, double net)
        {
            if (net >= -1e-9) return null;
            return (int)Math.Max(0, Math.Ceiling(stock / -net - 1e-9));
        }

        public static PreviewResult Preview(GameState st, Plan planIn)
        {
            var c = st.Clone();
            var plan = NormalizePlan(c, planIn);
            double bO2 = c.o2, bWater = c.water, bFood = c.food, bBat = c.battery;
            var rec = RunDay(c, plan, true);
            double foodAvgGrow = rec.litGh * V("gh_yield") / V("gh_cycle");
            double netFood = -rec.foodNeed + foodAvgGrow;
            double netBat = c.battery - bBat;
            double o2net = rec.o2prod - rec.o2need;
            double waterNet = rec.waterNetPlan;
            return new PreviewResult {
                rec = rec, supply = rec.supply, demand = rec.demand, used = rec.used, shut = rec.shut, idle = rec.idle,
                habOn = rec.habOn, doseToday = rec.doseToday, healthDelta = rec.healthDelta,
                netO2 = o2net, netWater = waterNet, netFood = netFood, netBattery = netBat,
                runoutO2 = Runout(bO2, o2net), runoutWater = Runout(bWater, waterNet),
                runoutFood = Runout(bFood, netFood), runoutBattery = Runout(bBat, netBat) };
        }

        public static Plan DefaultPlan(GameState st)
        {
            return new Plan { oxygen = true, lab = true, greenhouse = st.mod["greenhouse"], drill = st.mod["drill"], activity = "science", ration = "full" };
        }
    }
}
