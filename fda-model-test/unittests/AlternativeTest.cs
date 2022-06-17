﻿using System;
using System.Collections.Generic;
using Xunit;
using compute;
using paireddata;
using Statistics;
using Statistics.Histograms;
using metrics;
using alternatives;
using Statistics.Distributions;
using scenarios;

namespace fda_model_test.unittests
{
    [Trait("Category", "Unit")]
    public class AlternativeTest
    {
        static double[] FlowXs = { 0, 100000 };
        static double[] StageXs = { 0, 150000 };
        static string xLabel = "x label";
        static string yLabel = "y label";
        static string name = "name";
        static string damCat = "residential";
        static string assetCat = "content";
        CurveMetaData metaData = new CurveMetaData(xLabel, yLabel, name, damCat, assetCat);
        static int impactAreaID = 1;
        static int alternativeID = 1;
        static double exceedanceProbability = 0.5;
        /// <summary>
        /// calculations for the below test can be found at https://docs.google.com/spreadsheets/d/1mPp8O2jm1wnsacQ7ZE3_sU_2xvghWOjC/edit?usp=sharing&ouid=105470256128470573157&rtpof=true&sd=true
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="poa"></param>
        /// <param name="discountRate"></param>
        /// <param name="baseYear"></param>
        /// <param name="futureYear"></param>
        /// <param name="iterations"></param>
        [Theory]
        [InlineData(208213.8061, 50,.0275,2023,2072,1)]
        [InlineData(239260.1814, 50, .0275, 2023, 2050, 1)]
        public void ComputeAAEQDamage(double expected, int poa, double discountRate, int baseYear, int futureYear, int iterations)
        {
            MeanRandomProvider meanRandomProvider = new MeanRandomProvider();
            ConvergenceCriteria convergenceCriteria = new ConvergenceCriteria(maxIterations: iterations);
            ContinuousDistribution flow_frequency = new Uniform(0, 100000, 1000);
            //create a stage distribution
            IDistribution[] stages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                stages[i] = new Uniform(0, 300000 * i, 10);
            }
            UncertainPairedData flow_stage = new UncertainPairedData(FlowXs, stages, metaData);
            //create a damage distribution for base and future year (future year assumption is massive economic development) 
            IDistribution[] baseDamages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                baseDamages[i] = new Uniform(0, 600000 * i, 10);
            }
            IDistribution[] futureDamages = new IDistribution[2];
            for (int i = 0; i < 2; i++)
            {
                futureDamages[i] = new Uniform(0, 1200000 * i, 10);
            }
            UncertainPairedData base_stage_damage = new UncertainPairedData(StageXs, baseDamages, metaData);
            UncertainPairedData future_stage_damage = new UncertainPairedData(StageXs, futureDamages, metaData);
            List<UncertainPairedData> updBase = new List<UncertainPairedData>();
            updBase.Add(base_stage_damage);
            List<UncertainPairedData> updFuture = new List<UncertainPairedData>();
            updFuture.Add(future_stage_damage);

            ImpactAreaScenarioSimulation sBase = ImpactAreaScenarioSimulation.builder(impactAreaID)
                .withFlowFrequency(flow_frequency)
                .withFlowStage(flow_stage)
                .withStageDamages(updBase)
                .build();
 
            ImpactAreaScenarioSimulation sFuture = ImpactAreaScenarioSimulation.builder(impactAreaID)
                .withFlowFrequency(flow_frequency)
                .withFlowStage(flow_stage)
                .withStageDamages(updFuture)
                .build();

            IList<ImpactAreaScenarioSimulation> impactAreaListBaseYear = new List<ImpactAreaScenarioSimulation>();
            impactAreaListBaseYear.Add(sBase);
            IList<ImpactAreaScenarioSimulation> impactAreaListFutureYear = new List<ImpactAreaScenarioSimulation>();
            impactAreaListFutureYear.Add(sFuture);

            Scenario baseScenario = new Scenario(baseYear, impactAreaListBaseYear);
            ScenarioResults baseScenarioResults = baseScenario.Compute(meanRandomProvider, convergenceCriteria);
            Scenario futureScenario = new Scenario(futureYear, impactAreaListFutureYear);
            ScenarioResults futureScenarioResults = futureScenario.Compute(meanRandomProvider, convergenceCriteria);

            AlternativeResults alternativeResults = Alternative.AnnualizationCompute(meanRandomProvider, convergenceCriteria, discountRate, poa, alternativeID, baseScenarioResults, futureScenarioResults);
            double actual = alternativeResults.ConsequencesExceededWithProbabilityQ(exceedanceProbability, impactAreaID, damCat, assetCat);
            double difference = actual - expected;
            double err = Math.Abs(difference / actual);
            double tol = 0.01;
            Assert.True(err<tol);

        }
    }
}
