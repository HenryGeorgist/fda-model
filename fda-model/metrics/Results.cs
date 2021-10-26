using System;
using System.Collections.Generic;
using Statistics;
using Statistics.Histograms;
namespace metrics
{
    public class Results: IContainResults
    {
        //needs access to get AEP and EAD results.
        private const double AEP_HISTOGRAM_BINWIDTH = .0001;
        private const double EAD_HISTOGRAM_BINWIDTH = 10;
        private const double CNEP_HISTOGRAM_BINWIDTH = .0001;
        private double _aepThreshold; //I think we should name refactor this, because the threshold applies in the calculation of all performance metrics
        // the only one that might be different is assurance of AEP, but I think we'll just report assurance of AEP like we do CNEP, that is what we set up in the design document
        private Histogram _aep =  null;
        private Dictionary<string, Histogram> _ead; 
        private Dictionary<double, Histogram> _cnep;
        

        public double AEPThreshold { 
            get {
                return _aepThreshold;
            } 
            set {
            _aepThreshold = value;
            }
        }
        public Results(){
            _aepThreshold = 0.0;
            _aep = null; //is this necessary?
            _ead = new Dictionary<string, Histogram>();
            _cnep = new Dictionary<double, Histogram>();
        }
        public void AddAEPEstimate(double aepEstimate)
        {
            double[] data = new double[1] { aepEstimate };
            IData aep = IDataFactory.Factory(data);
            if (_aep!=null)
            {
                _aep.AddObservationToHistogram(aep);
            } else
            {
                var histo = new Histogram(aep, AEP_HISTOGRAM_BINWIDTH);
                _aep = histo;
            }
            

        }

        public void AddStageForCNEP(double standardProbability, double stageForCNEP)
        {
            double[] data = new double[1] {stageForCNEP};
            IData stage = IDataFactory.Factory(data);
            if (_cnep.ContainsKey(standardProbability))
            {
                _cnep[standardProbability].AddObservationToHistogram(stage);
            } else
            {
                var histo = new Histogram(stage,CNEP_HISTOGRAM_BINWIDTH);
                _cnep.Add(standardProbability,histo);
            }
        }

        public double MeanAEP()
        {
            return _aep.Mean;
        }

        public double MedianAEP()
        {
            return _aep.Median;
        }

        public double[] AssuranceOfAEP()
        {
            double[] standardExceedanceProbabilities = new double[8] {.5, .2, .1, .04, .02, .01, .004, .002};
            double[] assuranceOfAEP = new double[8];
            for (int i=0; i<standardExceedanceProbabilities.Length; i++)
            {
                assuranceOfAEP[i] = _aep.CDF(standardExceedanceProbabilities[i]);
            }
            return assuranceOfAEP;
        }

        public double[] ConditionalNonExceedanceProbability()
        {
            double[] standardProbabilities = new double[8] {.5, .2, .1, .04, .02, .01, .004, .002};
            double[] conditionalNonExceedanceProbability = new double[8];
            for (int i=0; i<standardProbabilities.Length; i++)
            {
                conditionalNonExceedanceProbability[i] = 1-_cnep[standardProbabilities[i]].CDF(_aepThreshold);
            }
            return conditionalNonExceedanceProbability;
        }

        public void AddEADEstimate(double eadEstimate, string category)
        {
            double[] data = new double[1] { eadEstimate };
            IData ead = IDataFactory.Factory(data);
            if (_ead.ContainsKey(category))
            {
                _ead[category].AddObservationToHistogram(ead); 
            }
            else
            {
                var histo = new Histogram(ead, EAD_HISTOGRAM_BINWIDTH);
                _ead.Add(category, histo);
            }
        }
        public double MeanEAD(string category)
        {
            return _ead[category].Mean;
        }

        public double[] EADExceededWithProbabilityQ(string category)
        {
            double[] quartiles = new double[3];
            quartiles[0] = _ead[category].InverseCDF(0.75);
            quartiles[1] = _ead[category].InverseCDF(0.5);
            quartiles[2] = _ead[category].InverseCDF(0.25);

            return quartiles;
        }

        public double[] LongTermRisk()
        {
            double[] longTermPeriod = new double[3] {10, 25, 50};
            double[] longTermRisk =  new double[3];
            for (int i = 0; i < longTermPeriod.Length; i++)
            {
                longTermRisk[i] = 1-Math.Pow((1-MeanAEP()),longTermPeriod[i]);       
            }
            return longTermRisk;
        }


    }
}