using System;
using scenarios;
using metrics;
using Statistics;
using Statistics.Histograms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HEC.MVVMFramework.Base.Events;
using HEC.MVVMFramework.Base.Implementations;
using HEC.MVVMFramework.Base.Interfaces;
using HEC.MVVMFramework.Base.Enumerations;

namespace alternatives
{
    public class Alternative: Validation
    {
        private const int _iterations = 50000;
        /// <summary>
        /// Annualization Compute takes the distributions of EAD in each of the Scenarios for a given Alternative and returns a 
        /// ConsequenceResults object with a ConsequenceResult that holds a ThreadsafeInlineHistogram of AAEQ damage for each damage category, asset category, impact area combination. 
        /// </summary>
        /// <param name="randomProvider"></param> random number provider
        /// <param name="discountRate"></param> Discount rate should be provided in decimal form.
        /// <param name="computedResultsBaseYear"<>/param> Previously computed Scenario results for the base year. Optionally, leave null and run scenario compute.  
        /// <param name="computedResultsFutureYear"<>/param> Previously computed Scenario results for the future year. Optionally, leave null and run scenario compute. 
        /// <returns></returns>
        public static AlternativeResults AnnualizationCompute(interfaces.IProvideRandomNumbers randomProvider, double discountRate, int periodOfAnalysis, int alternativeResultsID, ScenarioResults computedResultsBaseYear, ScenarioResults computedResultsFutureYear)
        {


            int baseYear = computedResultsBaseYear.AnalysisYear;
            int futureYear = computedResultsFutureYear.AnalysisYear;
            //validation on future year relative to base year 
            List<int> analysisYears = new List<int>();
            analysisYears.Add(baseYear);
            analysisYears.Add(futureYear);
            if (!CanCompute(baseYear,futureYear, periodOfAnalysis))
            {
                return new AlternativeResults(alternativeResultsID, analysisYears, periodOfAnalysis, false);
            }
            AlternativeResults alternativeResults = new AlternativeResults(alternativeResultsID, analysisYears, periodOfAnalysis);
            alternativeResults.BaseYearScenarioResults = computedResultsBaseYear;
            alternativeResults.FutureYearScenarioResults = computedResultsFutureYear;

            foreach (ImpactAreaScenarioResults baseYearResults in computedResultsBaseYear.ResultsList)
            {
                ImpactAreaScenarioResults mlfYearResults = computedResultsFutureYear.GetResults(baseYearResults.ImpactAreaID);

                foreach (ConsequenceDistributionResult baseYearDamageResult in baseYearResults.ConsequenceResults.ConsequenceResultList)
                {
                    ConsequenceDistributionResult mlfYearDamageResult = mlfYearResults.ConsequenceResults.GetConsequenceResult(baseYearDamageResult.DamageCategory, baseYearDamageResult.AssetCategory, baseYearDamageResult.RegionID);


                    double eadSampledBaseYearLowerBound = baseYearDamageResult.ConsequenceHistogram.Min;
                    double eadSampledFutureYearLowerBound = mlfYearDamageResult.ConsequenceHistogram.Min;
                    double eadSampledBaseYearUpperBound = baseYearDamageResult.ConsequenceHistogram.Max;
                    double eadSampledFutureYearUpperBound = mlfYearDamageResult.ConsequenceHistogram.Max;

                    double aaeqDamageLowerBound = ComputeEEAD(eadSampledBaseYearLowerBound, baseYear, eadSampledFutureYearLowerBound, futureYear, periodOfAnalysis, discountRate);
                    double aaeqDamageUpperBound = ComputeEEAD(eadSampledBaseYearUpperBound, baseYear, eadSampledFutureYearUpperBound, futureYear, periodOfAnalysis, discountRate);
                    double range = aaeqDamageUpperBound - aaeqDamageLowerBound;
                    double binQuantity = 1 + 3.322 * Math.Log(_iterations);
                    double binWidth = Math.Ceiling(range / binQuantity);

        private static bool CanCompute(int baseYear, int futureYear, int periodOfAnalysis)
        {
            bool canCompute = true;
            if (baseYear > futureYear)
            {
                canCompute = false;
            }
            int differenceBetweenBaseAndFutureYearInclusive = futureYear - baseYear + 1;
            if (differenceBetweenBaseAndFutureYearInclusive < 2)
            {
                canCompute = false;
            }
            if (differenceBetweenBaseAndFutureYearInclusive > periodOfAnalysis)
            {
                canCompute = false;
            }
            return canCompute;
        }

        private static ConsequenceDistributionResult IterateOnAAEQ(ConsequenceDistributionResult baseYearDamageResult, ConsequenceDistributionResult mlfYearDamageResult, int baseYear, int futureYear, int periodOfAnalysis, double discountRate, interfaces.IProvideRandomNumbers randomProvider, bool iterateOnFutureYear = true)
        {
            ConsequenceDistributionResult aaeqResult = new ConsequenceDistributionResult();
            ConvergenceCriteria convergenceCriteria;
            if (iterateOnFutureYear)
            {
                convergenceCriteria = mlfYearDamageResult.ConvergenceCriteria;
            }
            else
            {
                convergenceCriteria = baseYearDamageResult.ConvergenceCriteria;
            }
            List<double> resultCollection = new List<double>();
            Int64 iterations = convergenceCriteria.MinIterations;
            bool converged = false;
            while (!converged)
            {
                for (int i = 0; i < iterations; i++)
                {
                    double eadSampledBaseYear = baseYearDamageResult.ConsequenceHistogram.InverseCDF(randomProvider.NextRandom());
                    double eadSampledFutureYear = mlfYearDamageResult.ConsequenceHistogram.InverseCDF(randomProvider.NextRandom());
                    double aaeqDamage = ComputeEEAD(eadSampledBaseYear, baseYear, eadSampledFutureYear, futureYear, periodOfAnalysis, discountRate);
                    resultCollection.Add(aaeqDamage);
                }
                Histogram histogram = new Histogram(resultCollection, convergenceCriteria);
                converged = histogram.IsHistogramConverged(.95, .05);
                if (!converged)
                {
                    iterations = histogram.EstimateIterationsRemaining(.95, .05);
                }
                else
                {
                    iterations = 0;
                    if (iterateOnFutureYear)
                    {
                        aaeqResult = new ConsequenceDistributionResult(mlfYearDamageResult.DamageCategory, mlfYearDamageResult.AssetCategory, histogram, mlfYearDamageResult.RegionID);
                    }
                    else
                    {
                        aaeqResult = new ConsequenceDistributionResult(baseYearDamageResult.DamageCategory, baseYearDamageResult.AssetCategory, histogram, baseYearDamageResult.RegionID);
                    }
                    break;
                }
            }
            return aaeqResult;
            return aaeqResult;
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
        private static double IntoAverageAnnualEquivalentTerms(double sumPresentValueEAD, int periodOfAnalysis, double discountRate)
        {
            double presentValueInterestFactorOfAnnuity = (1 - (1 / Math.Pow(1 + discountRate, periodOfAnalysis))) / discountRate;
            double averageAnnualEquivalentDamage = sumPresentValueEAD / presentValueInterestFactorOfAnnuity;
            return averageAnnualEquivalentDamage;
        }
        private static double PresentValueCompute(double[] interpolatedEADs, double discountRate)
        {
            int periodOfAnalysis = interpolatedEADs.Length;
            double[] presentValueInterestFactor = new double[periodOfAnalysis];
            double sumPresentValueEAD = 0;
            for (int i=0; i<periodOfAnalysis; i++)
            {
                presentValueInterestFactor[i] = 1 / Math.Pow(1 + discountRate, i+1);
                sumPresentValueEAD += interpolatedEADs[i] * presentValueInterestFactor[i];
            }
            return sumPresentValueEAD;
        }
        private static double[] Interpolate(double baseYearEAD, double mostLikelyFutureEAD, int baseYear, int mostLikelyFutureYear, int periodOfAnalysis)
        {
            double yearsBetweenBaseAndMLFInclusive = Convert.ToDouble(mostLikelyFutureYear - baseYear +1);
            double[] interpolatedEADs = new double[periodOfAnalysis];
            for (int i =0; i<yearsBetweenBaseAndMLFInclusive; i++)
            {
                interpolatedEADs[i] = baseYearEAD + i*(1 / yearsBetweenBaseAndMLFInclusive) * (mostLikelyFutureEAD - baseYearEAD);
            }
            for (int i = Convert.ToInt32(yearsBetweenBaseAndMLFInclusive); i<periodOfAnalysis; i++)
            {
                interpolatedEADs[i] = mostLikelyFutureEAD;
            }
            return interpolatedEADs;
        }



    }
}