using Godot;
using System.Collections.Generic;

namespace NewWorldEvolution.Data
{
    public static class NameGenerator
    {
        private static readonly Dictionary<string, NameData> RaceNames = new Dictionary<string, NameData>
        {
            ["Human"] = new NameData
            {
                MaleNames = new[] { "Alexander", "Benjamin", "Christopher", "Daniel", "Edward", "Frederick", "Gabriel", "Henry", "Isaac", "James", "Kenneth", "Lucas", "Michael", "Nathan", "Oliver", "Patrick", "Quintin", "Robert", "Samuel", "Thomas", "Victor", "William" },
                FemaleNames = new[] { "Alice", "Beatrice", "Catherine", "Diana", "Elizabeth", "Fiona", "Grace", "Helena", "Isabella", "Julia", "Katherine", "Luna", "Margaret", "Natalie", "Olivia", "Penelope", "Quinn", "Rebecca", "Sophia", "Teresa", "Victoria", "Willow" },
                Surnames = new[] { "Ashford", "Blackwood", "Clearwater", "Drakeheart", "Emberly", "Fairwind", "Goldsmith", "Hawthorne", "Ironforge", "Kingsley", "Lightbringer", "Moonwhisper", "Nightfall", "Oakenshield", "Proudhammer", "Quicksilver", "Ravenwood", "Stargazer", "Thornfield", "Valorheart", "Windchaser", "Wyvernbane" }
            },
            ["Goblin"] = new NameData
            {
                MaleNames = new[] { "Grax", "Zik", "Norg", "Krix", "Vex", "Grik", "Zorg", "Nix", "Brak", "Skrunk", "Grex", "Zap", "Grok", "Snix", "Wrex", "Gax", "Zek", "Nark", "Brix", "Skrex" },
                FemaleNames = new[] { "Zixa", "Narga", "Vexia", "Grika", "Zorna", "Nixa", "Braka", "Skunka", "Grexa", "Zapa", "Groka", "Snixa", "Wrexa", "Gaxa", "Zeka", "Narka", "Brixa", "Skrexa", "Grixia", "Zorka" },
                Surnames = new[] { "Boneshard", "Mudcrawler", "Stinkfist", "Ratbane", "Scrapjaw", "Ironteeth", "Backstab", "Poisontooth", "Sneakfoot", "Grimgrin", "Shadowlurk", "Cutthroat", "Slyeye", "Quickblade", "Rustclaw", "Darkwhisper", "Bloodfang", "Nosetweak", "Rageclaw", "Vileheart" }
            },
            ["Spider"] = new NameData
            {
                MaleNames = new[] { "Arachnis", "Venomweaver", "Silkspinner", "Webmaster", "Shadowfang", "Darkweaver", "Nightcrawler", "Poisonsting", "Blackwidow", "Deathspin", "Voidweaver", "Thornspider", "Grimsilk", "Paleweb", "Duskweaver", "Bloodspinner" },
                FemaleNames = new[] { "Arachne", "Silkweave", "Webspinner", "Venomheart", "Shadowsilk", "Darkweb", "Nightweaver", "Poisonweave", "Blackspin", "Deathsilk", "Voidspinner", "Thornweave", "Grimweb", "Palesilk", "Duskspinner", "Bloodweave" },
                Surnames = new[] { "of the Dark Web", "the Silken", "the Venomous", "the Spinner", "the Weaver", "the Crawler", "the Hunter", "the Patient", "the Deadly", "the Swift", "the Silent", "the Ancient", "the Wise", "the Feared", "the Shadowed", "the Eternal" }
            },
            ["Demon"] = new NameData
            {
                MaleNames = new[] { "Baal", "Asmodeus", "Malphas", "Azazel", "Belial", "Mammon", "Belphegor", "Leviathan", "Beelzebub", "Moloch", "Abaddon", "Samael", "Lilith", "Dagon", "Baphomet", "Astaroth", "Paimon", "Buer", "Valac", "Gusion" },
                FemaleNames = new[] { "Lilith", "Jezebel", "Lamia", "Succubia", "Hecate", "Morrigan", "Banshee", "Fury", "Nemesis", "Discord", "Chaos", "Strife", "Wrath", "Malice", "Spite", "Venom", "Torment", "Anguish", "Despair", "Ruin" },
                Surnames = new[] { "the Corruptor", "Soulrender", "Flamebringer", "Darkbane", "Hellborn", "Voidcaller", "Shadowlord", "Doomweaver", "Chaosborn", "Nightterror", "Deathwhisper", "Painbringer", "Soulburner", "Vilehart", "Grimfate", "Dreadlord", "Tormentor", "Destroyer", "Annihilator", "the Eternal" }
            },
            ["Vampire"] = new NameData
            {
                MaleNames = new[] { "Vlad", "Alucard", "Dracula", "Lestat", "Louis", "Armand", "Nicolas", "Marius", "Akasha", "Khayman", "Maharet", "Mekare", "Pandora", "Vittorio", "Santino", "Thorne", "Cyrus", "Gregory", "Antoine", "Raphael" },
                FemaleNames = new[] { "Carmilla", "Lilith", "Selene", "Akasha", "Pandora", "Gabrielle", "Claudia", "Bianca", "Merrick", "Maharet", "Mekare", "Jesse", "Miriam", "Sarah", "Sonja", "Erika", "Kraven", "Amelia", "Antoinette", "Celeste" },
                Surnames = new[] { "Dracul", "Bathory", "Nosferatu", "Tepes", "Corvinus", "Von Carstein", "Bloodthorne", "Nightshade", "Crimsonmoon", "Shadowheart", "Deathwhisper", "Eternus", "Immortalis", "Sanguinarius", "Nocturnalis", "Morteus", "Vampyrus", "Gothicus", "Darkmoore", "Ravencroft" }
            }
        };

        public static string GenerateRandomName(string race, string gender = null)
        {
            if (!RaceNames.ContainsKey(race))
            {
                race = "Human"; // Default fallback
            }

            var nameData = RaceNames[race];
            var random = new System.Random();

            // If no gender specified, choose randomly
            if (string.IsNullOrEmpty(gender))
            {
                gender = random.Next(2) == 0 ? "Male" : "Female";
            }

            string firstName;
            if (gender.ToLower() == "female" && nameData.FemaleNames.Length > 0)
            {
                firstName = nameData.FemaleNames[random.Next(nameData.FemaleNames.Length)];
            }
            else
            {
                firstName = nameData.MaleNames[random.Next(nameData.MaleNames.Length)];
            }

            string surname = nameData.Surnames[random.Next(nameData.Surnames.Length)];

            // For some races, format names differently
            return race switch
            {
                "Spider" or "Demon" => $"{firstName} {surname}",
                "Vampire" => $"{firstName} {surname}",
                _ => $"{firstName} {surname}"
            };
        }

        public static string GeneratePlayerName(string race, string gender = null)
        {
            return GenerateRandomName(race, gender);
        }

        public static string[] GetMaleNames(string race)
        {
            return RaceNames.ContainsKey(race) ? RaceNames[race].MaleNames : RaceNames["Human"].MaleNames;
        }

        public static string[] GetFemaleNames(string race)
        {
            return RaceNames.ContainsKey(race) ? RaceNames[race].FemaleNames : RaceNames["Human"].FemaleNames;
        }

        public static string[] GetSurnames(string race)
        {
            return RaceNames.ContainsKey(race) ? RaceNames[race].Surnames : RaceNames["Human"].Surnames;
        }
    }

    public class NameData
    {
        public string[] MaleNames { get; set; } = System.Array.Empty<string>();
        public string[] FemaleNames { get; set; } = System.Array.Empty<string>();
        public string[] Surnames { get; set; } = System.Array.Empty<string>();
    }
}
