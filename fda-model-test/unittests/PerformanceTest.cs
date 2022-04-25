﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;
using metrics;
using compute;
using paireddata;
using Statistics;
using Statistics.Distributions;

namespace fda_model_test.unittests
{
    [Trait("Category", "Unit")]
    public class PerformanceTest
    {
        static double[] Flows = { 0, 100000 };
        static double[] Stages = { 0, 150000 };
        static double[] StageForNonLeveeFailureProbs = { 5000, 8000, 9000, 9600, 9800, 9900, 9960, 9980 };
        static double[] ProbLeveeFailure = { .01, .02, .05, .1, .2, .3, .4, 1 };
        static string xLabel = "x label";
        static string yLabel = "y label";
        static string name = "name";

        /// <summary>
        /// The calculations for the results in the test below can be found at https://docs.google.com/spreadsheets/d/1UUNgHYq1_zV4ifnu0iVmiPOzL2szyBCX/edit?usp=sharing&ouid=105470256128470573157&rtpof=true&sd=true
        /// </summary>
        /// <param name="thresholdValue"></param>
        /// <param name="years"></param>
        /// <param name="iterations"></param>
        /// <param name="expectedAEP"></param>
        /// <param name="expectedLTEP"></param>
        [Theory]
        [InlineData(9200, 80, 1, .08, 0.998732271693343)]
        [InlineData(9400, 60, 1, .06, 0.975584185541488)]
        [InlineData(9600, 40, 1, .04, 0.80463384844468)]
        [InlineData(9800, 20, 1, .02, 0.332392028244906)]
        public void ComputePerformanceWithSimulation_Test(double thresholdValue, int years, int iterations, double expectedAEP, double expectedLTEP)
        {
            
            ContinuousDistribution flow_frequency = new Uniform(0, 100000, 1000);
            //create a stage distribution
            IDistribution[] stages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                stages[i] = IDistributionFactory.FactoryUniform(0, 20000 * i, 10);
            }
            UncertainPairedData flow_stage = new UncertainPairedData(Flows, stages, xLabel, yLabel, name);
            //create a damage distribution
            IDistribution[] damages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                damages[i] = IDistributionFactory.FactoryUniform(0, 600000 * i, 10);
            }

            UncertainPairedData stage_damage = new UncertainPairedData(Stages, damages, xLabel, yLabel, name,  "residential");
            List<UncertainPairedData> uncertainPairedDataList = new List<UncertainPairedData>();
            uncertainPairedDataList.Add(stage_damage);
            int thresholdID = 1;
            ConvergenceCriteria cc = new ConvergenceCriteria(minIterations: 1, maxIterations: iterations);
            Threshold threshold = new Threshold(thresholdID, cc, ThresholdEnum.ExteriorStage, thresholdValue);

            Simulation simulation = Simulation.builder()
                .withFlowFrequency(flow_frequency)
                .withFlowStage(flow_stage)
                .withStageDamages(uncertainPairedDataList)
                .withAdditionalThreshold(threshold)
                .build();
 
            MeanRandomProvider meanRandomProvider = new MeanRandomProvider();
            metrics.Results results = simulation.Compute(meanRandomProvider, cc,false);

            double actualAEP = results.PerformanceByThresholds.ThresholdsDictionary[thresholdID].ProjectPerformanceResults.MeanAEP();
            double actualLTEP = results.PerformanceByThresholds.ThresholdsDictionary[thresholdID].ProjectPerformanceResults.LongTermExceedanceProbability(years);

            double aepDifference = Math.Abs(expectedAEP - actualAEP);
            double aepRelativeDifference = aepDifference / expectedAEP;
            Assert.True(aepRelativeDifference < .025);

            double ltepDifference = Math.Abs(expectedLTEP - actualLTEP);
            double ltepRelativeDifference = ltepDifference / expectedLTEP;
            Assert.True(ltepRelativeDifference < .025);
        }

        /// <summary>
        /// calculations for the below test can be obtained at https://docs.google.com/spreadsheets/d/1iSSQHjxlyKbtqfq1s3-RG_t4W19QZCiW/edit?usp=sharing&ouid=105470256128470573157&rtpof=true&sd=true
        /// </summary>
        /// <param name="thresholdValue"></param>
        /// <param name="iterations"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData(9980, 1, .026)]  
        public void ComputeLeveeAEP_Test(double thresholdValue, int iterations, double expected)
        {
            ContinuousDistribution flow_frequency = new Uniform(0, 100000, 1000);
            //create a stage distribution
            IDistribution[] stages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                stages[i] = IDistributionFactory.FactoryUniform(0, 20000 * i, 10);
            }
            UncertainPairedData flow_stage = new UncertainPairedData(Flows, stages, xLabel, yLabel, name);
            //create a damage distribution
            IDistribution[] damages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                damages[i] = IDistributionFactory.FactoryUniform(0, 600000 * i, 10);
            }

            UncertainPairedData stage_damage = new UncertainPairedData(Stages, damages, xLabel, yLabel, name, "residential");
            List<UncertainPairedData> uncertainPairedDataList = new List<UncertainPairedData>();
            uncertainPairedDataList.Add(stage_damage);
            IDistribution[] failureProbs = new IDistribution[StageForNonLeveeFailureProbs.Length];
            for (int i = 0; i < StageForNonLeveeFailureProbs.Length; i++)
            {
                failureProbs[i] = new Deterministic(ProbLeveeFailure[i]);
            }
            UncertainPairedData leveeCurve = new UncertainPairedData(StageForNonLeveeFailureProbs, failureProbs, xLabel, yLabel, name);

            int thresholdID = 1;
            ConvergenceCriteria cc = new ConvergenceCriteria(minIterations: 1, maxIterations: iterations);
            Threshold threshold = new Threshold(thresholdID, cc, ThresholdEnum.ExteriorStage, thresholdValue);

            Simulation simulation = Simulation.builder()
                .withFlowFrequency(flow_frequency)
                .withFlowStage(flow_stage)
                .withStageDamages(uncertainPairedDataList)
                .withAdditionalThreshold(threshold)
                .withLevee(leveeCurve,thresholdValue)
                .build();

            MeanRandomProvider meanRandomProvider = new MeanRandomProvider();
            metrics.Results results = simulation.Compute(meanRandomProvider, cc, false);
            double actual = results.PerformanceByThresholds.ThresholdsDictionary[thresholdID].ProjectPerformanceResults.MeanAEP();
            Assert.Equal(expected,actual,2);
        }
        /// <summary>
        /// calculations for the below test can be found at https://docs.google.com/spreadsheets/d/1ui_sPDAleoYyu-T3fgraY5ye-WAMVs_j/edit?usp=sharing&ouid=105470256128470573157&rtpof=true&sd=true
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="iterations"></param>
        /// <param name="thresholdValue"></param>
        /// <param name="recurrenceInterval"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData(3456,10001,12000,.9,.666667)]
        [InlineData(5678, 10001, 13000,.98, .663265)]
        [InlineData(6789, 10001, 14000, .99, .707071)]
        [InlineData(8910, 10001, 15000 , .996, .753012)]
        [InlineData(9102, 10001, 16000, .998, .801603)]
        public void ComputeConditionalNonExceedanceProbability_Test(int seed, int iterations, double thresholdValue, double recurrenceInterval, double expected)
        {
            ContinuousDistribution flow_frequency = new Uniform(0, 100000, 1000);
            //create a stage distribution
            IDistribution[] stages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                stages[i] = IDistributionFactory.FactoryUniform(0, 20000 * i, 10);
            }
            UncertainPairedData flow_stage = new UncertainPairedData(Flows, stages, xLabel, yLabel, name);
            //create a damage distribution
            IDistribution[] damages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                damages[i] = IDistributionFactory.FactoryUniform(0, 600000 * i, 10);
            }

            UncertainPairedData stage_damage = new UncertainPairedData(Stages, damages, xLabel, yLabel, name, "residential");
            List<UncertainPairedData> uncertainPairedDataList = new List<UncertainPairedData>();
            uncertainPairedDataList.Add(stage_damage);
            int thresholdID = 1;

            ConvergenceCriteria cc = new ConvergenceCriteria(minIterations: 100, maxIterations: iterations, tolerance: .001);
            Threshold threshold = new Threshold(thresholdID, cc, ThresholdEnum.ExteriorStage, thresholdValue);

            Simulation simulation = Simulation.builder()
                .withFlowFrequency(flow_frequency)
                .withFlowStage(flow_stage)
                .withStageDamages(uncertainPairedDataList)
                .withAdditionalThreshold(threshold)
                .build();

            RandomProvider randomProvider = new RandomProvider(seed);
            metrics.Results results = simulation.Compute(randomProvider, cc, false);
            double actual = results.PerformanceByThresholds.ThresholdsDictionary[thresholdID].ProjectPerformanceResults.ConditionalNonExceedanceProbability(recurrenceInterval);
            double difference = Math.Abs(actual - expected);
            double relativeDifference = difference / expected;
            double tolerance = 0.025;
            Assert.True(relativeDifference < tolerance);
        }

        [Fact]
        public void ConvergenceTest()
        {            
            ConvergenceCriteria convergenceCriteria = new ConvergenceCriteria();
            ThresholdEnum thresholdType = ThresholdEnum.ExteriorStage;
            double thresholdValue = 4.1;
            int thresholdID1 = 1;
            int thresholdID2 = 2;
            Threshold threshold1 = new Threshold(thresholdID1, convergenceCriteria, thresholdType,thresholdValue);
            Threshold threshold2 = new Threshold(thresholdID2, convergenceCriteria, thresholdType, thresholdValue);
            PerformanceByThresholds performanceByThresholds = new PerformanceByThresholds();
            performanceByThresholds.AddThreshold(threshold1);
            performanceByThresholds.AddThreshold(threshold2);

            double keyForCNEP = .98;
            performanceByThresholds.ThresholdsDictionary[thresholdID1].ProjectPerformanceResults.AddConditionalNonExceedenceProbabilityKey(keyForCNEP, convergenceCriteria);
            performanceByThresholds.ThresholdsDictionary[thresholdID2].ProjectPerformanceResults.AddConditionalNonExceedenceProbabilityKey(keyForCNEP, convergenceCriteria);

            int iterations = 2250;
            int seed = 1234;
            Random random = new Random(seed);
            Normal normal = new Normal();

            for (int i = 0; i < iterations; i++)
            {
                double uniformObservation1 = random.NextDouble()+1;
                double uniformObservation2 = random.NextDouble()+2;
                double messyObservation = normal.InverseCDF(random.NextDouble())* random.NextDouble(); //+ random.NextDouble() * random.NextDouble() * random.NextDouble() * 1000;
                double messyObservationLogged = Math.Log(Math.Abs(messyObservation));
                performanceByThresholds.ThresholdsDictionary[thresholdID1].ProjectPerformanceResults.AddStageForCNEP(keyForCNEP, uniformObservation1, i);
                performanceByThresholds.ThresholdsDictionary[thresholdID1].ProjectPerformanceResults.AddStageForCNEP(keyForCNEP, uniformObservation2, i);
                performanceByThresholds.ThresholdsDictionary[thresholdID2].ProjectPerformanceResults.AddStageForCNEP(keyForCNEP, messyObservationLogged, i);
                performanceByThresholds.ThresholdsDictionary[thresholdID2].ProjectPerformanceResults.AddStageForCNEP(keyForCNEP, messyObservation, i);
            }

            double upperConfidenceLimitProbability = 0.975;
            double lowerConfidenceLimitProbability = 0.025;
            Results results = new Results();
            results.PerformanceByThresholds = performanceByThresholds;

            bool isFirstThresholdConverged = performanceByThresholds.ThresholdsDictionary[thresholdID1].ProjectPerformanceResults.CNEPHistogramOfStages[keyForCNEP].TestForConvergence(upperConfidenceLimitProbability, lowerConfidenceLimitProbability);
            bool isSecondThresholdConverged = performanceByThresholds.ThresholdsDictionary[thresholdID2].ProjectPerformanceResults.CNEPHistogramOfStages[keyForCNEP].TestForConvergence(upperConfidenceLimitProbability, lowerConfidenceLimitProbability);
            bool isPerformanceConverged = results.IsPerformanceConverged();

            Assert.True(isFirstThresholdConverged);
            Assert.False(isSecondThresholdConverged);
            Assert.False(isPerformanceConverged);
        }

        [Theory]
        [InlineData(9102, 10001, 16000)]
        public void SerializationShouldReadTheSameObjectItWrites(int seed, int iterations, double thresholdValue)
        {
            ContinuousDistribution flow_frequency = new Uniform(0, 100000, 1000);
            //create a stage distribution
            IDistribution[] stages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                stages[i] = IDistributionFactory.FactoryUniform(0, 20000 * i, 10);
            }
            UncertainPairedData flow_stage = new UncertainPairedData(Flows, stages, xLabel, yLabel, name);
            //create a damage distribution
            IDistribution[] damages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                damages[i] = IDistributionFactory.FactoryUniform(0, 600000 * i, 10);
            }

            UncertainPairedData stage_damage = new UncertainPairedData(Stages, damages, xLabel, yLabel, name, "residential");
            List<UncertainPairedData> uncertainPairedDataList = new List<UncertainPairedData>();
            uncertainPairedDataList.Add(stage_damage);
            int thresholdID = 1;

            ConvergenceCriteria cc = new ConvergenceCriteria(minIterations: 100, maxIterations: iterations, tolerance: .001);
            Threshold threshold = new Threshold(thresholdID, cc, ThresholdEnum.ExteriorStage, thresholdValue);

            Simulation simulation = Simulation.builder()
                .withFlowFrequency(flow_frequency)
                .withFlowStage(flow_stage)
                .withStageDamages(uncertainPairedDataList)
                .withAdditionalThreshold(threshold)
                .build();

            RandomProvider randomProvider = new RandomProvider(seed);
            metrics.Results results = simulation.Compute(randomProvider, cc, false);
            XElement xElement = results.PerformanceByThresholds.ThresholdsDictionary[thresholdID].ProjectPerformanceResults.WriteToXML();
            //TODO: At the next line, convergence criteria is being re-set to 100000
            ProjectPerformanceResults projectPerformanceResults = ProjectPerformanceResults.ReadFromXML(xElement);
            bool success = results.PerformanceByThresholds.ThresholdsDictionary[thresholdID].ProjectPerformanceResults.Equals(projectPerformanceResults);
            Assert.True(success);
        }

    }

}

