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

        Console.WriteLine(failures > 0 ? "\n" + failures + " check(s) FAILED" : "\nAll checks passed");
        return failures > 0 ? 1 : 0;
    }
}
