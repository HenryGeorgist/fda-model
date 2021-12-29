﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using metrics;
using ead;
using paireddata;
using Statistics;

namespace fda_model_test
{
    public class StudyDataResultsTests
    {
        static IDistribution LP3Distribution = IDistributionFactory.FactoryLogPearsonIII(3.537, .438, .075, 125);
        static double[] RatingCurveFlows = { 0, 1500, 2120, 3140, 4210, 5070, 6240, 7050, 9680 };

        static string xLabel = "x label";
        static string yLabel = "y label";
        static string name = "name";
        static string description = "description";
        static int id = 1;


        static IDistribution[] StageDistributions =
        {
            IDistributionFactory.FactoryNormal(458,0),
            IDistributionFactory.FactoryNormal(468.33,.312),
            IDistributionFactory.FactoryNormal(469.97,.362),
            IDistributionFactory.FactoryNormal(471.95,.422),
            IDistributionFactory.FactoryNormal(473.06,.456),
            IDistributionFactory.FactoryNormal(473.66,.474),
            IDistributionFactory.FactoryNormal(474.53,.5),
            IDistributionFactory.FactoryNormal(475.11,.5),
            IDistributionFactory.FactoryNormal(477.4,.5)
                //note that the rating curve domain lies within the stage-damage domain
        };
        static double[] StageDamageStages = { 470, 471, 472, 473, 474, 475, 476, 477, 478, 479 };
        static IDistribution[] DamageDistrbutions =
        {
            IDistributionFactory.FactoryNormal(0,0),
            IDistributionFactory.FactoryNormal(.04,.16),
            IDistributionFactory.FactoryNormal(.66,1.02),
            IDistributionFactory.FactoryNormal(2.83,2.47),
            IDistributionFactory.FactoryNormal(7.48,3.55),
            IDistributionFactory.FactoryNormal(17.82,7.38),
            IDistributionFactory.FactoryNormal(39.87,12.35),
            IDistributionFactory.FactoryNormal(76.91,13.53),
            IDistributionFactory.FactoryNormal(124.82,13.87),
            IDistributionFactory.FactoryNormal(173.73,13.12),
        };

        [Theory]
        [InlineData(20.74)]
        public void ComputeMeanEAD_Test(double expected)
        {
            IDistribution flowFrequency = IDistributionFactory.FactoryLogPearsonIII(3.537, .438, .075, 125);
            UncertainPairedData flowStage = new UncertainPairedData(RatingCurveFlows, StageDistributions, xLabel, yLabel, name, description, id);
            UncertainPairedData stageDamage = new UncertainPairedData(StageDamageStages, DamageDistrbutions, xLabel, yLabel, name, description, id, "residential");
            List<UncertainPairedData> stageDamageList = new List<UncertainPairedData>();
            stageDamageList.Add(stageDamage);
            Simulation simulation = Simulation.builder()
                .withFlowFrequency(flowFrequency)
                .withFlowStage(flowStage)
                .withStageDamages(stageDamageList)
                .build();
            ead.MeanRandomProvider meanRandomProvider = new MeanRandomProvider();
            metrics.Results results = simulation.Compute(meanRandomProvider, 1);
            double difference = expected - results.ExpectedAnnualDamageResults.MeanEAD("residential");
            double relativeDifference = difference / expected;
            Assert.True(relativeDifference < .01);
        }

        [Theory]
        [InlineData(10000,2345,21.09)]
        public void ComputeMeanEADWithIterations_Test(int iterations, int seed, double expected)
        {
            IDistribution flowFrequency = IDistributionFactory.FactoryLogPearsonIII(3.537, .438, .075, 125);
            UncertainPairedData flowStage = new UncertainPairedData(RatingCurveFlows, StageDistributions, xLabel, yLabel, name, description, id);
            UncertainPairedData stageDamage = new UncertainPairedData(StageDamageStages, DamageDistrbutions, xLabel, yLabel, name, description, id, "residential");
            List<UncertainPairedData> stageDamageList = new List<UncertainPairedData>();
            stageDamageList.Add(stageDamage);
            Simulation simulation = Simulation.builder()
                .withFlowFrequency(flowFrequency)
                .withFlowStage(flowStage)
                .withStageDamages(stageDamageList)
                .build();

            ead.RandomProvider randomProvider = new RandomProvider(seed);
            metrics.Results results = simulation.Compute(randomProvider, iterations);

            double difference = expected - results.ExpectedAnnualDamageResults.MeanEAD("residential");
            double relativeDifference = difference / expected;
            Assert.True(relativeDifference < .01);
        }
    }
}
