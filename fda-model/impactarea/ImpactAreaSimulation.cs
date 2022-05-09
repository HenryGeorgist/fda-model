using Statistics;
using System;
namespace impactarea
{
    public class ImpactAreaSimulation
    {
        public string Name { get; }
        public int ID { get; }
        public compute.ImpactAreaScenarioSimulation Simulation { get; }
        public ImpactArea ImpactArea { get; }
        /// <summary>
        /// The impact area scenario consists of a simulation, the name of the impact area simulation, and an ID
        /// </summary>
        /// <param name="name"></param>
        /// <param name="simulation"></param>
        /// <param name="id"></param> The ID should be the ID of the impact area 
        public ImpactAreaSimulation(String name, compute.ImpactAreaScenarioSimulation simulation, int id, ImpactArea impactArea){
            Name = name;
            Simulation = simulation;
            ID = id;
            ImpactArea = impactArea;
        }
        public metrics.ImpactAreaScenarioResults Compute(interfaces.IProvideRandomNumbers randomProvider, Int64 iterations){
            ConvergenceCriteria convergenceCriteria = new ConvergenceCriteria(minIterations: 1, maxIterations: iterations);
            return Simulation.Compute(randomProvider,convergenceCriteria);
        }

    }
}