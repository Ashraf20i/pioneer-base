// Balance checks for the C# port, same bands as tests/balance.test.js. Run: dotnet run --project csharp-tests
using System;
using System.Collections.Generic;
using System.Linq;
using PioneerBase;

static class Program
{
    const int N = 200;
    static int failures;

    static void Check(string label, bool cond, string detail)
    {
        Console.WriteLine("  " + (cond ? "PASS" : "FAIL") + "  " + label + "  " + detail);
        if (!cond) failures++;
    }
    static string Pct(double x) { return (100.0 * x / N).ToString("0.0") + "%"; }

    static (int evac, int win, int three) Run(string site, Func<int, Func<GameState, Plan>> mk)
    {
        int evac = 0, win = 0, three = 0;
        for (int s = 1; s <= N; s++)
        {
            var st = Players.Play(site, s, mk(s));
            if (st.ending.type == "success") { win++; if (st.ending.stars == 3) three++; } else evac++;
        }
        return (evac, win, three);
    }

    static void SearchPlans(string site, string[] allowed, ExpertOpts baseOpts, string label)
    {
        var r = new Rng(2026); int wins = 0, games = 0;
        for (int i = 0; i < 40; i++)
        {
            var b = new List<string>(); int n = 3 + (int)Math.Floor(r.Next() * 5);
            for (int k = 0; k < n; k++) b.Add(allowed[(int)Math.Floor(r.Next() * allowed.Length)]);
            for (int s = 1; s <= 10; s++)
            {
                var o = new ExpertOpts { builds = b, neverClean = baseOpts.neverClean };
                var st = Players.Play(site, s, Players.Expert(o));
                games++; if (st.ending.type == "success") wins++;
            }
        }
        Check(label, wins == 0, wins + " of " + games + " games completed");
    }

    static JourneyState RunJourney(double detour, double sandShare, string[] pods)
    {
        var j = new JourneyState(); int pi = 0, guard = 0;
        while (!j.arrived && !j.failed && guard++ < 14)
        {
            double want = 30;                                  // drive until the battery is empty
            double d = Journey.Drive(j, want * sandShare, true) + Journey.Drive(j, want, false);
            j.traveledKm += d * (1 - detour);
            if (pi < pods.Length) Journey.ApplyPod(j, pods[pi++], true);
            if (j.traveledKm >= Journey.TotalKm) { j.arrived = true; break; }
            Journey.Camp(j);
        }
        return j;
    }
    static readonly string[][] HardBuilds = {
        new[] { "reactor", "drill", "shield", "greenhouse", "solar" }, new[] { "reactor", "greenhouse", "drill", "shield" },
        new[] { "reactor", "shield", "drill", "drill" }, new[] { "greenhouse", "drill", "shield", "solar", "solar", "battery" },
        new[] { "reactor", "drill", "greenhouse" }, new[] { "reactor", "drill", "drill", "shield" } };
    static double BaseWinRate(JourneyState j)
    {
        double best = 0;
        foreach (var b in HardBuilds)
        {
            int wins = 0;
            for (int s = 1; s <= N; s++)
            {
                var st = Journey.ToGameState(j, s); var bot = Players.Expert(new ExpertOpts { builds = b.ToList() }); int guard = 0;
                while (!st.over && guard++ < 40) Sim.RunDay(st, bot(st));
                if (st.ending.type == "success") wins++;
            }
            best = Math.Max(best, wins / (double)N);
        }
        return best;
    }

    static int Main()
    {
        foreach (var site in new[] { "moon", "mars" })
        {
            Console.WriteLine("\n" + site.ToUpper() + " (" + N + " seeds)");
            var a = Run(site, s => Players.Nothing());
            Check("Does nothing: evacuated in 100%", a.evac == N, "evacuated " + Pct(a.evac));
            var b = Run(site, s => Players.Naive());
            Check("Naive: evacuated in at least 90%", b.evac >= 0.9 * N, "evacuated " + Pct(b.evac));
            var c = Run(site, s => Players.Decent(s, site));
            Check("Decent: completes in 40-90%", c.win >= 0.4 * N && c.win <= 0.9 * N, "completed " + Pct(c.win));
            var d = Run(site, s => Players.Expert());
            Check("Expert: completes in at least 95%", d.win >= 0.95 * N, "completed " + Pct(d.win));
            Check("Expert: 3 stars in at least 50%", d.three >= 0.5 * N, "3 stars " + Pct(d.three));
        }
        Console.WriteLine("\nImpossible plans");
        SearchPlans("moon", new[] { "solar", "battery", "drill", "greenhouse" }, new ExpertOpts(), "Moon, no reactor and no shielding: cannot complete");
        SearchPlans("moon", new[] { "solar", "battery", "drill", "greenhouse", "shield" }, new ExpertOpts(), "Moon, shielding but no reactor: cannot complete");
        SearchPlans("moon", new[] { "reactor", "solar", "battery", "drill", "greenhouse" }, new ExpertOpts(), "Moon, reactor but no shielding: cannot complete");
        SearchPlans("mars", new[] { "solar", "battery", "drill", "greenhouse", "shield" }, new ExpertOpts { neverClean = true }, "Mars, ignores dust: cannot complete");
        int w = 0;
        for (int s = 1; s <= N; s++) if (Players.Play("mars", s, Players.Naive()).ending.type == "success") w++;
        Check("Mars, solar only, never cleans: cannot complete", w == 0, w + " of " + N + " games completed");

        // Debrief smoke test: every ending must give a headline, 3+ chain steps, tradeoff and tip, and never say "death".
        int bad = 0;
        foreach (var site in new[] { "moon", "mars" })
            for (int s = 1; s <= 50; s++)
                foreach (var mk in new Func<Func<GameState, Plan>>[] { () => Players.Nothing(), () => Players.Naive(), () => Players.Decent(s, site), () => Players.Expert() })
                {
                    var st = Players.Play(site, s, mk());
                    var db = Debrief.Analyze(st);
                    var all = db.headline + db.tradeoff + db.tip + string.Join(" ", db.chain.Select(x => x.text));
                    if (string.IsNullOrEmpty(db.headline) || string.IsNullOrEmpty(db.tradeoff) || string.IsNullOrEmpty(db.tip) || db.chain.Count < 3 || all.ToLower().Contains("dead") || all.ToLower().Contains("death") || all.ToLower().Contains("die"))
                        bad++;
                }
        Check("Debrief: complete and never uses banned words", bad == 0, bad + " bad debriefs");

        // ---------------- Hard mode: the rover journey, then the base with what is left
        Console.WriteLine(); Console.WriteLine("Hard mode (Crash Landing)");
        var careful = RunJourney(0.0, 0.0, new string[0]);
        var good = RunJourney(0.12, 0.05, new[] { "oxygen", "water", "crates", "battery", "crates" });
        var careless = RunJourney(0.25, 0.35, new string[0]);
        foreach (var kv in new[] { ("careful", careful), ("good", good), ("careless", careless) })
            Console.WriteLine("  journey " + kv.Item1 + ": arrived=" + kv.Item2.arrived + " sols=" + kv.Item2.sol + " o2=" + kv.Item2.o2.ToString("0.0") + " water=" + kv.Item2.water.ToString("0.0") + " food=" + kv.Item2.food.ToString("0.0") + " health=" + kv.Item2.health + " crates=" + kv.Item2.crates);
        Check("Journey: a straight, careful drive arrives with full health", careful.arrived && careful.health >= 100, "sol " + careful.sol);
        Check("Journey: collecting pods arrives with more crates", good.arrived && good.crates > careful.crates, good.crates + " crates");
        Check("Journey: a careless drive costs health", careless.failed || careless.health < 100, "health " + careless.health + (careless.failed ? " (evacuated)" : ""));
        double wCareful = BaseWinRate(careful), wGood = BaseWinRate(good);
        Check("Hard base after a careful journey: winnable (at least 20%)", wCareful >= 0.20, (100 * wCareful).ToString("0.0") + "% completed");
        Check("Hard base after a good journey: at least as winnable", wGood >= wCareful, (100 * wGood).ToString("0.0") + "% completed");
        if (!careless.failed) Console.WriteLine("  (info) base after a careless journey: " + (100 * BaseWinRate(careless)).ToString("0.0") + "% completed");
        Check("Tech questions: 12 questions, each with 3 options and a valid answer", Journey.Questions.Count == 12 && Journey.Questions.All(q => q.options.Length == 3 && q.answer >= 0 && q.answer < 3 && q.why.Length > 10), Journey.Questions.Count + " questions");

        Console.WriteLine(failures > 0 ? "\n" + failures + " check(s) FAILED" : "\nAll checks passed");
        return failures > 0 ? 1 : 0;
    }
}
