using System;
using scenarios;
using metrics;
using Statistics;

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

        [Obsolete("This object is obsolete. There is no need to create an alternative. We only require alternative results.")]
        public Alternative(Scenario currentYear, Scenario futureYear, Int64 periodOfAnalysis, int id){
            _currentYear = currentYear;
            _futureYear = futureYear;
            _periodOfAnalysis = periodOfAnalysis;
            _id = id;
        }
        [Obsolete("This method is obsolete. The intention of the alternative results is to calculate results based on existing scenario results")]
        /// <summary>
        /// Annualization Compute takes the distributions of EAD in each of the Scenarios for a given Alternative and returns a 
        /// ConsequenceResults object with a ConsequenceResult that holds a ThreadsafeInlineHistogram of AAEQ damage for each damage category, asset category, impact area combination. 
        /// </summary>
        /// <param name="randomProvider"></param> random number provider
        /// <param name="iterations"></param> number of iterations to sample distributions
        /// <param name="discountRate"></param> Discount rate should be provided in decimal form.
        /// <param name="computedResultsBaseYear"<>/param> Previously computed Scenario results for the base year. Optionally, leave null and run scenario compute.  
        /// <param name="computedResultsFutureYear"<>/param> Previously computed Scenario results for the future year. Optionally, leave null and run scenario compute. 
        /// <returns></returns>
        public AlternativeResults AnnualizationCompute(interfaces.IProvideRandomNumbers randomProvider, ConvergenceCriteria convergenceCriteria, double discountRate)
        {
            _discountRate = discountRate;
            ScenarioResults baseYearScenarioResults = _currentYear.Compute(randomProvider, convergenceCriteria);//this is a list of impact area-specific ead
            ScenarioResults mlfYearScenarioResults = _futureYear.Compute(randomProvider, convergenceCriteria);
            

            AlternativeResults alternativeResults = new AlternativeResults(_id);
            foreach (ImpactAreaScenarioResults baseYearResults in baseYearScenarioResults.ResultsList)
            {
                ConsequenceResults aaeqResults = new ConsequenceResults(baseYearResults.ImpactAreaID);
                ImpactAreaScenarioResults mlfYearResults = mlfYearScenarioResults.GetResults(baseYearResults.ImpactAreaID);

                foreach (ConsequenceResult baseYearDamageResult in baseYearResults.ConsequenceResults.ConsequenceResultList)
                {
                    ConsequenceResult mlfYearDamageResult = mlfYearResults.ConsequenceResults.GetConsequenceResult(baseYearDamageResult.DamageCategory, baseYearDamageResult.AssetCategory, baseYearDamageResult.RegionID);
                    //Sturges rule 
                    double lowerBoundProbability = 0.0001;
                    double upperBoundProbability = 0.9999;

                    baseYearDamageResult.ConsequenceHistogram.ForceDeQueue();
                    mlfYearDamageResult.ConsequenceHistogram.ForceDeQueue();

                    double eadSampledBaseYearLowerBound = baseYearDamageResult.ConsequenceHistogram.InverseCDF(lowerBoundProbability);
                    double eadSampledFutureYearLowerBound = mlfYearDamageResult.ConsequenceHistogram.InverseCDF(lowerBoundProbability);
                    double eadSampledBaseYearUpperBound = baseYearDamageResult.ConsequenceHistogram.InverseCDF(upperBoundProbability);
                    double eadSampledFutureYearUpperBound = mlfYearDamageResult.ConsequenceHistogram.InverseCDF(upperBoundProbability);

                    double aaeqDamageLowerBound = ComputeEEAD(eadSampledBaseYearLowerBound, (int)_currentYear.Year, eadSampledFutureYearLowerBound, (int)_futureYear.Year, (int)_periodOfAnalysis, _discountRate);
                    double aaeqDamageUpperBound = ComputeEEAD(eadSampledBaseYearUpperBound, (int)_currentYear.Year, eadSampledFutureYearUpperBound, (int)_futureYear.Year, (int)_periodOfAnalysis, _discountRate);
                    double range = aaeqDamageUpperBound - aaeqDamageLowerBound;
                    //TODO: if this depends on convergence criteria, what do we do?
                    double binQuantity = 1 + 3.322 * Math.Log(convergenceCriteria.MaxIterations);
                    double binWidth = Math.Ceiling(range / binQuantity);
                    ConsequenceResult aaeqResult = new ConsequenceResult(baseYearDamageResult.DamageCategory, baseYearDamageResult.AssetCategory, baseYearDamageResult.ConvergenceCriteria, baseYearDamageResult.RegionID, binWidth);
                    //TODO: run this loop until convergence 
                    for (int i = 0; i < convergenceCriteria.MaxIterations; i++)
                    {
                        double eadSampledBaseYear = baseYearDamageResult.ConsequenceHistogram.InverseCDF(randomProvider.NextRandom());
                        double eadSampledFutureYear = mlfYearDamageResult.ConsequenceHistogram.InverseCDF(randomProvider.NextRandom());
                        double aaeqDamage = ComputeEEAD(eadSampledBaseYear, (int)_currentYear.Year, eadSampledFutureYear, (int)_futureYear.Year, (int)_periodOfAnalysis, _discountRate);
                        aaeqResult.AddConsequenceRealization(aaeqDamage,i);
                    }
                    aaeqResult.ConsequenceHistogram.ForceDeQueue();
                    aaeqResults.AddExistingConsequenceResultObject(aaeqResult);
                }
                alternativeResults.AddConsequenceResults(aaeqResults);
            }
            return alternativeResults;
        }
        /// <summary>
        /// Annualization Compute takes the distributions of EAD in each of the Scenarios for a given Alternative and returns a 
        /// ConsequenceResults object with a ConsequenceResult that holds a ThreadsafeInlineHistogram of AAEQ damage for each damage category, asset category, impact area combination. 
        /// </summary>
        /// <param name="randomProvider"></param> random number provider
        /// <param name="iterations"></param> number of iterations to sample distributions
        /// <param name="discountRate"></param> Discount rate should be provided in decimal form.
        /// <param name="computedResultsBaseYear"<>/param> Previously computed Scenario results for the base year. Optionally, leave null and run scenario compute.  
        /// <param name="computedResultsFutureYear"<>/param> Previously computed Scenario results for the future year. Optionally, leave null and run scenario compute. 
        /// <returns></returns>
        public static AlternativeResults AnnualizationCompute(interfaces.IProvideRandomNumbers randomProvider, ConvergenceCriteria convergenceCriteria, double discountRate, int periodOfAnalysis, int alternativeResultsID, int baseYear, ScenarioResults computedResultsBaseYear, int futureYear, ScenarioResults computedResultsFutureYear)
        {
            AlternativeResults alternativeResults = new AlternativeResults(alternativeResultsID);
            alternativeResults.BaseYearScenarioResults = computedResultsBaseYear;
            alternativeResults.FutureYearScenarioResults = computedResultsFutureYear;
            foreach (ImpactAreaScenarioResults baseYearResults in alternativeResults.BaseYearScenarioResults.ResultsList)
            {
                ConsequenceResults aaeqResults = new ConsequenceResults(baseYearResults.ImpactAreaID);
                ImpactAreaScenarioResults mlfYearResults = alternativeResults.FutureYearScenarioResults.GetResults(baseYearResults.ImpactAreaID);

                foreach (ConsequenceResult baseYearDamageResult in baseYearResults.ConsequenceResults.ConsequenceResultList)
                {
                    ConsequenceResult mlfYearDamageResult = mlfYearResults.ConsequenceResults.GetConsequenceResult(baseYearDamageResult.DamageCategory, baseYearDamageResult.AssetCategory, baseYearDamageResult.RegionID);
                    //Sturges rule 
                    double lowerBoundProbability = 0.0001;
                    double upperBoundProbability = 0.9999;

                    baseYearDamageResult.ConsequenceHistogram.ForceDeQueue();
                    mlfYearDamageResult.ConsequenceHistogram.ForceDeQueue();

                    double eadSampledBaseYearLowerBound = baseYearDamageResult.ConsequenceHistogram.InverseCDF(lowerBoundProbability);
                    double eadSampledFutureYearLowerBound = mlfYearDamageResult.ConsequenceHistogram.InverseCDF(lowerBoundProbability);
                    double eadSampledBaseYearUpperBound = baseYearDamageResult.ConsequenceHistogram.InverseCDF(upperBoundProbability);
                    double eadSampledFutureYearUpperBound = mlfYearDamageResult.ConsequenceHistogram.InverseCDF(upperBoundProbability);

                    double aaeqDamageLowerBound = ComputeEEAD(eadSampledBaseYearLowerBound, baseYear, eadSampledFutureYearLowerBound, futureYear, periodOfAnalysis, discountRate);
                    double aaeqDamageUpperBound = ComputeEEAD(eadSampledBaseYearUpperBound, baseYear, eadSampledFutureYearUpperBound, futureYear, periodOfAnalysis, discountRate);
                    double range = aaeqDamageUpperBound - aaeqDamageLowerBound;
                    //TODO: if this depends on convergence criteria, what do we do?
                    double binQuantity = 1 + 3.322 * Math.Log(convergenceCriteria.MaxIterations);
                    double binWidth = Math.Ceiling(range / binQuantity);
                    ConsequenceResult aaeqResult = new ConsequenceResult(baseYearDamageResult.DamageCategory, baseYearDamageResult.AssetCategory, baseYearDamageResult.ConvergenceCriteria, baseYearDamageResult.RegionID, binWidth);
                    //TODO: run this loop until convergence 
                    for (int i = 0; i < convergenceCriteria.MaxIterations; i++)
                    {
                        double eadSampledBaseYear = baseYearDamageResult.ConsequenceHistogram.InverseCDF(randomProvider.NextRandom());
                        double eadSampledFutureYear = mlfYearDamageResult.ConsequenceHistogram.InverseCDF(randomProvider.NextRandom());
                        double aaeqDamage = ComputeEEAD(eadSampledBaseYear, baseYear, eadSampledFutureYear, futureYear, periodOfAnalysis, discountRate);
                        aaeqResult.AddConsequenceRealization(aaeqDamage, i);
                    }
                    aaeqResult.ConsequenceHistogram.ForceDeQueue();
                    aaeqResults.AddExistingConsequenceResultObject(aaeqResult);
                }
                alternativeResults.AddConsequenceResults(aaeqResults);
            }
            return alternativeResults;
        }
        //TODO: these functions should be private, but currently have unit tests 
        //so these will remain public until the unit tests are re-written on the above public method
        public static double ComputeEEAD(double baseYearEAD, int baseYear, double mostLikelyFutureEAD, int mostLikelyFutureYear, int periodOfAnalysis, double discountRate){

            //probably instantiate a rng to seed each impact area differently

            double[] interpolatedEADs = Interpolate(baseYearEAD, mostLikelyFutureEAD, baseYear, mostLikelyFutureYear, periodOfAnalysis);
            double sumPresentValueEAD = PresentValueCompute(interpolatedEADs, discountRate);
            double averageAnnualEquivalentDamage = IntoAverageAnnualEquivalentTerms(sumPresentValueEAD, periodOfAnalysis, discountRate);
            return averageAnnualEquivalentDamage;
        }
        private static double IntoAverageAnnualEquivalentTerms(double sumPresentValueEAD, Int64 periodOfAnalysis, double discountRate)
        {
            double presentValueInterestFactorOfAnnuity = (1 - (1 / Math.Pow(1 + discountRate, periodOfAnalysis))) / discountRate;
            double averageAnnualEquivalentDamage = sumPresentValueEAD / presentValueInterestFactorOfAnnuity;
            return averageAnnualEquivalentDamage;
        }
        private static double PresentValueCompute(double[] interpolatedEADs, double discountRate)
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
        private static double[] Interpolate(double baseYearEAD, double mostLikelyFutureEAD, Int64 baseYear, Int64 mostLikelyFutureYear, Int64 periodOfAnalysis)
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