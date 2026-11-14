// Pioneer Base - Debrief.cs
// Port of js/debrief.js. Builds the debrief from the real log of one game.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static PioneerBase.GameData;

namespace PioneerBase
{
    public class Step { public int day; public string text; }
    public class DebriefResult
    {
        public bool ok; public int stars; public string cause, headline, tradeoff, tip;
        public List<Step> chain;
    }

    public static class Debrief
    {
        static readonly Dictionary<string, string> NAMES = new Dictionary<string, string> {
            { "solar", "solar array" }, { "battery", "battery" }, { "reactor", "fission reactor" },
            { "greenhouse", "greenhouse" }, { "drill", "ice drill" }, { "shield", "shield layer" } };
        static readonly Dictionary<string, string> CATS = new Dictionary<string, string> {
            { "solar", "power" }, { "battery", "power" }, { "reactor", "power" },
            { "greenhouse", "food" }, { "drill", "water" }, { "shield", "radiation shield" } };
        static readonly Dictionary<string, string> FAULT_NAME = new Dictionary<string, string> {
            { "oxygen", "oxygen plant" }, { "lab", "science lab" }, { "drill", "ice drill" }, { "greenhouse", "greenhouse" } };
        static readonly Dictionary<string, string> FAULT_FOR = new Dictionary<string, string> {
            { "o2", "oxygen" }, { "water", "drill" }, { "food", "greenhouse" } };

        static string F(double x) { return x.ToString("0.##########", CultureInfo.InvariantCulture); }
        static string N0(double x) { return Sim.RoundJs(x).ToString(CultureInfo.InvariantCulture); }
        static double R1(double x) { return Math.Floor(x * 10 + 0.5) / 10; }
        static string Plural(int n, string w)
        {
            if (n == 1) return n + " " + w;
            return n + " " + (w.EndsWith("battery") ? w.Substring(0, w.Length - 1) + "ies" : w + "s");
        }

        class Spent { public Dictionary<string, int> by; public List<KeyValuePair<string, int>> n; public int total; }
        static Spent GetSpent(GameState st)
        {
            var by = new Dictionary<string, int> { { "power", 0 }, { "food", 0 }, { "water", 0 }, { "radiation shield", 0 } };
            var order = new List<string>(); var cnt = new Dictionary<string, int>();
            foreach (var b in st.builds)
            {
                by[CATS[b.type]] += Sim.GetTypeInfo(b.type).cost;
                if (!cnt.ContainsKey(b.type)) { cnt[b.type] = 0; order.Add(b.type); }
                cnt[b.type]++;
            }
            return new Spent { by = by, n = order.Select(t => new KeyValuePair<string, int>(t, cnt[t])).ToList(),
                total = by.Values.Sum() };
        }
        public static string BuildText(GameState st)
        {
            var s = GetSpent(st);
            var parts = s.n.Select(kv => Plural(kv.Value, NAMES[kv.Key])).ToList();
            return parts.Count > 0 ? string.Join(", ", parts) : "nothing";
        }
        static string SpendPhrase(GameState st)
        { return st.builds.Count > 0 ? "You put your crates into " + BuildText(st) : "You built nothing"; }

        static int? DayOf(GameState st, string key) { int v; return st.first.TryGetValue(key, out v) ? v : (int?)null; }
        static bool HasModule(GameState st, string t) { int v; return st.mod.TryGetValue(t, out v) && v > 0; }
        static double? DustAt(GameState st, int day)
        { foreach (var h in st.history) if (h.day == day) return h.dust; return null; }

        static List<Step> PowerSteps(GameState st)
        {
            var steps = new List<Step>(); var dd = DayOf(st, "deficit"); string site = st.site;
            if (dd == null) return steps;
            var night = DayOf(st, "night"); var dust = DayOf(st, "dustStorm");
            var hd = st.history.FirstOrDefault(h => h.day == dd.Value);
            if (site == "moon" && night != null && dd >= night)
                steps.Add(new Step { day = night.Value, text = "The lunar night began. The solar arrays make no power for " + F(V("lunar_night")) + " days" + (HasModule(st, "reactor") ? "." : ", and you had no reactor.") });
            else if (site == "mars" && dust != null && dd >= dust)
                steps.Add(new Step { day = dust.Value, text = "A dust storm began. The arrays made only " + Sim.RoundJs(V("dust_storm_mult") * 100) + "% of their power" + (st.counts["cleaned"] == 0 ? ", and you had never cleaned the dust off them." : ".") });
            else if (site == "mars")
            {
                var f = DustAt(st, dd.Value);
                steps.Add(new Step { day = dd.Value, text = "The base needed " + (hd != null ? N0(hd.demand) + " energy" : "more power") + " but the arrays made " + (hd != null ? N0(hd.supply) + " energy" : "less") + (f != null && f < 0.95 ? " (dust had cut them to " + Sim.RoundJs(f.Value * 100) + "%)" : "") + ". The battery began to drain." });
            }
            else
                steps.Add(new Step { day = dd.Value, text = "The base needed " + (hd != null ? N0(hd.demand) + " energy" : "more power") + " but " + Plural(st.mod["solar"], "solar array") + " made " + (hd != null ? N0(hd.supply) + " energy" : "less") + ". The battery began to drain." });
            var be = DayOf(st, "batteryEmpty");
            if (be != null) steps.Add(new Step { day = be.Value, text = "The battery ran empty." });
            return steps;
        }
        static Step ShutText(GameState st, string sys, string label)
        { var d = DayOf(st, "shut_" + sys); return d == null ? null : new Step { day = d.Value, text = label }; }

        static string CauseKey(GameState st)
        {
            if (st.ending.type == "success") return "success";
            if (st.ending.reason == "dose") return "dose";
            double o2 = st.lost["o2"], hab = st.lost["hab"], water = st.lost["water"], food = st.lost["food"] + st.lost["ration"];
            string best = "o2"; double bv = o2;
            if (hab > bv) { best = "hab"; bv = hab; }
            if (water > bv) { best = "water"; bv = water; }
            if (food > bv) { best = "food"; bv = food; }
            if (bv <= 0) best = "o2";
            return best;
        }

        static Step FaultStep(GameState st, string key)
        {
            string want; if (!FAULT_FOR.TryGetValue(key, out want)) return null;
            foreach (var f in st.faultLog)
                if (f.type == want)
                    return new Step { day = f.day, text = "The " + FAULT_NAME[want] + " broke" + (f.why == "emergency" ? " in the fuel-to-water emergency" : "") + (f.fixedDay != null ? " and was fixed on day " + f.fixedDay + "." : " and was never repaired.") };
            return null;
        }

        static List<Step> ChainFor(GameState st, string key)
        {
            var steps = new List<Step>(); var end = st.ending; string site = st.site; Step s;
            string endLabel = end.reason == "dose"
                ? "The crew's radiation dose passed the " + F(V("dose_limit")) + " mSv mission limit, so they were evacuated."
                : "The crew's health reached zero, so they were medically evacuated.";
            if (key == "o2")
            {
                var pnw = DayOf(st, "plantNoWater"); var o2d = DayOf(st, "o2Short");
                var shutO = ShutText(st, "oxygen", "The oxygen plant shut down because there was not enough power.");
                if (pnw != null && (shutO == null || pnw <= shutO.day))
                {
                    if (DayOf(st, "waterEmpty") != null || st.water < V("o2p_water_moon"))
                        steps.Add(new Step { day = 1, text = "The Moon oxygen plant splits water to make oxygen: " + F(V("o2p_water_moon")) + " L of water for each " + F(V("o2p_out")) + " kg." });
                    steps.Add(new Step { day = pnw.Value, text = "There was not enough water, so the oxygen plant could not run." });
                }
                else
                {
                    var ps = PowerSteps(st);
                    steps.AddRange(ps);
                    if (shutO != null) steps.Add(shutO);
                    else if (ps.Count == 0) steps.Add(new Step { day = 1, text = "The oxygen plant made 4 kg a day, but four astronauts breathe 3.4 kg and the plant was often off." });
                }
                if (o2d != null) steps.Add(new Step { day = o2d.Value, text = "The oxygen tank ran dry. The crew lost " + F(-V("pen_o2")) + " health each day without air." });
            }
            else if (key == "hab")
            {
                steps.AddRange(PowerSteps(st));
                s = ShutText(st, "habitat", "The habitat core lost power. It stopped recycling water and the crew lost " + F(-V("pen_hab")) + " health a day.");
                if (s != null) steps.Add(s);
            }
            else if (key == "water")
            {
                steps.Add(new Step { day = 1, text = "The crew and the oxygen plant use water every day. You started with " + F(V("water_start")) + " L." + (HasModule(st, "drill") ? "" : " You never built an ice drill.") });
                var sd = ShutText(st, "drill", "The ice drill shut down when power ran short, so no new water came in.");
                if (sd != null) steps.Add(sd);
                var hd = DayOf(st, "habDown");
                if (hd != null) steps.Add(new Step { day = hd.Value, text = "The habitat lost power, so water recycling stopped and the tank drained faster." });
                var we = DayOf(st, "waterEmpty");
                if (we != null) steps.Add(new Step { day = we.Value, text = "The water ran out. The crew lost " + F(-V("pen_water")) + " health each day." });
            }
            else if (key == "food")
            {
                steps.Add(new Step { day = 1, text = "Four astronauts eat " + F(R1(4 * V("food_person"))) + " kg a day. You started with " + F(V("food_start")) + " kg." });
                if (st.counts["lean"] + st.counts["half"] > 0)
                    steps.Add(new Step { day = 1, text = "You used " + Plural(st.counts["lean"], "Lean day") + " and " + Plural(st.counts["half"], "Half day") + ". Smaller rations cost health." });
                steps.Add(new Step { day = 1, text = HasModule(st, "greenhouse") ? "Your greenhouse crops grow in 10 days, but they need power and water to keep growing." : "You built no greenhouse, so no new food was grown." });
                var fe = DayOf(st, "foodEmpty");
                if (fe != null) steps.Add(new Step { day = fe.Value, text = "The food ran out. The crew lost " + F(-V("pen_food")) + " health each day." });
            }
            else if (key == "dose")
            {
                double bas = site == "moon" ? V("dose_moon") : V("dose_mars");
                steps.Add(new Step { day = 1, text = (site == "moon" ? "The Moon has no air to block radiation: " : "Mars has a thin air layer, but it blocks little: ") + F(bas) + " mSv every day." + (st.mod["shield"] == 0 ? " You built no shield layers." : " You had " + Plural(st.mod["shield"], "shield layer") + ".") });
                if (st.counts["stormUnsheltered"] > 0)
                {
                    var h = st.history.First(x => x.solarStorm && x.activity != "shelter");
                    steps.Add(new Step { day = h.day, text = "A solar storm hit and the crew did not shelter. The dose jumped." });
                }
                else if (st.counts["stormSheltered"] > 0)
                    steps.Add(new Step { day = st.history.First(x => x.solarStorm).day, text = "You sheltered in the storm, but the daily dose kept adding up." });
            }
            var fs = FaultStep(st, key);
            if (fs != null && steps.Count < 5) steps.Add(fs);
            steps.Add(new Step { day = end.day, text = endLabel });
            if (steps.Count < 3)
                steps.Insert(0, new Step { day = 1, text = "You started with " + F(V("crates_start")) + " crates and built " + BuildText(st) + "." });
            if (steps.Count < 3)
                steps.Insert(steps.Count - 1, new Step { day = Math.Max(1, end.day - 1), text = "Health kept falling. Final health: " + Sim.RoundJs(st.health) + " of 100." });
            return steps;
        }

        static List<Step> SuccessSteps(GameState st)
        {
            var steps = new List<Step>(); string site = st.site; var s = GetSpent(st);
            steps.Add(new Step { day = 1, text = "You spent " + s.total + " of your " + F(V("crates_start")) + " crates on: " + BuildText(st) + "." });
            if (site == "moon")
            {
                int nd = DayOf(st, "night") ?? 11;
                steps.Add(new Step { day = nd, text = HasModule(st, "reactor") ? "The lunar night began. Your reactor kept the habitat running while the arrays made nothing." : "The lunar night began. You got through it on battery power and careful switching." });
            }
            else
            {
                int ds = DayOf(st, "dustStorm") ?? 12;
                steps.Add(new Step { day = ds, text = "The dust storm began. " + (HasModule(st, "reactor") ? "Your reactor kept power on while the arrays fell to " + Sim.RoundJs(V("dust_storm_mult") * 100) + "%." : "You rode it out with careful power use.") + (st.counts["cleaned"] > 0 ? " You cleaned the arrays on " + Plural(st.counts["cleaned"], "day") + "." : "") });
            }
            int storms = st.counts["stormSheltered"] + st.counts["stormUnsheltered"];
            var first = st.history.FirstOrDefault(h => h.solarStorm);
            steps.Add(new Step { day = first != null ? first.day : 7,
                text = "Radiation: " + Plural(st.mod["shield"], "shield layer") + ", " + (storms > 0 ? "sheltered in " + st.counts["stormSheltered"] + " of " + storms + " storm(s)" : "no storms hit") + ". Total dose: " + F(R1(st.dose)) + " mSv of " + F(V("dose_limit")) + "." });
            steps.Add(new Step { day = st.ending.day, text = "All four astronauts finished the mission. Science: " + F(st.science) + ". Health: " + Sim.RoundJs(st.health) + "." });
            return steps;
        }

        static bool PlantOff(GameState st, string key)
        { return key == "o2" && !st.first.ContainsKey("shut_oxygen") && !st.first.ContainsKey("plantNoWater"); }

        static string TradeoffFor(GameState st, string key)
        {
            string site = st.site;
            if (PlantOff(st, key)) return "Switch the oxygen plant on every day. It makes " + F(V("o2p_out")) + " kg, and the crew breathes " + F(R1(4 * V("o2_person"))) + " kg a day.";
            if (key == "success")
            {
                var parts = new List<string>();
                if (HasModule(st, "reactor"))
                    parts.Add("The reactor used " + F(V("reactor_cost")) + " crates, so you could afford " + (st.mod["greenhouse"] > 0 ? Plural(st.mod["greenhouse"], "greenhouse") : "no greenhouse") + " and " + (st.mod["drill"] > 0 ? Plural(st.mod["drill"], "ice drill") : "no ice drill"));
                else parts.Add("Without a reactor, you put your crates into " + BuildText(st));
                if (st.counts["lean"] + st.counts["half"] == 0) parts.Add("you could feed the crew Full rations every day");
                else parts.Add("you ran Lean rations for " + Plural(st.counts["lean"], "day") + (st.counts["half"] > 0 ? " and Half rations for " + Plural(st.counts["half"], "day") : ""));
                foreach (var f in st.faultLog)
                    parts.Add("a " + FAULT_NAME[f.type] + " broke on day " + f.day + (f.fixedDay != null ? " and you fixed it on day " + f.fixedDay : " and stayed broken"));
                parts.Add("and had " + Plural(st.counts["science"], "Science day") + ", " + Plural(st.counts["maintenance"], "Maintenance day") + " and " + Plural(st.counts["shelter"], "Shelter day"));
                return string.Join(", ", parts) + ".";
            }
            if ((key == "o2" || key == "hab") && site == "moon" && !HasModule(st, "reactor"))
                return SpendPhrase(st) + " instead of a power source that works at night. A reactor costs " + F(V("reactor_cost")) + " crates.";
            if ((key == "o2" || key == "hab") && site == "mars" && !HasModule(st, "reactor"))
                return SpendPhrase(st) + ", and no reactor. Solar power falls in a dust storm. A reactor does not.";
            if (key == "o2" || key == "hab")
                return "You built " + BuildText(st) + ", but the base still ran short of power. Every system you switch on spends power you may need later.";
            if (key == "dose")
                return st.mod["shield"] == 0
                    ? (st.builds.Count > 0 ? "You built " + BuildText(st) + " but no shield layers." : "You built no shield layers.") + " Shielding costs " + F(V("shield_cost")) + " crates a layer and cuts the dose by a quarter each time."
                    : "You had " + Plural(st.mod["shield"], "shield layer") + ", but " + (st.counts["stormUnsheltered"] > 0 ? "you did not shelter in the storm" : "the daily dose still added up") + ". Shielding and sheltering work together.";
            if (key == "water")
                return HasModule(st, "drill") ? "You had an ice drill, but it could not make water faster than the crew used it." : SpendPhrase(st) + ", and no ice drill. Water is used every day.";
            return HasModule(st, "greenhouse")
                ? "Your greenhouse could not grow food fast enough. You need food or smaller rations, and smaller rations cost health."
                : "You built no greenhouse. Without one the only choice is to cut rations, and that costs health.";
        }

        static string TipFor(GameState st, string key)
        {
            string site = st.site;
            if (key == "success")
            {
                if (st.ending.stars < 3)
                {
                    if (st.science < V("star3_science")) return "Try for three stars: do more Science days while the lab has power (you got " + F(st.science) + " of 45 points).";
                    if (st.health < V("star3_health")) return "Try for three stars: keep health above 70. Use Full rations when the food store can afford it.";
                    return "Try for three stars: add a shield layer or shelter in every storm to keep the dose under " + (site == "moon" ? 35 : 22) + " mSv.";
                }
                return "Great mission! Now try the other site. " + (site == "moon" ? "Mars has dust instead of night." : "The Moon has a 15-day night.");
            }
            if (key == "dose") return st.mod["shield"] < 2 ? "Build at least one shield layer on day 1 and press Shelter on every storm day." : "Press Shelter on storm days: it cuts the storm dose to one tenth.";
            if (site == "moon" && (key == "o2" || key == "hab") && !HasModule(st, "reactor")) return "Build the fission reactor on day 1. It works through the 15-day night.";
            if (site == "mars" && (key == "o2" || key == "hab"))
                return st.counts["cleaned"] == 0 ? "Use a Maintenance day to clean the arrays every few days, and before the dust storm on day 12." : "Add a reactor, and keep cleaning the arrays. In the dust storm, switch off the lab and greenhouse to save power.";
            if (key == "o2" || key == "hab") return "Watch the \"Energy today\" line. When power is short, switch off the lab first and keep the oxygen plant on.";
            if (key == "water") return "Build an ice drill early and keep it switched on while you have spare power.";
            return "Build a greenhouse early (crops take 10 days) and use Lean rations at the start, then Full rations when the store is safe.";
        }

        public static DebriefResult Analyze(GameState st)
        {
            string key = CauseKey(st); var end = st.ending; bool ok = end.type == "success";
            string headline;
            if (ok) headline = "Mission complete! All four astronauts finished 30 days" + (end.stars > 1 ? " and earned " + end.stars + " stars." : ".");
            else if (end.reason == "dose") headline = "Emergency evacuation on day " + end.day + ". The crew got too much radiation.";
            else headline = "Medical evacuation on day " + end.day + ". The crew's health ran out.";
            var chain = (ok ? SuccessSteps(st) : ChainFor(st, key)).OrderBy(x => x.day).ToList(); // OrderBy is stable
            return new DebriefResult { ok = ok, stars = end.stars, cause = key, headline = headline, chain = chain,
                tradeoff = TradeoffFor(st, key), tip = TipFor(st, key) };
        }
    }
}
