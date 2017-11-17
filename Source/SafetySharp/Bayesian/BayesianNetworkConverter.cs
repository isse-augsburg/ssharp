namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ISSE.SafetyChecking.Modeling;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class BayesianNetworkConverter : JsonConverter
    {
        private readonly IList<RandomVariable> _randomVariables; 
        private const string DagProperty = "dag";
        private const string EdgesProperty = "edges";
        private const string NodesProperty = "nodes";
        private const string RandomVariableProperty = "randomVariable";
        private const string ConditionsProperty = "conditions";
        private const string DistributionProperty = "distribution";
        private const string DistributionsProperty = "distributions";

        public BayesianNetworkConverter(IList<RandomVariable> randomVariables)
        {
            _randomVariables = randomVariables;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var network = (BayesianNetwork)value;
            var json = new JObject();

            // write DagPattern
            var dag = new JObject
            {
                new JProperty(EdgesProperty, network.Dag.Edges),
                new JProperty(NodesProperty, network.Dag.Nodes.Select(node => node.Name).ToList())
            };
            json.Add(new JProperty(DagProperty, dag));

            // write probability distributions
            var probObjects = new JArray();
            foreach (var distribution in network.Distributions)
            {
                var distObject = new JObject
                {
                    new JProperty(RandomVariableProperty, distribution.Value.RandomVariable.Name),
                    new JProperty(ConditionsProperty, distribution.Value.Conditions.Select(rvar => rvar.Name)),
                    new JProperty(DistributionProperty, distribution.Value.Distribution.Select(prob => prob.Value))
                };
                probObjects.Add(distObject);
            }
            json.Add(new JProperty(DistributionsProperty, probObjects));

            json.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var randomVariableNames = _randomVariables.Select(rvar => rvar.Name).ToList();
            var randomVariableMapping = new Dictionary<string, RandomVariable>();
            foreach (var randomVariable in _randomVariables)
            {
                randomVariableMapping[randomVariable.Name] = randomVariable;
            }
            var json = JObject.Load(reader);

            // construct DagPattern
            var dag = json.Property(DagProperty);
            var edges = dag.Value.Value<JArray>(EdgesProperty).ToObject<int[]>();
            var nodes = dag.Value.Value<JArray>(NodesProperty).ToObject<string[]>();
            var realEdges = new int[nodes.Length, nodes.Length];
            for(var i = 0; i < nodes.Length; i++)
            {
                for (var j = 0; j < nodes.Length; j++)
                {
                    // given random variables could be in another order, so lookup the index
                    realEdges[randomVariableNames.IndexOf(nodes[i]), randomVariableNames.IndexOf(nodes[j])] = edges[i*nodes.Length + j];
                }
            }
            var dagPattern = DagPattern<RandomVariable>.InitDagWithMatrix(_randomVariables, realEdges);

            // construct probability distributions
            var distributions = json.Property(DistributionsProperty).Value.Children();
            var realDistributions = new List<ProbabilityDistribution>();
            foreach (var distribution in distributions)
            {
                var randomVariable = distribution.Value<string>(RandomVariableProperty);
                var realRandomVariable = randomVariableMapping[randomVariable];
                var conditions = distribution.Value<JArray>(ConditionsProperty).ToObject<string[]>();
                var realConditions = conditions.Select(condition => randomVariableMapping[condition]).ToList();
                var distributionValues = distribution.Value<JArray>(DistributionProperty).ToObject<double[]>();
                var realDistributionValues = distributionValues.Select(distValue => new Probability(distValue)).ToList();
                realDistributions.Add(new ProbabilityDistribution(realRandomVariable, realConditions, realDistributionValues));
            }

            return BayesianNetwork.FromDagPattern(dagPattern, realDistributions);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BayesianNetwork);
        }

        
    }
}