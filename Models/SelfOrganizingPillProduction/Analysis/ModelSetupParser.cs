using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    class ModelSetupParser
    {
        private readonly List<Station> stations = new List<Station>();
        private readonly Dictionary<string, Station> namedStations = new Dictionary<string, Station>();

        public Model Parse(string fileName)
        {
            var lines = File.ReadAllLines(fileName);
            int lineIndex;

            // read model setup
            for (lineIndex = 0; lineIndex < lines.Length && lines[lineIndex] != ""; ++lineIndex)
            {
                int currentPosition = 0;
                var input = lines[lineIndex];

                // read alternating sequence of station identifiers and connections
                Station lastStation = ReadStation(input, ref currentPosition);
                while (currentPosition < input.Length)
                {
                    var isTwoWay = ReadConnection(input, ref currentPosition);
                    var station = ReadStation(input, ref currentPosition);
                    Connect(lastStation, station);
                    if (isTwoWay)
                        Connect(station, lastStation);
                    lastStation = station;
                }
            }
            var model = new Model(stations.ToArray(), new MiniZincObserverController(stations.ToArray()));

            // read recipes to be produced by the model
            var ingredientNames = Enum.GetNames(typeof(IngredientType));
            var ingredientTypes = (IngredientType[])Enum.GetValues(typeof(IngredientType));
            for (lineIndex++; lineIndex < lines.Length; ++lineIndex)
            {
                var input = lines[lineIndex];
                int currentPosition = 0;

                var amount = ReadUntil(input, ref currentPosition, ' ');
                var ingredients = new List<Ingredient>();
                while (currentPosition < input.Length)
                {
                    ingredients.Add(ReadIngredient(input, ref currentPosition));
                }

                model.ScheduleProduction(new Recipe(ingredients.ToArray(), uint.Parse(amount)));
            }

            return model;
        }

        private static Regex noSpaceRegex = new Regex(@"[^\s]");
        private void ReadSpace(string input, ref int currentPosition)
        {
            var match = noSpaceRegex.Match(input, currentPosition);
            currentPosition = match.Success ? match.Index : input.Length;
        }

        private static Regex stationRegex = new Regex(@"^([PDC])(\d+)?");
        private Station ReadStation(string input, ref int currentPosition)
        {
            ReadSpace(input, ref currentPosition);

            Station station;
            var type = input[currentPosition++];

            string name = ReadUntil(input, ref currentPosition, ' ');
            name = (name == "") ? null : (type + name);

            if (name == null || !namedStations.TryGetValue(name, out station))
                station = CreateStation(type, name);

            ReadSpace(input, ref currentPosition);
            return station;
        }

        private Station CreateStation(char type, string name)
        {
            Station station;
            switch (type)
            {
                case 'P':
                    station = new ContainerLoader();
                    break;
                case 'C':
                    station = new PalletisationStation();
                    break;
                case 'D':
                    station = new ParticulateDispenser();
                    foreach (IngredientType ingredient in Enum.GetValues(typeof(IngredientType)))
                    {
                        (station as ParticulateDispenser).SetStoredAmount(ingredient, Model.InitialIngredientAmount);
                    }
                    break;
                default: throw new InvalidDataException($"invalid type: {type}");
            }

            stations.Add(station);
            if (name != null)
                namedStations.Add(name, station);

            return station;
        }

        private bool ReadConnection(string input, ref int currentPosition)
        {
            ReadSpace(input, ref currentPosition);

            bool isTwoWay = input[currentPosition] == '<';
            currentPosition += isTwoWay ? 3 : 2; // <-> or ->

            ReadSpace(input, ref currentPosition);
            return isTwoWay;
        }

        private void Connect(Station input, Station output)
        {
            input.Outputs.Add(output);
            output.Inputs.Add(input);
        }

        private Ingredient ReadIngredient(string input, ref int currentPosition)
        {
            ReadSpace(input, ref currentPosition);

            string typeName = ReadUntil(input, ref currentPosition, '(');
            currentPosition++;

            string amount = ReadUntil(input, ref currentPosition, ')');
            currentPosition++;

            var type = (IngredientType)Enum.Parse(typeof(IngredientType), typeName + "Particulate");
            var ingredient = new Ingredient(type, uint.Parse(amount));

            ReadSpace(input, ref currentPosition);
            return ingredient;
        }

        private string ReadUntil(string input, ref int currentPosition, char endChar)
        {
            int endPosition = input.IndexOf(endChar, currentPosition);
            if (endPosition == -1)
                endPosition = input.Length;

            var value = input.Substring(currentPosition, endPosition - currentPosition);
            currentPosition = endPosition;
            return value;
        }
    }
}
