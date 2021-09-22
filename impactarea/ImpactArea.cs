using System;
namespace impactarea
{
    public class ImpactArea
    {
        private String _name;
        private ead.Simulation _simulation;
        //probably need getters and setters
        public ImpactArea(String name, ead.Simulation sim){
            _name = name;
            _simulation = sim;
        }
        public double Compute(Int64 seed, Int64 iterations){
            return _simulation.Compute(seed,iterations);
        }

    }
}