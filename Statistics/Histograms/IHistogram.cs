﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Statistics.Histograms
{
    public interface IHistogram
    {
        #region Properties 
        bool IsConverged { get; } 
        int ConvergedIteration { get; }
        double BinWidth { get; }
        Int32[] BinCounts { get; }
        double Min { get;  }
        double Max { get;  }
        double Mean { get; }
        double Variance { get; }
        double StandardDeviation { get; }
        int SampleSize { get; }
        ConvergenceCriteria ConvergenceCriteria { get; }
        string MyType { get; }


        #endregion

        #region Methods
        double PDF(double x);
        double CDF(double x);
        double InverseCDF(double p);
        void AddObservationToHistogram(double observation, int iterationIndex);
        void ForceDeQueue();
        XElement WriteToXML();
        bool TestForConvergence(double upperq, double lowerq);
        int EstimateIterationsRemaining(double upperq, double lowerq);
       void AddHistograms(List<IHistogram> listOfHistogramsToBeAdded);
        bool Equals(IHistogram histogramForComparison);
        #endregion
    }
}
