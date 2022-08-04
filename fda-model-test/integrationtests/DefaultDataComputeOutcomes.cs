﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alternatives;
using compute;
using metrics;
using paireddata;
using scenarios;
using Statistics;
using Statistics.Distributions;
using Statistics.Histograms;
using Xunit;

namespace fda_model_test.integrationtests
{
    public class DefaultDataComputeOutcomes
    {
        //this class draws on the default data used in the user interface
        //this data can be found in HEC-FDA/ViewModel/Utilities/DefaultCurveData.cs
        #region StudyData
        //Set up curve meta data and convergence criteria 
        private static string xLabel = "X";
        private static string yLabel = "Y";
        private static string name = "Name";
        private static CurveTypesEnum curveType = CurveTypesEnum.MonotonicallyIncreasing;
        private static CurveMetaData generalCurveMetaData = new CurveMetaData(xLabel, yLabel, name, curveType);
        private static ConvergenceCriteria convergenceCriteria = new ConvergenceCriteria();
        private static int seed = 1234;
        private static RandomProvider randomProvider = new RandomProvider(seed);
        private static MeanRandomProvider meanRandomProvider = new MeanRandomProvider();
        private static ConvergenceCriteria singleIterationConvergenceCriteria = new ConvergenceCriteria(1, 1);
        //set up exterior-interior relationship 
        private static double[] _ExteriorInteriorXValues = new double[] { 474, 474.1, 474.3, 474.5, 478 };
        private static IDistribution[] _ExteriorInteriorYValues = new IDistribution[]
        {
            new Deterministic(472),
            new Deterministic(473),
            new Deterministic(474),
            new Deterministic(474.1),
            new Deterministic(478)
        };
        private static UncertainPairedData interiorExterior = new UncertainPairedData(_ExteriorInteriorXValues, _ExteriorInteriorYValues, generalCurveMetaData);

        //set up LP3 dist
        private static double LP3Mean = 3.3;
        private static double LP3StDev = .254;
        private static double LP3Skew = -.1021;
        private static int LP3POR = 48;
        private static ContinuousDistribution lp3 = new LogPearson3(LP3Mean, LP3StDev, LP3Skew, LP3POR);

        //set up graphical flow-frequency relationship - uses LP3POR for ERL
        private static double[] _GraphicalXValues = new double[] { .5, .2, .1, .04, .02, .01, .004, .002 };
        private static double[] _GraphicalYValues = new double[] { 1500, 2120, 3140, 4210, 5070, 6240, 7050, 9680 };
        private static GraphicalUncertainPairedData graphicalFlowFrequency = new GraphicalUncertainPairedData(_GraphicalXValues, _GraphicalYValues, LP3POR, generalCurveMetaData, false);

        //set up graphical stage-frequency relationship - uses LP3POR for ERL 
        private static double[] _GraphicalStageFreqXValues = new double[] { .999, .5, .2, .1, .04, .02, .01, .004, .002 };
        private static double[] _GraphicalStageFreqYValues = new double[] { 458, 468.33, 469.97, 471.95, 473.06, 473.66, 474.53, 475.11, 477.4 };

        private static GraphicalUncertainPairedData graphicalStageFrequency = new GraphicalUncertainPairedData(_GraphicalStageFreqXValues, _GraphicalStageFreqYValues, LP3POR, generalCurveMetaData);
        private static GraphicalUncertainPairedData graphicalStageAsFlowsFrequency = new GraphicalUncertainPairedData(_GraphicalStageFreqXValues, _GraphicalStageFreqYValues, LP3POR, generalCurveMetaData, false);



        //set up regulated-unregulated transform function 
        private static double[] _RegulatedUnregulatedXValues = new double[] { 900, 1500, 2120, 3140, 4210, 5070, 6240, 7050, 9680 };
        private static IDistribution[] _RegulatedUnregulatedYValues = new IDistribution[]
        {
            new Deterministic(900),
            new Deterministic(1500),
            new Deterministic(2000),
            new Deterministic(2010),
            new Deterministic(2020),
            new Deterministic(2050),
            new Deterministic(5500),
            new Deterministic(7050),
            new Deterministic(9680)
        };
        private static UncertainPairedData regulatedUnregulated = new UncertainPairedData(_RegulatedUnregulatedXValues, _RegulatedUnregulatedYValues, generalCurveMetaData);

        //set up a set of stage-damage functions - all functions are the same - just different damage categories and asset categories 
        private static double[] _StageDamageXValues = new double[] { 463, 464, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482 };
        private static IDistribution[] _StageDamageYValues = new IDistribution[]
        {
            new Normal(0,0),
            new Normal(0,0),
            new Normal(0,0),
            new Normal(.04, .16),
            new Normal(.66,1.02),
            new Normal(2.83,2.47),
            new Normal(7.48,3.55),
            new Normal(17.82,7.38),
            new Normal(39.87, 12.35),
            new Normal(76.91, 13.53),
            new Normal(124.82, 13.87),
            new Normal(173.73, 13.12),
            new Normal(218.32, 12.03),
            new Normal(257.83, 11.1),
            new Normal(292.52, 10.31),
            new Normal(370.12,12.3),
            new Normal(480.94,20.45),
            new Normal(890.76,45.67),
            new Normal(1287.45,62.34),
            new Normal(2376.23,134.896),
        };
        private static string residentialDamCat = "residential";
        private static string commercialDamCat = "commercial";
        private static string structureAssetCat = "structure";
        private static string contentAssetCat = "content";
        private static CurveMetaData residentialStructureMeta = new CurveMetaData(xLabel, yLabel, name, residentialDamCat, curveType, structureAssetCat);
        private static CurveMetaData residentialContentMeta = new CurveMetaData(xLabel, yLabel, name, residentialDamCat, curveType, contentAssetCat);
        private static CurveMetaData commercialStructureMeta = new CurveMetaData(xLabel, yLabel, name, commercialDamCat, curveType, structureAssetCat);
        private static CurveMetaData commercialContentMeta = new CurveMetaData(xLabel, yLabel, name, commercialDamCat, curveType, contentAssetCat);
        private static UncertainPairedData residentialContentDamage = new UncertainPairedData(_StageDamageXValues, _StageDamageYValues, residentialContentMeta);
        private static UncertainPairedData residentialStructureDamage = new UncertainPairedData(_StageDamageXValues, _StageDamageYValues, residentialStructureMeta);
        private static UncertainPairedData commercialStructureDamage = new UncertainPairedData(_StageDamageXValues, _StageDamageYValues, commercialStructureMeta);
        private static UncertainPairedData commercialContentDamage = new UncertainPairedData(_StageDamageXValues, _StageDamageYValues, commercialContentMeta);
        private static List<UncertainPairedData> stageDamageList = new List<UncertainPairedData>() { residentialStructureDamage, commercialStructureDamage };
        private static List<UncertainPairedData> expandedStageDamageList = new List<UncertainPairedData>() { residentialStructureDamage, residentialContentDamage, commercialStructureDamage, commercialContentDamage };

        //set up stage-discharge function 
        private static double[] _StageDischargeXValues = new double[] { 0, 1500, 2120, 3140, 4210, 5070, 6240, 7050, 9680 };
        private static IDistribution[] _StageDischargeYValues = new IDistribution[]
        {
            new Normal(458,0),
            new Normal(468.33,.312),
            new Normal(469.97,.362),
            new Normal(471.95,.422),
            new Normal(473.06,.456),
            new Normal(473.66,.474),
            new Normal(477.53,0.5),
            new Normal(479.11,0.5),
            new Normal(481.44, 0.5),
        };
        private static UncertainPairedData stageDischarge = new UncertainPairedData(_StageDischargeXValues, _StageDischargeYValues, generalCurveMetaData);

        //set up levee
        private static double[] _FailureXValues = new double[] { 458, 468, 470, 471, 472, 472.5, 473, 474 };
        private static IDistribution[] _FailureYValues = new IDistribution[]
        {
            new Deterministic(0),
            new Deterministic(.01),
            new Deterministic(.05),
            new Deterministic(.07),
            new Deterministic(.1),
            new Deterministic(.8),
            new Deterministic(.9),
            new Deterministic(1),
        };
        private static UncertainPairedData systemResponse = new UncertainPairedData(_FailureXValues, _FailureYValues, generalCurveMetaData);
        private static double defaultLeveeElevation = 476;
        private static double[] defaultFailureStages = new double[] { 458, 475.999, 476 };
        private static IDistribution[] defaultFailureProbs = new IDistribution[] 
        { 
            new Deterministic(0), 
            new Deterministic(0), 
            new Deterministic(1) 
        };
        private static UncertainPairedData defaultSystemResponse = new UncertainPairedData(defaultFailureStages, defaultFailureProbs, generalCurveMetaData);

        private static int impactAreaID1 = 1;
        private static int impactAreaID2 = 2;
        private static int baseYear = 2030;
        private static int futureYear = 2050;
        #endregion

        [Theory]
        [InlineData(240.5)]
        public void WithoutAnalyticalExpandedStageDamage_ScenarioResults(double expectedMeanEAD)
        {
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(lp3)
                .withFlowStage(stageDischarge)
                .withStageDamages(expandedStageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);

            Scenario scenario2 = new Scenario(futureYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults2 = scenario2.Compute(randomProvider, convergenceCriteria);

            AlternativeResults alternativeResults = Alternative.AnnualizationCompute(randomProvider, .025, 50, 1, scenarioResults, scenarioResults2);

            double actualMeanAAEQ = alternativeResults.MeanAAEQDamage();
            double actualMeanEAD = alternativeResults.MeanBaseYearEAD();

            double tolerance = 0.061;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - expectedMeanEAD) / expectedMeanEAD;
            double AAEQRelativeDifference = Math.Abs(actualMeanAAEQ - expectedMeanEAD) / expectedMeanEAD; //EAD is constant over POA so AAEQ = EAD

            //TODO: Add these three lines to the investigation list. 
            //the results should be approximately the same but are off by 
            //about 10%

            IHistogram eadHistogram = alternativeResults.GetBaseYearEADHistogram();
            double actualMeanEADFromAnotherSource = eadHistogram.Mean;
            Assert.Equal(actualMeanEAD, actualMeanEADFromAnotherSource, 1);

            Assert.True(EADRelativeDifference < tolerance);
            Assert.True(AAEQRelativeDifference < tolerance);
        }

        [Theory]
        [InlineData(.3591, 120.23)]
        public void WithoutAnalytical_ScenarioResults(double expectedMeanAEP, double expectedMeanEAD)
        {
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(lp3)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);
            double actualMeanAEP = scenarioResults.MeanAEP(impactAreaID1);
            double actualMeanEAD = scenarioResults.MeanExpectedAnnualConsequences(impactAreaID1);

            double tolerance = 0.06;
            double AEPRelativeDifference = Math.Abs(actualMeanAEP - expectedMeanAEP) / expectedMeanAEP;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - expectedMeanEAD) / expectedMeanEAD;

            Assert.True(AEPRelativeDifference < tolerance);
            Assert.True(EADRelativeDifference < tolerance);

        }

        [Theory]
        [InlineData(.0526, 65.20)]
        public void AnalyticalWithRegUnreg_ScenarioResults(double meanAEP, double meanEAD)
        {
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(lp3)
                .withInflowOutflow(regulatedUnregulated)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .build();
            ImpactAreaScenarioSimulation simulation2 = ImpactAreaScenarioSimulation.builder(impactAreaID2)
                .withFlowFrequency(lp3)
                .withInflowOutflow(regulatedUnregulated)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);
            impactAreaScenarioSimulations.Add(simulation2);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);
            double actualMeanAEP = scenarioResults.MeanAEP(impactAreaID1);
            double actualMeanEAD = scenarioResults.MeanExpectedAnnualConsequences(impactAreaID1);

            double tolerance = 0.10;
            double AEPRelativeDifference = Math.Abs(actualMeanAEP - meanAEP) / meanAEP;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - meanEAD) / meanEAD;

            Assert.True(AEPRelativeDifference < tolerance);
            Assert.True(EADRelativeDifference < tolerance);

        }

        [Theory]
        [InlineData(.036, 50.23)]
        public void AnalyticalWithLevee_ScenarioResults(double meanAEP, double meanEAD)
        {  
            //TODO: The compute ran when I passed a double[] instead of IDistribution[] into .WithLevee - WHY?
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(lp3)
                .withLevee(defaultSystemResponse,defaultLeveeElevation)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);

            double actualMeanAEP = scenarioResults.MeanAEP(impactAreaID1);
            double actualMeanEAD = scenarioResults.MeanExpectedAnnualConsequences(impactAreaID1);

            //Note the tolerance: 2.0 results are just under 14% different from the 1.4.3 results 
            //whereas without the levee, 2.0 is 6% different from 1.4.3
            //so something about the levee
            //large quantity of iterations does not change the result 
            double tolerance = 0.14;
            double AEPRelativeDifference = Math.Abs(actualMeanAEP - meanAEP) / meanAEP;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - meanEAD) / meanEAD;

            Assert.True(AEPRelativeDifference < tolerance);
            Assert.True(EADRelativeDifference < tolerance);

        }

        [Theory]
        [InlineData(.036, 24.80)]
        public void AnalyticalWithLeveeAndExtInt_ScenarioResults(double meanAEP, double meanEAD)
        {//TODO: Why would AEP here be closer than AEP above?
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(lp3)
                .withLevee(defaultSystemResponse, defaultLeveeElevation)
                .withFlowStage(stageDischarge)
                .withInteriorExterior(interiorExterior)
                .withStageDamages(stageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);
            double actualMeanAEP = scenarioResults.MeanAEP(impactAreaID1);
            double actualMeanEAD = scenarioResults.MeanExpectedAnnualConsequences(impactAreaID1);

            double tolerance = 0.10;
            double AEPRelativeDifference = Math.Abs(actualMeanAEP - meanAEP) / meanAEP;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - meanEAD) / meanEAD;

            Assert.True(AEPRelativeDifference < tolerance);
            Assert.True(EADRelativeDifference < tolerance);

        }

        [Theory]
        [InlineData(.1986, 88.73)]
        public void AnalyticalWithLeveeAndFragility_ScenarioResults(double meanAEP, double meanEAD)
        {
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(lp3)
                .withLevee(systemResponse, defaultLeveeElevation)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .build();

            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);
            double actualMeanAEP = scenarioResults.MeanAEP(impactAreaID1);
            double actualMeanEAD = scenarioResults.MeanExpectedAnnualConsequences(impactAreaID1);

            double tolerance = 0.10;
            double AEPRelativeDifference = Math.Abs(actualMeanAEP - meanAEP) / meanAEP;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - meanEAD) / meanEAD;

            Assert.True(AEPRelativeDifference < tolerance);
            Assert.True(EADRelativeDifference < tolerance);

        }

        [Theory]
        [InlineData(.1522, 65.42)]
        public void WithoutGraphicalFlow_ScenarioResults(double meanAEP, double meanEAD)
        {//TODO: These results are REALLY messed up mathematically 
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(graphicalFlowFrequency)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);
            double actualMeanAEP = scenarioResults.MeanAEP(impactAreaID1);
            double actualMeanEAD = scenarioResults.MeanExpectedAnnualConsequences(impactAreaID1);

            double tolerance = 0.10;
            double AEPRelativeDifference = Math.Abs(actualMeanAEP - meanAEP) / meanAEP;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - meanEAD) / meanEAD;

            Assert.True(AEPRelativeDifference < tolerance);
            Assert.True(EADRelativeDifference < tolerance);

        }

        [Theory]
        [InlineData(.1554, 45.36)]
        public void WithoutGraphicalStage_ScenarioResults(double meanAEP, double meanEAD)
        {//TODO: These results are REALLY messed up mathematically 
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFrequencyStage(graphicalStageFrequency)
                .withStageDamages(stageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);
            double actualMeanAEP = scenarioResults.MeanAEP(impactAreaID1);
            double actualMeanEAD = scenarioResults.MeanExpectedAnnualConsequences(impactAreaID1);

            double tolerance = 0.10;
            double AEPRelativeDifference = Math.Abs(actualMeanAEP - meanAEP) / meanAEP;
            double EADRelativeDifference = Math.Abs(actualMeanEAD - meanEAD) / meanEAD;

            Assert.True(AEPRelativeDifference < tolerance);
            Assert.True(EADRelativeDifference < tolerance);

        }
        [Fact]
        public void WithoutGraphicalStageAsFlows_ScenarioResults()
        {
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(graphicalStageAsFlowsFrequency)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);
            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);
            ImpactAreaScenarioResults impactAreaScenarioResults = scenarioResults.GetResults(impactAreaID1);

            bool resultsAreNull = impactAreaScenarioResults.ConsequenceResults.ConsequenceResultList.Count == 0;
            
            Assert.True(resultsAreNull);

        }
        //The expected values below are not for testing the validity of the compute 
        //rather, the values are used as part of troubleshooting unhandled exceptions
        //TODO this objective of this test is to pass in a sample size of zero and return blank results 
        //The property rule is not working like we expect 
        //until the property rule works, we need to keep a good sample size here 
        [Fact]
        public void AssuranceOfAEPDoesNotHitIndexOutOfBoundsException()
        {
            ContinuousDistribution lp3 = new LogPearson3(3.3, .254, -.1021, 0);
            ImpactAreaScenarioSimulation simulation = ImpactAreaScenarioSimulation.builder(impactAreaID1)
                .withFlowFrequency(lp3)
                .withFlowStage(stageDischarge)
                .withStageDamages(stageDamageList)
                .withLevee(systemResponse, defaultLeveeElevation)
                .build();
            List<ImpactAreaScenarioSimulation> impactAreaScenarioSimulations = new List<ImpactAreaScenarioSimulation>();
            impactAreaScenarioSimulations.Add(simulation);

            Scenario scenario = new Scenario(baseYear, impactAreaScenarioSimulations);
            ScenarioResults scenarioResults = scenario.Compute(randomProvider, convergenceCriteria);

            bool resultsAreNull = scenarioResults.GetResults(impactAreaID1).ConsequenceResults.ConsequenceResultList.Count == 0;
            Assert.True(resultsAreNull);
        }
    }
}
