﻿using System.Reflection;

namespace TownOfZuul
{
    public class Game
    {
        private bool continuePlaying = true;
        private Location? currentLocation;
        private readonly Stack<Location> previousLocations = new();
        private readonly List<FishableLocation> fishableLocations = new();
        private readonly List<CleanableLocation> cleanableLocations = new();
        private uint monthCounter;
        private const uint endingMonth = 13;
        public int PopulationCount { get; private set; }
        public double PopulationHealth { get; private set; }
        public double FoodUnits { get; private set; }
        public bool AlgaeCleanerUnlocked = true;
        public Game()
        {
            CreateLocations();
            //UpdateGame();
            monthCounter = 1;
            PopulationCount = 10;
            PopulationHealth = 0.5;
            FoodUnits = 15.0;
        }

        private void CreateLocations()
        {
            Village? village = new();
            ElderHouse? elderHouse = new();
            Docks? docks = new();
            Ocean? ocean = new();
            ResearchVessel? researchVessel = new(100.0);
            Coast? coast = new(100.0);
            WastePlant? wastePlant = new(100.0);

            village.SetExits(null, docks, coast, elderHouse); // North, East, South, West
            docks.SetExits(researchVessel, ocean, null, village);
            elderHouse.SetExit("east", village);
            researchVessel.SetExit("south", docks);
            ocean.SetExit("west", docks);
            coast.SetExits(village, null, wastePlant, null);
            wastePlant.SetExit("north", coast);

            fishableLocations.AddRange(new List<FishableLocation>() { docks, ocean });
            cleanableLocations.AddRange(new List<CleanableLocation>() { coast, researchVessel, wastePlant });

            currentLocation = village;
        }

        public void Play()
        {
            Parser parser = new();

            Console.WriteLine(currentLocation?.Art);
            PrintWelcome();

            while (continuePlaying)
            {
                Console.WriteLine(currentLocation?.Name);
                Console.Write("> ");

                string? input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Please enter a command.");
                    continue;
                }

                Command? command = parser.GetCommand(input);

                if (command == null)
                {
                    Console.WriteLine("I don't know that command.");
                    continue;
                }

                switch (command.Name)
                {
                    case "look":
                        Console.WriteLine(currentLocation?.Description);
                        break;

                    case "back":
                        if (previousLocations.Count == 0)
                            Console.WriteLine("You can't go back from here!");
                        else
                        {
                            currentLocation = previousLocations.First();
                            previousLocations.Pop();
                            Console.Clear();
                            Console.WriteLine(currentLocation?.Art);
                        }
                        break;

                    case "north":
                    case "south":
                    case "east":
                    case "west":
                        Console.Clear();
                        Move(command.Name);
                        break;

                    case "exit":
                    case "quit":
                        continuePlaying = false;
                        break;

                    case "help":
                        PrintHelp();
                        break;

                    case "talk":
                        Console.WriteLine(currentLocation?.Dialogue);
                        break;

                    case "story":
                        Console.WriteLine(currentLocation?.Story);
                        break;

                    case "info":
                        Console.WriteLine(currentLocation?.Information);
                        break;

                    case "clear":
                        Console.Clear();
                        break;

                    case "assign":
                        if (command.SecondWord == null)
                        {
                            Console.WriteLine("The 'assign' command requires defining a number of villagers to be assigned, i.e. \"assign 5\".");
                            break;
                        }

                        if (uint.TryParse(command.SecondWord, out uint result))
                            currentLocation?.AssignVillagers(result);
                        else
                            Console.WriteLine("\"" + command.SecondWord + "\" is not a valid or accepted number. Please try again.");
                        break;

                    case "unassign":
                        currentLocation?.AssignVillagers(0);
                        break;

                    case "boo":
                        Console.WriteLine(" .-.\n(o o) boo!\n| O \\\n \\   \\\n  `~~~'\n");
                        break;

                    case "xmas":
                        Console.WriteLine("             *\r\n            /.\\\r\n           /..'\\\r\n           /'.'\\\r\n          /.''.'\\\r\n          /.'.'.\\\r\n         /'.''.'.\\\n         ^^^[_]^^^\r\n\r\n");
                        break;

                    case "algae":
                        AlgaeCleanerUnlocked = true;
                        Console.WriteLine("Great, you now have the algae cleaner and can start cleaning the algae.");
                        break;

                    case "sleep":
                        UpdateGame();
                        break;

                    case "close":
                        CloseGame();
                        break;

                    case "report":
                        GetReport();
                        break;

                    default:
                        Console.WriteLine("I don't know what command...");
                        break;
                }
            }

            Console.Clear();
        }

        private void Move(string direction)
        {
            if (currentLocation?.Exits.ContainsKey(direction) == true)
            {
                if (currentLocation?.Name == "Docks" && direction == "east")
                {
                    Docks docks = (Docks)currentLocation;
                    if (!docks.IsOceanUnlocked(PopulationCount))
                    {
                        Console.WriteLine("Nope");
                        return;
                    }
                }

                if (currentLocation != null) previousLocations.Push(currentLocation);
                currentLocation = currentLocation?.Exits[direction];

                Console.Clear();
                Console.WriteLine(currentLocation?.Art);
            }
            else
            {
                Console.Clear();
                Console.WriteLine(currentLocation?.Art);
                Console.WriteLine($"There's nothing of interest towards the {direction}.");
            }
        }

        private void GetReport()
        {
            // TODO: split all these into separate methods, since they'd be useful outside this function also (and probably help w/ encapsulation)
            Console.WriteLine("\n- Report -");

            Console.WriteLine("Population count: " + PopulationCount);
            Console.WriteLine("Population health: " + Math.Round(PopulationHealth * 100, 2) + "%");
            Console.WriteLine("Village food stock: " + FoodUnits);
            Console.WriteLine();

            //Console.WriteLine($"Ocean unlocked: " + (docks != null && docks.OceanUnlocked ? "Yes" : "No"));
            foreach (CleanableLocation cleanableLocation in cleanableLocations)
            {
                Console.WriteLine($"{cleanableLocation.Name} unlocked: " + (cleanableLocation.CleanupUnlocked ? "Yes" : "No"));
                Console.WriteLine($"{cleanableLocation.Name} initial pollution: " + cleanableLocation.InitialPollution);
                Console.WriteLine($"{cleanableLocation.Name} current pollution: " + cleanableLocation.PollutionCount);
            }
            Console.WriteLine();

            Console.WriteLine("Water quality: " + Math.Round(GetWaterQualityPercentage() * 100, 2) + "% pure");
            Console.WriteLine();

            foreach (FishableLocation fishableLocation in fishableLocations)
            {
                Console.WriteLine($"Total fish in the {fishableLocation.Name}: " + fishableLocation.LocalFish.Sum(item => item.Population));
                foreach (Fish fish in fishableLocation.LocalFish)
                {
                    Console.WriteLine("- " + fish.Name + ": " + fish.Population + " (previously " + fish.PreviousPopulation + ")");
                }
                foreach (Fish fish in fishableLocation.LocalFish)
                {
                    Console.WriteLine("- " + fish.Name + " reproduction rate: " + Math.Round(fish.ReproductionRate, 2) + " (previously " + Math.Round(fish.PreviousReproductionRate, 2) + ")");
                }
            }
            Console.WriteLine();

            foreach (FishableLocation fishableLocation in fishableLocations)
            {
                Console.WriteLine($"Villagers fishing in {fishableLocation.Name}: " + fishableLocation.LocalFishers.Sum(item => Convert.ToUInt32(item)));
                for (int i = 0; i < fishableLocation.LocalFishers.Count; i++)
                    Console.WriteLine("- " + fishableLocation.LocalFish[i].Name + " fishers: " + fishableLocation.LocalFishers[i]);
            }

            foreach (CleanableLocation cleanableLocation in cleanableLocations)
            {
                Console.WriteLine($"Villagers cleaning in {cleanableLocation.Name}: " + cleanableLocation.LocalCleaners);
            }
            Console.WriteLine();
        }

        public void AddToFoodStock(double? additionalFood)
        {
            FoodUnits += additionalFood.GetValueOrDefault();
        }
        public double ConsumeFoodStock(double? foodAmount)
        {
            FoodUnits -= foodAmount.GetValueOrDefault();
            double leftovers = FoodUnits;
            if (FoodUnits < 0)
                FoodUnits = 0;
            return leftovers;
        }

        public void UpdatePopulation()
        {
            double leftovers = ConsumeFoodStock(PopulationCount);
            // Population health is updated dependent on food, water quality
            Console.WriteLine($"Leftovers {leftovers}");
            string? input=Console.ReadLine();
            if (leftovers < 0)
            {
                Console.WriteLine($"leftovers<0 {1.0+(leftovers/PopulationCount)}");
                string? input1=Console.ReadLine();
                SetPopulationHealth(1.0 + (leftovers / PopulationCount));
            }
            else
            {
                SetPopulationHealth(1.5); //improves by 50% if all food needs are met
            }
            // Health naturally decreases when water quality < 30%, and improves (slowly) when water quality goes up
            Console.WriteLine($"Natural increase {0.7+(GetWaterQualityPercentage()-0.3)}");
            string? input3=Console.ReadLine();
            SetPopulationHealth(0.7 + (GetWaterQualityPercentage() - 0.3));
            // Population count is updated based on food stock, and existing health
            int newVillagers = (int)(leftovers * (leftovers >= 0 ? PopulationHealth : 1.0));
            if (newVillagers > 100)
                newVillagers = 100;
            PopulationCount += newVillagers;
            if(PopulationCount<0)
                PopulationCount=0;
        }

        public void SetPopulationHealth(double multiplier)
        {
            PopulationHealth *= multiplier;
            if (PopulationHealth < 0.0)
                PopulationHealth = 0.0;
            else if (PopulationHealth > 1.0)
                PopulationHealth = 1.0;
        }

        public static void AdvanceMonth(uint monthCounter)
        {
            if (monthCounter != endingMonth)
            {
                // TODO: replace AdvanceMonth() art
                string advanceArt =
            @"


            Nap time, sleeby eeby
        I am placeholder art, replace me!
            
                                                                     
---------------------------------------------------------
                                                                     
                ";

                string advanceText = "You wrap up the plans for this month and note them down. Tomorrow they will be put into action.\n\n" +
                "Time passes, and eventually, month #" + monthCounter + " arrives.\n" +
                "As you prepare for planning once again, you wonder how the village has kept itself up " +
                "since you last examined it and are eager to find out.\n";

                GenericMenu advancementMenu = new(advanceArt, advanceText);
                advancementMenu.Display();
            }
        }

        public void UpdateGame()
        {
            AdvanceMonth(monthCounter);

            foreach (FishableLocation fishableLocation in fishableLocations)
                AddToFoodStock(fishableLocation.CatchFish());

            foreach (CleanableLocation cleanableLocation in cleanableLocations)
                cleanableLocation.CleanPollution();

            // Update actual fish reproduction rates based on water quality and population (and base repop rate, and biodiversity score)
            foreach (FishableLocation fishableLocation in fishableLocations)
                fishableLocation.UpdateFishPopulation(GetWaterQualityPercentage());

            UpdatePopulation();

            monthCounter++;

            // Check ending here
            if (monthCounter == endingMonth)
            {
                Ending ending = new();
                ending.ShowGoodEnding();
                EndingMenu endingMenu = new();
                endingMenu.Display();
                if (endingMenu.StopGame)
                {
                    continuePlaying = false;
                    return;
                }
            }

            Console.Clear();
            Console.WriteLine(currentLocation?.Art);
        }

        private double GetWaterQualityPercentage()
        {
            double waterPollution = 0;
            foreach (CleanableLocation cleanableLocation in cleanableLocations)
                waterPollution += 0.25 * (cleanableLocation.PollutionCount / cleanableLocation.InitialPollution);
            double waterQuality = 1.0 - waterPollution * 0.5;
            return waterQuality;
        }

        static void CloseGame()
        {
            Console.Clear();
            Console.WriteLine(Program.QuitMessage);
            Environment.Exit(0);
        }


        private static void PrintWelcome()
        {
            Console.WriteLine("Welcome to Town Of Zuul!");
            Console.WriteLine("Type 'help' for a list of commands.");
            //PrintHelp();
            Console.WriteLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("You are a mayor of the town of Zuul.");
            Console.WriteLine("Your job is to manage the town's population and assign villagers from your town to do certain tasks, such as fishing.");
            Console.WriteLine();
            Console.WriteLine("Navigate by typing 'north', 'south', 'east', or 'west'.");
            Console.WriteLine("Type 'look' for more details about your current location.");
            Console.WriteLine("Type 'back' to go to the previous location.");
            Console.WriteLine("Type 'help' to print this message again.");
            Console.WriteLine("Type 'quit' or 'exit' to exit the game.");
            Console.WriteLine("Type 'talk' to have an interaction with any locals.");
            Console.WriteLine("Type 'info' to get more information from your current location.");
            Console.WriteLine("Type 'assign [number]' to assign a specified amount of villagers to your current location (if possible).");
            Console.WriteLine("Type 'sleep' to advance to the next month.");
            Console.WriteLine("Type 'close' to immediately close this application.");
        }
    }
}