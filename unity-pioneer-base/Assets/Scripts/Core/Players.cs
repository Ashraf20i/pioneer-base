// Pioneer Base - Players.cs
// Port of tests/players.js: scripted players used by the balance tests. No UnityEngine dependency.
using System;
using System.Collections.Generic;
using System.Linq;
using static PioneerBase.GameData;

namespace PioneerBase
{
    public class ExpertOpts
    {
        public bool neverClean; public int cleanEvery = 5, buildDay = 1;
        public List<string> builds; public string ration;
    }

    public static class Players
    {
        static bool StormToday(GameState st) { return Sim.EnvFor(st, st.day).solarStorm; }
        static List<string> L(params string[] a) { return a.ToList(); }

        public static readonly Dictionary<string, List<string>> BUILDS = new Dictionary<string, List<string>> {
            { "moon", L("reactor", "drill", "drill", "greenhouse", "shield") },
            { "mars", L("reactor", "greenhouse", "drill", "shield", "solar") } };

        public static Plan ChooseSystems(GameState st, Plan plan, double reserve)
        {
            Plan best = null; double bestScore = -1e9; int hasGh = st.mod["greenhouse"], hasDr = st.mod["drill"];
            for (int ox = 1; ox >= 0; ox--)
                for (int lab = 1; lab >= 0; lab--)
                    for (int g = hasGh; g >= 0; g--)
                        for (int dr = hasDr; dr >= 0; dr--)
                        {
                            var p = plan.Clone();
                            p.oxygen = ox == 1; p.lab = lab == 1; p.greenhouse = g; p.drill = dr; p.build = new List<string>();
                            var pv = Sim.Preview(st, p);
                            if (!pv.habOn) continue;
                            if (pv.shut.Count > 0 || pv.idle.Count > 0) continue;
                            double score = 0;
                            if (ox == 1) score += st.o2 < 30 ? 100 : (st.o2 < 38 ? 40 : 0);
                            score += g * (st.food < 200 ? 22 : 5);
                            score += dr * (st.water < 100 ? (st.water < 60 ? 40 : 25) : 3);
                            if (lab == 1 && plan.activity == "science") score += 35;
                            double after = st.battery + pv.netBattery;
                            score -= Math.Max(0, reserve - after) * 3;
                            score += pv.netBattery * 0.01;
                            if (score > bestScore) { bestScore = score; best = p; }
                        }
            if (best == null)
            {
                best = plan.Clone(); best.oxygen = false; best.lab = false; best.greenhouse = 0; best.drill = 0;
            }
            best.build = new List<string>(plan.build ?? new List<string>());
            return best;
        }

        public static string RationChoice(GameState st, List<string> buildList)
        {
            int left = 31 - st.day; double harvest = 0;
            int extra = (buildList ?? new List<string>()).Count(t => t == "greenhouse");
            foreach (var p in st.pending) if (p.type == "greenhouse") extra++;
            harvest += extra * Math.Floor((left - 1) / V("gh_cycle")) * V("gh_yield");
            for (int i = 0; i < st.gh.Count; i++)
                harvest += Math.Floor((left + st.gh[i]) / V("gh_cycle")) * V("gh_yield");
            harvest *= 0.8;
            Func<double, double> need = f => 7.2 * f * left;
            Func<double, bool> afford = f => st.food + harvest >= need(f);
            if (st.health < 85 && afford(0.75)) return afford(1) ? "full" : "lean";
            if (afford(0.75)) return "lean";
            return "half";
        }

        public static Func<GameState, Plan> Expert(ExpertOpts opts = null)
        {
            opts = opts ?? new ExpertOpts();
            return st =>
            {
                int d = st.day; string site = st.site; string act = "science";
                if (StormToday(st)) act = "shelter";
                else if (site == "mars" && !opts.neverClean && st.dustDays >= opts.cleanEvery) act = "maintenance";
                var plan = new Plan { activity = act };
                if (st.faults.Count > 0)
                {
                    if (st.crates >= 1) plan.repair = true;
                    else if (!StormToday(st) && !opts.neverClean) plan.activity = "maintenance";
                }
                if (!st.emergencyUsed && st.water < 12 && d > 1) plan.emergency = true;
                if (d == opts.buildDay) plan.build = new List<string>(opts.builds ?? BUILDS[site]);
                plan.ration = opts.ration ?? RationChoice(st, plan.build);
                return ChooseSystems(st, plan, site == "mars" ? 5 : 0);
            };
        }

        public static Func<GameState, Plan> Nothing()
        {
            return st => new Plan { oxygen = false, lab = false, greenhouse = 0, drill = 0, activity = "shelter", ration = "full" };
        }

        public static Func<GameState, Plan> Naive()
        {
            return st =>
            {
                var plan = new Plan { oxygen = true, lab = true, greenhouse = st.mod["greenhouse"], drill = st.mod["drill"], activity = "science", ration = "full" };
                if (st.day == 1) plan.build = L("solar", "solar", "solar", "solar", "solar", "solar", "solar");
                return plan;
            };
        }

        class Tpl { public int w; public List<string> b; }
        public static Func<GameState, Plan> Decent(int seed, string site)
        {
            var r = new Rng(unchecked(seed * 31 + 12345));
            var T = site == "moon" ? new List<Tpl> {
                new Tpl { w = 4, b = L("reactor", "drill", "drill", "greenhouse", "shield") },
                new Tpl { w = 3, b = L("reactor", "drill", "greenhouse", "shield", "shield") },
                new Tpl { w = 1, b = L("reactor", "greenhouse", "shield", "shield", "solar") },
                new Tpl { w = 1, b = L("reactor", "drill", "drill", "solar", "solar") },
                new Tpl { w = 1, b = L("solar", "solar", "battery", "battery", "shield", "shield", "drill") } }
            : new List<Tpl> {
                new Tpl { w = 3, b = L("reactor", "greenhouse", "drill", "shield", "solar") },
                new Tpl { w = 2, b = L("reactor", "greenhouse", "greenhouse", "drill") },
                new Tpl { w = 2, b = L("solar", "solar", "battery", "greenhouse", "drill", "shield") },
                new Tpl { w = 2, b = L("greenhouse", "drill", "shield", "shield", "solar") },
                new Tpl { w = 1, b = L("shield", "shield", "shield", "greenhouse", "drill") } };
            int tot = T.Sum(t => t.w); double x = r.Next() * tot; var pick = T[0];
            foreach (var t in T) { x -= t.w; if (x <= 0) { pick = t; break; } }
            double pShelterMiss = 0.2, pCleanMiss = 0.25, pAllOn = 0.12; bool fullAlways = r.Next() < 0.35;
            return st =>
            {
                int d = st.day; string act = "science";
                if (StormToday(st) && r.Next() > pShelterMiss) act = "shelter";
                else if (st.site == "mars" && st.dustDays >= 6 && r.Next() > pCleanMiss) act = "maintenance";
                var plan = new Plan { activity = act };
                if (st.faults.Count > 0 && r.Next() > 0.3)
                {
                    if (st.crates >= 1) plan.repair = true;
                    else if (!StormToday(st)) plan.activity = "maintenance";
                }
                if (!st.emergencyUsed && st.water < 10 && d > 1 && r.Next() > 0.5) plan.emergency = true;
                if (d == 1) plan.build = new List<string>(pick.b);
                plan.ration = fullAlways ? "full" : RationChoice(st, plan.build);
                if (r.Next() < pAllOn)
                {
                    plan.oxygen = true; plan.lab = true; plan.greenhouse = st.mod["greenhouse"]; plan.drill = st.mod["drill"];
                    return plan;
                }
                return ChooseSystems(st, plan, st.site == "mars" ? 5 : 0);
            };
        }

        public static GameState Play(string site, int seed, Func<GameState, Plan> player)
        {
            var st = Sim.NewGame(site, seed); int guard = 0;
            while (!st.over && guard++ < 40) Sim.RunDay(st, player(st));
            return st;
        }
    }
}
