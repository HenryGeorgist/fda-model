using System;
using System.Collections.Generic;
using scenarios;
using System.Linq;
using Statistics.Histograms;
using metrics;

namespace alternatives
{
    public class Alternative
    {
        private Scenario _currentYear;
        private Scenario _futureYear;
        private Int64 _periodOfAnalysis;
        private double _discountRate;
        private int _id;

        public int ID
        {
            get { return _id; }
        }

        public Scenario CurrentYearScenario
        {
            get { return _currentYear;  }
        }

        public Scenario FutureYearScenario
        {
            get { return _futureYear; }
        }


        public Alternative(Scenario currentYear, Scenario futureYear, Int64 periodOfAnalysis, int id){
            _currentYear = currentYear;
            _futureYear = futureYear;
            _periodOfAnalysis = periodOfAnalysis;
            _id = id;
        }
        /// <summary>
        /// Annualization Compute takes the distributions of EAD in each of the Scenarios for a given Alternative and returns a 
        /// nested dictionary. The first dictionary consists of a key of type int which is an impact area ID and a value which is itself a 
        /// dictionary of damage results. This internal dictionary consists of a key of type string which is a damage category
        /// and a value which is a histogram of damage in average annual equivalent terms.
        /// </summary>
        /// <param name="randomProvider"></param> random number provider
        /// <param name="iterations"></param> number of iterations to sample distributions
        /// <param name="discountRate"></param> Discount rate should be provided in decimal form.
        /// <returns></returns>
        public Dictionary<int,Dictionary<string,Histogram>> AnnualizationCompute(interfaces.IProvideRandomNumbers randomProvider, Int64 iterations, double discountRate)
        {
            _discountRate = discountRate;
            List<Results> baseYearResults = _currentYear.Compute(randomProvider, iterations);//this is a list of impact area-specific ead
            List<Results> mlfYearResults = _futureYear.Compute(randomProvider, iterations);

            Dictionary<int, Dictionary<string, Histogram>> damageByImpactAreas = new Dictionary<int, Dictionary<string, Histogram>>();

            foreach (Results baseYearResult in baseYearResults)
            {
                foreach (Results mlfYearResult in mlfYearResults)
                {
                    if (baseYearResult.ImpactAreaID == mlfYearResult.ImpactAreaID)
                    {

                    }
                }

                Dictionary<string, Histogram> damageByDamageCategories = new Dictionary<string, Histogram>();
                foreach (string damageCategory in baseYearResults[result].DamageResults.HistogramsOfEADs.Keys)
                {
                    //Sturges rule 
                    double lowerBoundProbability = 0.0001;
                    double upperBoundProbability = 0.9999;
                    baseYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].ForceDeQueue();
                    double eadSampledBaseYearLowerBound = baseYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].InverseCDF(lowerBoundProbability);
                    mlfYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].ForceDeQueue();
                    double eadSampledFutureYearLowerBound = mlfYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].InverseCDF(lowerBoundProbability);
                    double eadSampledBaseYearUpperBound = baseYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].InverseCDF(upperBoundProbability);
                    double eadSampledFutureYearUpperBound = mlfYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].InverseCDF(upperBoundProbability);
                    double aaeqDamageLowerBound = ComputeEEAD(eadSampledBaseYearLowerBound, eadSampledFutureYearLowerBound);
                    double aaeqDamageUpperBound = ComputeEEAD(eadSampledBaseYearUpperBound, eadSampledFutureYearUpperBound);
                    double range = aaeqDamageUpperBound - aaeqDamageLowerBound;
                    double binQuantity = 1 + 3.322 * Math.Log(iterations);
                    double binWidth = Math.Ceiling(range / binQuantity);
                    Histogram histogram = new Histogram(aaeqDamageLowerBound, binWidth);

                    for (int i = 0; i < iterations; i++)
                    {
                        double eadSampledBaseYear = baseYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].InverseCDF(randomProvider.NextRandom());
                        double eadSampledFutureYear = mlfYearResults[result].DamageResults.HistogramsOfEADs[damageCategory].InverseCDF(randomProvider.NextRandom());
                        double aaeqDamage = ComputeEEAD(eadSampledBaseYear, eadSampledFutureYear);
                        histogram.AddObservationToHistogram(aaeqDamage);
                    }
                    damageByDamageCategories.Add(damageCategory, histogram);
                }
                damageByImpactAreas.Add(result, damageByDamageCategories);
            }
            return damageByImpactAreas;
        }
        //TODO: these functions should be private, but currently have unit tests 
        //so these will remain public until the unit tests are re-written on the above public method
        public double ComputeEEAD(double baseYearEAD, double mostLikelyFutureEAD){

            //probably instantiate a rng to seed each impact area differently

            double[] interpolatedEADs = Interpolate(baseYearEAD, mostLikelyFutureEAD, _currentYear.Year, _futureYear.Year, _periodOfAnalysis);
            double sumPresentValueEAD = PresentValueCompute(interpolatedEADs, _discountRate);
            double averageAnnualEquivalentDamage = IntoAverageAnnualEquivalentTerms(sumPresentValueEAD, _periodOfAnalysis, _discountRate);
            return averageAnnualEquivalentDamage;
        }
        private double IntoAverageAnnualEquivalentTerms(double sumPresentValueEAD, Int64 periodOfAnalysis, double discountRate)
        {
            double presentValueInterestFactorOfAnnuity = (1 - (1 / Math.Pow(1 + discountRate, periodOfAnalysis))) / discountRate;
            double averageAnnualEquivalentDamage = sumPresentValueEAD / presentValueInterestFactorOfAnnuity;
            return averageAnnualEquivalentDamage;
        }
        private double PresentValueCompute(double[] interpolatedEADs, double discountRate)
        {
            Int64 periodOfAnalysis = interpolatedEADs.Length;
            double[] presentValueInterestFactor = new double[periodOfAnalysis];
            double sumPresentValueEAD = 0;
            for (int i=0; i<periodOfAnalysis; i++)
            {
                presentValueInterestFactor[i] = 1 / Math.Pow(1 + discountRate, i+1);
                sumPresentValueEAD += interpolatedEADs[i] * presentValueInterestFactor[i];
            }
            return sumPresentValueEAD;
        }
        private double[] Interpolate(double baseYearEAD, double mostLikelyFutureEAD, Int64 baseYear, Int64 mostLikelyFutureYear, Int64 periodOfAnalysis)
        {
            double yearsBetweenBaseAndMLFInclusive = Convert.ToDouble(mostLikelyFutureYear - baseYear);
            //Int64 yearsAfterMLF = periodOfAnalysis - yearsBetweenBaseAndMLFInclusive;
            double[] interpolatedEADs = new double[periodOfAnalysis];
            for (Int64 i =0; i<yearsBetweenBaseAndMLFInclusive; i++)
            {
                interpolatedEADs[i] = baseYearEAD + i*(1 / yearsBetweenBaseAndMLFInclusive) * (mostLikelyFutureEAD - baseYearEAD);
            }
            for (Int64 i = Convert.ToInt64(yearsBetweenBaseAndMLFInclusive); i<periodOfAnalysis; i++)
            {
                interpolatedEADs[i] = mostLikelyFutureEAD;
            }
            return interpolatedEADs;
        }



    }
}