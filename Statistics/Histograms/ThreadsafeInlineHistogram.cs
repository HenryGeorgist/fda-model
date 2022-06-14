﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Statistics.Histograms
{
    public class ThreadsafeInlineHistogram: IHistogram
    {
        #region Fields
        private Int32[] _BinCounts = new Int32[] { };
        private double _SampleMean;
        private double _SampleVariance;
        private double _Min;
        private double _Max;
        private double _SampleMin;
        private double _SampleMax;
        private int _SampleSize;
        private double _BinWidth;
        private bool _Converged = false;
        private int _ConvergedIterations = int.MinValue;
        private bool _ConvergedOnMax = false;
        private ConvergenceCriteria _ConvergenceCriteria;
        private int _maxQueueCount = 1000; //TODO: what does this represent?
        private int _postQueueCount = 100; //TODO: what does this represent?
        private object _lock = new object();
        private object _bwListLock = new object(); //TODO: what does this represent?
        private static int _enqueue; //TODO: what does this represent?
        private static int _dequeue; //TODO: what does this represent?
        private System.ComponentModel.BackgroundWorker _backgroundWorker;
        private System.Collections.Concurrent.ConcurrentQueue<double> _observations;
        private const string _type = "ThreadsafeInlineHistogram";
        #endregion
        #region Properties
        public string MyType
        {
            get
            {
                return _type;
            }
        }
        public bool IsConverged
        {
            get
            {   //TODO: why did we want to force dequeue but we don't want to anymore?
                //ForceDeQueue();//would need to test for convergence if anything is dequeued...
                return _Converged;
            }
        }
        public int ConvergedIteration
        {
            get
            {
                //ForceDeQueue();//would need to test for convergence if anything is dequeued...
                return _ConvergedIterations;
            }
        }
        public bool ConvergedOnMax
        {
            get
            {
                //ForceDeQueue();//would need to test for convergence if anything is dequeued...
                return _ConvergedOnMax;
            }
        }
        public ConvergenceCriteria ConvergenceCriteria
        {
            get
            {
                return _ConvergenceCriteria;
            }
        }
        public double BinWidth
        {
            get
            {
                return _BinWidth;
            }
        }
        public Int32[] BinCounts
        {
            get
            {
                ForceDeQueue();
                return _BinCounts;
            }
        }
        public double Min
        {
            get
            {
                ForceDeQueue();
                return _Min;
            }
            private set
            {
                _Min = value;
            }
        }
        public double Max
        {
            get
            {
                ForceDeQueue();
                return _Max;
            }
            set
            {
                _Max = value;
            }
        }
        public double Mean
        {
            get
            {
                ForceDeQueue();
                return _SampleMean;
            }
            private set
            {
                _SampleMean = value;
            }
        }
        public double Variance
        {
            get
            {
                ForceDeQueue();
                return _SampleVariance * (double)((double)(_SampleSize - 1) / (double)_SampleSize);
            }
        }
        public double StandardDeviation
        {
            get
            {
                //force is triggered on variance.
                return Math.Pow(Variance, 0.5);
            }
        }
        public int SampleSize
        {
            get
            {
                ForceDeQueue();
                return _SampleSize;
            }
            private set
            {
                _SampleSize = value;
            }
        }
        #endregion
        #region Constructor
        public ThreadsafeInlineHistogram()
        {
            _observations = new System.Collections.Concurrent.ConcurrentQueue<double>();
            _ConvergenceCriteria = new ConvergenceCriteria();
            _backgroundWorker = new System.ComponentModel.BackgroundWorker();
            _backgroundWorker.DoWork += _bw_DoWork;
        }
        public ThreadsafeInlineHistogram(ConvergenceCriteria c)
        {
            _observations = new System.Collections.Concurrent.ConcurrentQueue<double>();
            _ConvergenceCriteria = c;
            _backgroundWorker = new System.ComponentModel.BackgroundWorker();
            _backgroundWorker.DoWork += _bw_DoWork;
        }
        public ThreadsafeInlineHistogram(double binWidth, ConvergenceCriteria c, int startqueueSize = 1000, int postqueueSize = 100)
        {
            _observations = new System.Collections.Concurrent.ConcurrentQueue<double>();
            _BinWidth = binWidth;
            _ConvergenceCriteria = c;
            _backgroundWorker = new System.ComponentModel.BackgroundWorker();
            _backgroundWorker.DoWork += _bw_DoWork;
            _maxQueueCount = startqueueSize;
            _postQueueCount = postqueueSize;
        }
        private ThreadsafeInlineHistogram(double min, double max, double binWidth, Int32[] binCounts, ConvergenceCriteria convergenceCriteria)
        {
            _observations = new System.Collections.Concurrent.ConcurrentQueue<double>();
            Min = min;
            Max = max;
            _BinWidth = binWidth;
            _BinCounts = binCounts;
            //need to sum up the bincounts to get to _N.
            foreach (int count in _BinCounts)
            {
                _SampleSize += count;
            }
            //sample mean, max, variance, and min dont work in this context...
            _ConvergenceCriteria = convergenceCriteria;
            _backgroundWorker = new System.ComponentModel.BackgroundWorker();
            _backgroundWorker.DoWork += _bw_DoWork;
        }
        #endregion

        #region Methods
        private void _bw_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            DeQueue();
        }
        public double Skewness()
        {
            ForceDeQueue();
            if (_SampleSize == 0)
            {
                return double.NaN;
            }
            if (_SampleSize <= 2)
            {
                return 0;
            }
            if (_Min == (_Max - _BinWidth))
            {
                return 0.0;
            }
            double deviation = 0, deviation2 = 0, deviation3 = 0;

            for (int i = 0; i < _BinCounts.Length; i++)
            {
                double midpoint = Min + (i * _BinWidth) + (0.5 * _BinWidth);

                deviation += midpoint - _SampleMean;
                deviation2 += deviation * deviation;
                deviation3 += deviation2 * deviation;

            }
            double variance = _SampleVariance * (double)((double)(_SampleSize - 1) / (double)_SampleSize);
            return deviation3 / _SampleSize / Math.Pow(variance, 3 / 2);
        }
        public double HistogramMean()
        {
            ForceDeQueue();
            if (_SampleSize == 0)
            {
                return double.NaN;
            }
            if (_Min == (_Max - _BinWidth))
            {
                return _Min + (.5 * _BinWidth);
            }
            double sum = 0;
            for (int i = 0; i < _BinCounts.Length; i++)
            {
                sum += (_Min + (i * _BinWidth) + (0.5 * _BinWidth)) * _BinCounts[i];
            }
            return sum / _SampleSize;
        }
        public double HistogramVariance()
        {
            ForceDeQueue();
            if (_SampleSize == 0)
            {
                return double.NaN;
            }
            if (_SampleSize <= 1)
            {
                return 0;
            }
            if (_Min == (_Max - _BinWidth))
            {
                return 0.0;
            }
            double deviation = 0, deviation2 = 0;

            for (int i = 0; i < _BinCounts.Length; i++)
            {
                double midpoint = _Min + (i * _BinWidth) + (0.5 * _BinWidth);

                deviation = midpoint - _SampleMean;
                deviation2 += deviation * deviation;

            }
            return deviation2 / (_SampleSize - 1);
        }
        public double HistogramStandardDeviation()
        {
            //force dequeue is triggered in histogram variance.
            return Math.Sqrt(HistogramVariance());
        }
        public void SetIterationSize(int iterationSize)
        {
            //_observations = new double[iterationSize];
            //_maxQueueCount = iterationSize;
        }
        public void AddObservationToHistogram(double observation, int index)
        {
            _observations.Enqueue(observation);
            Interlocked.Increment(ref _enqueue);
            SafelyDeQueue();
        }
        private void SafelyDeQueue()
        {
            //if (_enqueue - _dequeue > (_maxQueueCount * 2))
            //{
            //    throw new Exception("what the hey!");
            //}
            if (_observations.Count > _maxQueueCount)
            {
                //while (_bw.IsBusy)
                //{
                //    Thread.Sleep(1);
                //}
                lock (_bwListLock)
                {
                    if (!_backgroundWorker.IsBusy) _backgroundWorker.RunWorkerAsync();
                }
            }
        }
        public void ForceDeQueue()
        {
            if (_observations.Count > 0)
            {
                lock (_bwListLock)
                {
                    if (!_backgroundWorker.IsBusy)
                    {
                        DeQueue();
                    }
                    else
                    {
                        while (_backgroundWorker.IsBusy)
                        {
                            Thread.Sleep(1);
                            if (_observations.Count == 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        //TODO: WHat does this mean??
        private void DeQueue()
        {   
            //do NOT reference any properties of this class in this method!
            //it will trigger unsafe operations across threads.

            //apply sturges rule if _n = 0.
            if (_SampleSize == 0)
            {
                Min = _observations.Min();
                double max = _observations.Max();
                int size = _observations.Count();
                double range = max - _Min;
                if (_BinWidth == 0)
                {
                    if (range == 0)
                    {
                        _BinWidth = .01;
                    }
                    else
                    {
                        _BinWidth = range / (1.0 + 3.322 * Math.Log(size));
                    }
                }

                _BinCounts = new int[] { 0 };
                _Max = _Min + _BinWidth;
                _maxQueueCount = _postQueueCount;

            }
            double observation;
            while (_observations.TryDequeue(out observation))
            {
                if (double.IsNaN(observation)) continue;
                if (double.IsInfinity(observation)) continue;
                if (_SampleSize == 0)
                {
                    _SampleMax = observation;
                    _SampleMin = observation;
                    _SampleMean = observation;
                    _SampleVariance = 0;
                    _SampleSize = 1;
                }
                else
                {
                    if (observation > _SampleMax) _SampleMax = observation;
                    if (observation < _SampleMin) _SampleMin = observation;
                    _SampleSize += 1;
                    double tmpMean = _SampleMean + ((observation - _SampleMean) / (double)_SampleSize);
                    _SampleVariance = ((((double)(_SampleSize - 2) / (double)(_SampleSize - 1)) * _SampleVariance) + (Math.Pow(observation - _SampleMean, 2)) / (double)_SampleSize);
                    _SampleMean = tmpMean;
                }
                int quantityAdditionalBins = 0;
                if (observation < _Min)
                {
                    quantityAdditionalBins = Convert.ToInt32(Math.Ceiling((_Min - observation) / _BinWidth));
                    Int32[] newBinCounts = new Int32[quantityAdditionalBins + _BinCounts.Length];

                    for (int i = _BinCounts.Length + quantityAdditionalBins - 1; i > (quantityAdditionalBins - 1); i--)
                    {
                        newBinCounts[i] = _BinCounts[i - quantityAdditionalBins];
                    }
                    _BinCounts = newBinCounts;
                    _BinCounts[0] += 1;
                    double newMin = _Min - (quantityAdditionalBins * _BinWidth);
                    double max = _Max;
                    Min = newMin;
                }
                else if (observation > _Max)
                {
                    quantityAdditionalBins = Convert.ToInt32(Math.Ceiling((observation - _Max + _BinWidth) / _BinWidth));
                    Int32[] newBinCounts = new Int32[quantityAdditionalBins + _BinCounts.Length];
                    for (int i = 0; i < _BinCounts.Length; i++)
                    {
                        newBinCounts[i] = _BinCounts[i];
                    }
                    newBinCounts[_BinCounts.Length + quantityAdditionalBins - 1] += 1;
                    _BinCounts = newBinCounts;
                    double newMax = _Min + (_BinCounts.Length * _BinWidth); //is this right?
                    Max = newMax;
                }
                else
                {
                    int newObsIndex = 0;
                    if (observation != _Min)
                    {
                        newObsIndex = Convert.ToInt32(Math.Floor((observation - _Min) / _BinWidth));
                    }
                    if (observation == _Max)
                    {
                        quantityAdditionalBins = 1;
                        Int32[] newBinCounts = new Int32[quantityAdditionalBins + _BinCounts.Length];
                        for (int i = 0; i < _BinCounts.Length; i++)
                        {
                            newBinCounts[i] = _BinCounts[i];
                        }
                        _BinCounts = newBinCounts;
                        double newMax = _Min + (_BinCounts.Length * _BinWidth);//double check
                        Max = newMax;
                    }
                    _BinCounts[newObsIndex] += 1;
                }
                Interlocked.Increment(ref _dequeue);
            }

        }
        public void AddObservationsToHistogram(double[] data)
        {
            //
            foreach (double x in data)
            {
                AddObservationToHistogram(x,1);
            }
        }
        private double FindBinCount(double x, bool cumulative = true)
        {
            int obsIndex = Convert.ToInt32(Math.Floor((x - _Min) / _BinWidth));
            if (cumulative)
            {
                double sum = 0;
                for (int i = 0; i < obsIndex + 1; i++)
                {
                    sum += _BinCounts[i];
                }
                return sum;
            }
            else
            {
                if(obsIndex<0||obsIndex>=_BinCounts.Length) return 0;
                return _BinCounts[obsIndex];
            }

        }
        public double PDF(double x)
        {
            //ForceDeQueue();
            if (_SampleSize == 0)
            {
                return double.NaN;
            }
            if (_Min == (_Max - _BinWidth))
            {
                if (x > _Min)
                {
                    if (x <= _Max)
                    {
                        return 1.0;
                    }
                }
                return 0.0;
            }
            double nAtX = Convert.ToDouble(FindBinCount(x, false));
            double n = Convert.ToDouble(_SampleSize);
            n = n * _BinWidth;
            return nAtX / n;
        }
        public double CDF(double x)
        {
            //ForceDeQueue();
            if (_SampleSize == 0)
            {
                return double.NaN;
            }
            if (_Min == (_Max - _BinWidth))
            {
                if (x > _Min)
                {
                    if (x <= _Max)
                    {
                        return (_Max - x) / (_Max - _Min);
                    }
                    else
                    {
                        return 1.0;
                    }
                }
                return 0.0;
            }
            double nAtX = Convert.ToDouble(FindBinCount(x));
            double n = Convert.ToDouble(_SampleSize);
            return nAtX / n;
        }
        public double InverseCDF(double p)
        {
            //ForceDeQueue();
            if (p <= 0) return _Min;
            if (p >= 1) return _Max;
            else
            {
                if (_SampleSize == 0)
                {
                    return double.NaN;
                }
                if (_Min == (_Max - _BinWidth))
                {
                    return _Min + (_BinWidth * p);
                }
                int numobs = Convert.ToInt32(_SampleSize * p);
                if (p <= 0.5)
                {
                    int index = 0;
                    double obs = _BinCounts[index];
                    double cobs = obs;
                    while (cobs < numobs)
                    {
                        index++;
                        obs = _BinCounts[index];
                        cobs += obs;

                    }
                    double fraction = 0.0;
                    if (obs == 0)
                    {
                        fraction = .5;
                    }
                    else
                    {
                        fraction = (cobs - numobs) / obs;
                    }

                    double binOffSet = Convert.ToDouble(index + 1);
                    return _Min + _BinWidth * binOffSet - _BinWidth * fraction;
                }
                else
                {
                    int index = _BinCounts.Length - 1;
                    double obs = _BinCounts[index];
                    double cobs = _SampleSize - obs;
                    while (cobs > numobs)
                    {
                        index--;
                        obs = _BinCounts[index];
                        cobs -= obs;
                    }
                    double fraction = 0.0;
                    if (obs == 0)
                    {
                        fraction = .5;
                    }
                    else
                    {
                        fraction = (numobs - cobs) / obs;
                    }
                    double binOffSet = Convert.ToDouble(_BinCounts.Length - index);
                    return _Max - _BinWidth * binOffSet + _BinWidth * fraction;
                }

            }
        }

        public void AddHistograms(List<IHistogram> histograms)
        {
            //if histograms.count == 0, then nothing happens, as you would expect
            //adding 0 histograms to *this* histogram changes nothing 
            if (histograms.Count > 0)
            {
                ConvergenceCriteria convergenceCriteria = histograms[0].ConvergenceCriteria;
                double min = 0;
                double max = 0;
                int sampleSize = 0;
                foreach (ThreadsafeInlineHistogram histogramToAdd in histograms)
                {
                    double newMin = Math.Min(min, histogramToAdd.Min);
                    min = newMin;
                    double newMax = Math.Max(max, histogramToAdd.Max);
                    max = newMax;
                    int newSampleSize = Math.Max(sampleSize, (int)histogramToAdd.SampleSize);
                    sampleSize = newSampleSize;
                }
                double range = max - min;
                double binQuantity = 1 + 3.322 * Math.Log(sampleSize); //sturges rule 
                double binWidth = range / sampleSize;
                ThreadsafeInlineHistogram histogram = new ThreadsafeInlineHistogram(binWidth, convergenceCriteria);
                int seed = 1234;
                Random random = new Random(seed);
                for (int i = 0; i < sampleSize; i++)
                {
                    double summedValue = 0;
                    foreach (ThreadsafeInlineHistogram histogramToSample in histograms)
                    {
                        double value = histogramToSample.InverseCDF(random.NextDouble());
                        summedValue += value;
                    }
                    double thisValue = this.InverseCDF(random.NextDouble());
                    summedValue += thisValue;
                    histogram.AddObservationToHistogram(summedValue, i);
                }
                _BinCounts = histogram._BinCounts;
                _SampleMean = histogram._SampleMean;
                _SampleVariance = histogram._SampleVariance;
                _Min = histogram._Min;
                _Max = histogram._Max;
                _SampleMin = histogram._SampleMin;
                _SampleMax = histogram._SampleMax;
                _SampleSize = histogram._SampleSize;
                _BinWidth = histogram._BinWidth;
                _Converged = histogram._Converged;
                _ConvergedIterations = histogram._ConvergedIterations;
                _ConvergedOnMax = histogram._ConvergedOnMax;
                _ConvergenceCriteria = histogram._ConvergenceCriteria;
            }
        }

        public XElement WriteToXML()
        {
            ForceDeQueue();
            XElement masterElem = new XElement("Histogram");
            masterElem.SetAttributeValue("Min", Min);
            masterElem.SetAttributeValue("Max", Max);
            masterElem.SetAttributeValue("Bin_Width", _BinWidth);
            masterElem.SetAttributeValue("Ordinate_Count", SampleSize);
            masterElem.SetAttributeValue("Bin_Quantity", _BinCounts.Length);
            for (int i = 0; i < _BinCounts.Length; i++)
            {
                masterElem.SetAttributeValue($"Bin_Counts_{i}", _BinCounts[i]);
            }
            XElement convergenceCriteriaElement = _ConvergenceCriteria.WriteToXML();
            convergenceCriteriaElement.Name = "Convergence_Criteria";
            masterElem.Add(convergenceCriteriaElement);
            return masterElem;
        }
        public static ThreadsafeInlineHistogram ReadFromXML(XElement element)
        {
            string minString = element.Attribute("Min").Value;
            double min = Convert.ToDouble(minString);
            string maxString = element.Attribute("Max").Value;
            double max = Convert.ToDouble(maxString);
            string binWidthString = element.Attribute("Bin_Width").Value;
            double binWidth = Convert.ToDouble(binWidthString);
            string sampleSizeString = element.Attribute("Ordinate_Count").Value;
            int sampleSize = Convert.ToInt32(sampleSizeString);
            string binQuantityString = element.Attribute("Bin_Quantity").Value;
            int binQuantity = Convert.ToInt32(binQuantityString);
            Int32[] binCounts = new Int32[binQuantity];
            for (int i = 0; i < binQuantity; i++)
            {
                binCounts[i] = Convert.ToInt32(element.Attribute($"Bin_Counts_{i}").Value);
            }
            ConvergenceCriteria convergenceCriteria = ConvergenceCriteria.ReadFromXML(element.Element("Convergence_Criteria"));
            return new ThreadsafeInlineHistogram(min, max, binWidth, binCounts, convergenceCriteria);
        }
        public bool TestForConvergence(double upperq, double lowerq)
        {
            ForceDeQueue();
            if (_Converged) { return true; }
            if (_SampleSize < _ConvergenceCriteria.MinIterations) { return false; }
            if (_SampleSize >= _ConvergenceCriteria.MaxIterations)
            {
                _Converged = true;
                _ConvergedIterations = _SampleSize;
                _ConvergedOnMax = true;
                return true;
            }
            double qval = InverseCDF(lowerq);
            double qslope = PDF(qval);
            double variance = (lowerq * (1 - lowerq)) / (((double)_SampleSize) * qslope * qslope);
            bool lower = false;
            double lower_comparison = Math.Abs(_ConvergenceCriteria.ZAlpha * Math.Sqrt(variance) / qval);
            if (lower_comparison <= (_ConvergenceCriteria.Tolerance * .5)) { lower = true; }
            qval = InverseCDF(upperq);
            qslope = PDF(qval);
            variance = (upperq * (1 - upperq)) / (((double)_SampleSize) * qslope * qslope);
            bool upper = false;
            double upper_comparison = Math.Abs(_ConvergenceCriteria.ZAlpha * Math.Sqrt(variance) / qval);
            if (upper_comparison <= (_ConvergenceCriteria.Tolerance * .5)) { upper = true; }
            if (lower)
            {
                _Converged = true;
                _ConvergedIterations = _SampleSize;
            }
            if (upper)
            {
                _Converged = true;
                _ConvergedIterations = _SampleSize;
            }
            return _Converged;
        }
        public int EstimateIterationsRemaining(double upperq, double lowerq)
        {
            //TODO: WHAT DO THE BELOW VARIABLES EVEN MEAN??????????
            //PLEASE PROVIDE VARIABLE NAMES IN ENGLISH thank you so much 
            if (_Converged) return 0;
            double up = upperq;
            double val = up * (1 - up);
            double uz2 = 2 * _ConvergenceCriteria.ZAlpha;
            double uxp = InverseCDF(up);
            double ufxp = PDF(uxp);
            int upperestimate = _ConvergenceCriteria.MaxIterations;
            if (ufxp > 0.0 & uxp !=0 )
            {
                upperestimate = Math.Abs((int)Math.Ceiling(val * (Math.Pow((uz2 / (uxp * _ConvergenceCriteria.Tolerance * ufxp)), 2.0))));
            }
            double lp = lowerq;
            double lval = lp * (1 - lp);
            double lz2 = 2 * _ConvergenceCriteria.ZAlpha;
            double lxp = InverseCDF(lp);
            double lfxp = PDF(lxp);
            int lowerestimate = _ConvergenceCriteria.MaxIterations;
            if (lfxp > 0.0 & uxp != 0)
            {
                lowerestimate = Math.Abs((int)Math.Ceiling(val * (Math.Pow((lz2 / (lxp * _ConvergenceCriteria.Tolerance * lfxp)), 2.0))));
            }
            int biggestGuess = Math.Max(upperestimate, lowerestimate);
            int remainingIters = _ConvergenceCriteria.MaxIterations - _SampleSize;
            return Convert.ToInt32(Math.Min(remainingIters, biggestGuess));
        }
        public bool Equals(IHistogram threadsafeInlineHistogram)
        {
            bool convergenceCriteriaAreEqual = _ConvergenceCriteria.Equals(threadsafeInlineHistogram.ConvergenceCriteria);
            if (!convergenceCriteriaAreEqual)
            { 
                return false; 
            }
            for (int i = 0; i < _BinCounts.Length; i++)
            {
                bool binCountsAreEqual = _BinCounts[i].Equals(threadsafeInlineHistogram.BinCounts[i]);
                if (!binCountsAreEqual)
                {
                    return false;
                }
            }
            bool minAreEqual = _Min.Equals(threadsafeInlineHistogram.Min);
            if (!minAreEqual)
            { 
                return false; 
            }
            return true; 

        }
        #endregion
    }
}