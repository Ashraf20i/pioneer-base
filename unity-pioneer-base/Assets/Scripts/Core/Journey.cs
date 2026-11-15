// Pioneer Base - Journey.cs
// Hard mode ("Crash Landing"): the crew drives a rover 40 km to the outpost before the 30-day mission.
// Pure logic, no UnityEngine dependency. All journey numbers are Simplified game values, except the
// crew needs (oxygen, food, water), which reuse the Real/Derived values of GameData.
using System;
using System.Collections.Generic;
using static PioneerBase.GameData;

namespace PioneerBase
{
    public class JourneyState
    {
        public double o2 = 16, water = 55, food = 40, health = 100, battery = 100, traveledKm = 0;
        public int sol = 1, crates = 9, pods = 0, right = 0, asked = 0;
        public bool arrived, failed; public string failReason;
        public List<string> log = new List<string>();
    }

    public class TechQuestion
    {
        public string topic, text, why; public string[] options; public int answer;
    }

    public static class Journey
    {
        public const double TotalKm = 40;           // crash site to outpost (Simplified)
        public const double KmPerCharge = 10.5;     // distance on a full battery, firm ground (Simplified)
        public const double SandFactor = 2;         // soft sand doubles the power use (Simplified)
        public const double StormRecharge = 35;     // % recharged on a dust-storm night (Simplified)
        public const int StormSol = 3;              // the dust storm arrives on this sol (Simplified)
        public const double CrewWaterNoRecycler = 12; // L/day: the rover has no water recycler (Derived: 4 x 3 L)

        public static double O2PerSol { get { return 4 * V("o2_person"); } }
        public static double FoodPerSol { get { return 4 * V("food_person"); } }
        public static double KmLeft(JourneyState j) { return Math.Max(0, TotalKm - j.traveledKm); }
        public static double SolsOfOxygen(JourneyState j) { return j.o2 / O2PerSol; }

        // Drive a distance. Returns the distance actually driven (limited by the battery).
        public static double Drive(JourneyState j, double km, bool sand)
        {
            if (j.arrived || j.failed || km <= 0) return 0;
            double costPerKm = 100.0 / KmPerCharge * (sand ? SandFactor : 1);
            double can = j.battery / costPerKm; double d = Math.Min(km, can);
            j.battery = Math.Max(0, j.battery - d * costPerKm);
            return d;
        }

        // One night in the rover: the crew uses a day of supplies and the panels recharge the battery.
        public static void Camp(JourneyState j, double bonusPct = 0)
        {
            if (j.arrived || j.failed) return;
            bool storm = j.sol == StormSol;
            double lost = 0;
            if (j.o2 >= O2PerSol) j.o2 -= O2PerSol; else { j.o2 = 0; lost += -V("pen_o2"); j.log.Add("Sol " + j.sol + ": the oxygen ran out."); }
            if (j.water >= CrewWaterNoRecycler) j.water -= CrewWaterNoRecycler; else { j.water = 0; lost += -V("pen_water"); j.log.Add("Sol " + j.sol + ": the water ran out."); }
            if (j.food >= FoodPerSol) j.food -= FoodPerSol; else { j.food = 0; lost += -V("pen_food"); j.log.Add("Sol " + j.sol + ": the food ran out."); }
            j.health = Math.Max(0, j.health - lost);
            j.battery = Math.Min(100, j.battery + (storm ? StormRecharge : 100) + bonusPct);
            if (storm) j.log.Add("Sol " + j.sol + ": a dust storm cut the recharge to " + StormRecharge + "%.");
            j.sol++;
            if (j.health <= 0) { j.failed = true; j.failReason = "health"; }
        }

        public static readonly string[] PodTypes = { "oxygen", "water", "battery", "crates", "food" };
        public static string ApplyPod(JourneyState j, string type, bool full)
        {
            j.pods++; double k = full ? 1 : 0.5;
            switch (type)
            {
                case "oxygen": j.o2 += 3.4 * k; return "+" + (3.4 * k).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " kg oxygen";
                case "water": j.water += 12 * k; return "+" + (12 * k) + " L water";
                case "battery": j.battery = Math.Min(100, j.battery + 35 * k); return "+" + (35 * k).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "% battery";
                case "crates": { int n = full ? 2 : 1; j.crates = Math.Min(14, j.crates + n); return "+" + n + " supply crate" + (n > 1 ? "s" : ""); }
                default: j.food += 8 * k; return "+" + (8 * k) + " kg food";
            }
        }

        // The outpost has its own reserves; what the crew brings is added to them.
        public const double BaseO2 = 8, BaseWater = 28, BaseFood = 90;
        public static GameState ToGameState(JourneyState j, int seed)
        {
            var st = Sim.NewGame("mars", seed);
            st.o2 = Math.Min(V("o2_cap"), j.o2 + BaseO2);
            st.water = Math.Min(V("water_cap"), j.water + BaseWater);
            st.food = Math.Min(V("food_cap"), j.food + BaseFood);
            st.health = Math.Max(1, Math.Min(V("health_start"), j.health));
            st.crates = j.crates;
            return st;
        }

        // Technical challenges. Every answer can be checked with the numbers in GameData or by simple arithmetic.
        public static readonly List<TechQuestion> Questions = new List<TechQuestion>
        {
            new TechQuestion { topic = "PHYSICS: RADIO DELAY", text = "Radio travels at about 300,000 km per second. Mars is 180 million km from Earth today. How long does one message take?",
                options = new[] { "About 10 seconds", "About 10 minutes", "About 10 hours" }, answer = 1, why = "Time = distance / speed = 180,000,000 / 300,000 = 600 seconds = 10 minutes. Mission Control cannot help in an emergency: the crew must decide." },
            new TechQuestion { topic = "PHYSICS: ENERGY", text = "The rover battery stores 12 kWh. The motor uses 3 kW. How long can it drive?",
                options = new[] { "2 hours", "4 hours", "9 hours" }, answer = 1, why = "Energy = power x time, so time = 12 kWh / 3 kW = 4 hours." },
            new TechQuestion { topic = "PHYSICS: MOTION", text = "The rover drives at 8 km/h. A supply pod is 24 km away. How long is the drive?",
                options = new[] { "2 hours", "3 hours", "4 hours" }, answer = 1, why = "Time = distance / speed = 24 km / 8 km/h = 3 hours." },
            new TechQuestion { topic = "CHEMISTRY: ELECTROLYSIS", text = "Electricity splits water: 2 H2O -> 2 H2 + O2. How much water makes 4 kg of oxygen?",
                options = new[] { "2 kg", "4.5 kg", "8 kg" }, answer = 1, why = "36 g of water gives 32 g of oxygen, so each kg of oxygen needs 1.125 kg of water. 4 x 1.125 = 4.5 kg." },
            new TechQuestion { topic = "CHEMISTRY: COMBUSTION", text = "Hydrogen from rocket fuel reacts with oxygen: 2 H2 + O2 -> ?",
                options = new[] { "2 H2O (water)", "H2O2 (peroxide)", "CO2 (carbon dioxide)" }, answer = 0, why = "Two hydrogen molecules and one oxygen molecule make two water molecules. This is how fuel can become drinking water." },
            new TechQuestion { topic = "PHYSICS: LIGHT", text = "Mars is 1.524 times farther from the Sun than Earth. How strong is sunlight there, compared with Earth's distance?",
                options = new[] { "About 66%", "About 43%", "About 15%" }, answer = 1, why = "Light spreads out with distance squared: 1 / (1.524 x 1.524) = 0.43. Solar panels make less than half the power." },
            new TechQuestion { topic = "LIFE SUPPORT", text = "One astronaut breathes 0.84 kg of oxygen a day. How much do four astronauts need for 5 days?",
                options = new[] { "4.2 kg", "16.8 kg", "33.6 kg" }, answer = 1, why = "0.84 x 4 astronauts x 5 days = 16.8 kg." },
            new TechQuestion { topic = "LIFE SUPPORT", text = "The crew uses 12 L of water a day. A recycler recovers 90% of it. How much new water is needed each day?",
                options = new[] { "1.2 L", "6 L", "10.8 L" }, answer = 0, why = "90% comes back, so only 10% is lost: 12 x 0.10 = 1.2 L. The rover has no recycler, so it needs the full 12 L." },
            new TechQuestion { topic = "PHYSICS: RADIATION", text = "The Mars surface gives about 0.67 mSv of radiation a day. What is the dose after 30 days?",
                options = new[] { "About 2 mSv", "About 20 mSv", "About 67 mSv" }, answer = 1, why = "0.67 x 30 = 20.1 mSv. That is why shielding and sheltering matter." },
            new TechQuestion { topic = "PHYSICS: SHIELDING", text = "Each shield layer lets 75% of the radiation through. How much gets through two layers?",
                options = new[] { "50%", "About 56%", "25%" }, answer = 1, why = "The effects multiply: 0.75 x 0.75 = 0.5625, about 56%." },
            new TechQuestion { topic = "PHYSICS: PRESSURE", text = "Mars air is under 1% as thick as Earth's. What does a fast Martian wind feel like?",
                options = new[] { "It can flip the rover", "A very gentle push", "Exactly like on Earth" }, answer = 1, why = "Thin air carries little force. The real danger of a storm is the dust that blocks sunlight." },
            new TechQuestion { topic = "PHYSICS: POWER", text = "A clean solar panel makes 10 units of energy. Dust now blocks 30% of the light. What does it make?",
                options = new[] { "3 units", "7 units", "13 units" }, answer = 1, why = "30% is lost, 70% is left: 10 x 0.70 = 7 units. Cleaning the panel brings it back." },
            new TechQuestion { topic = "CHEMISTRY: MARS AIR", text = "The Mars air is mostly CO2. Which element can an oxygen plant take from it for the crew to breathe?",
                options = new[] { "Nitrogen", "Oxygen", "Helium" }, answer = 1, why = "CO2 is one carbon atom and two oxygen atoms. NASA's MOXIE test pulled oxygen out of Martian CO2." },
        };
    }
}
