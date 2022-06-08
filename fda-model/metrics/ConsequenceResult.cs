﻿using System;
using Statistics.Histograms;
using Statistics;
using System.Xml.Linq;
using System.Runtime.Remoting;
using System.Reflection;
using Statistics.Distributions;
namespace metrics
{ //TODO: I THINK SOME OR ALL OF THIS CLASS SHOULD BE INTERNAL 
    public class ConsequenceResult
    {
        #region Fields
        //TODO: hard-wiring the bin width is no good
        private const double HISTOGRAM_BINWIDTH = 10;
        private IHistogram _consequenceHistogram;
        private string _damageCategory;
        private string _assetCategory;
        private int _regionID;
        private ConvergenceCriteria _convergenceCriteria;
        private bool _isNull;
        #endregion

        #region Properties
        public IHistogram ConsequenceHistogram
        {
            get
            {
                return _consequenceHistogram;
            }
        }
        public string DamageCategory
        {
            get
            {
                return _damageCategory;
            }
        }
        public string AssetCategory
        {
            get
            {
                return _assetCategory;
            }
        }
        public int RegionID
        {
            get
            {
                return _regionID;
            }
        }
        public bool IsNull
        {
            get
            {
                return _isNull;
            }
        }
        public ConvergenceCriteria ConvergenceCriteria
        {
            get
            {
                return _convergenceCriteria;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// This constructor builds a ThreadsafeInlineHistogram. Only use for parallel computes. 
        /// </summary>
        public ConsequenceResult()
        {
            _damageCategory = "unassigned";
            _assetCategory = "unassigned";
            _regionID = 0;
            _convergenceCriteria = new ConvergenceCriteria();
            _consequenceHistogram = new ThreadsafeInlineHistogram(HISTOGRAM_BINWIDTH, _convergenceCriteria);
            _isNull = true;
        }
        /// <summary>
        /// This constructor builds a ThreadsafeInlineHistogram. Only use for parallel computes. 
        /// </summary>
        public ConsequenceResult(string damageCategory, string assetCategory, ConvergenceCriteria convergenceCriteria, int impactAreaID)
        {
            _damageCategory = damageCategory;
            _assetCategory = assetCategory;
            _regionID = impactAreaID;
            _convergenceCriteria = convergenceCriteria;
            _consequenceHistogram = new ThreadsafeInlineHistogram(HISTOGRAM_BINWIDTH, _convergenceCriteria);
            _isNull = false;
        }
        /// <summary>
        /// This constructor builds a ThreadsafeInlineHistogram. Only use for parallel computes. 
        /// </summary>
        public ConsequenceResult(string damageCategory, string assetCategory, ConvergenceCriteria convergenceCriteria, int impactAreaID, double binWidth)
        {
            _damageCategory = damageCategory;
            _assetCategory = assetCategory;
            _regionID = impactAreaID;
            _convergenceCriteria = convergenceCriteria;
            _consequenceHistogram = new ThreadsafeInlineHistogram(binWidth, _convergenceCriteria);
            _isNull = false;
        }
        /// <summary>
        /// This constructor can accept wither a Histogram or a ThreadsageInlineHistogram
        /// as such can be used for both compute types
        /// </summary>
        /// <param name="damageCategory"></param>
        /// <param name="assetCategory"></param>
        /// <param name="histogram"></param>
        /// <param name="impactAreaID"></param>
        public ConsequenceResult(string damageCategory, string assetCategory, IHistogram histogram, int impactAreaID)
        {
            _damageCategory = damageCategory;
            _assetCategory = assetCategory;
            _consequenceHistogram = histogram;
            _convergenceCriteria = _consequenceHistogram.ConvergenceCriteria;
            _regionID = impactAreaID;
            _isNull = false;

        }
        #endregion

        #region Methods
        internal void AddConsequenceRealization(double damageRealization, int iteration)
        {
            _consequenceHistogram.AddObservationToHistogram(damageRealization, iteration);
        }

        internal double MeanExpectedAnnualConsequences()
        {
            return _consequenceHistogram.Mean;
        }

        internal double ConsequenceExceededWithProbabilityQ(double exceedanceProbability)
        {
            double nonExceedanceProbability = 1 - exceedanceProbability;
            double quartile = _consequenceHistogram.InverseCDF(nonExceedanceProbability);
            return quartile;
        }

        public bool Equals(ConsequenceResult damageResult)
        {
                bool histogramsMatch = _consequenceHistogram.Equals(damageResult.ConsequenceHistogram);
                if (!histogramsMatch)
                {
                    return false;
                }
            return true;
        }
        //I am leaving this code here for now - when I prove that this other xml stuff works
        //I will delete
        //public XElement WriteToXML()
        //{
        //    XElement masterElement = new XElement("Consequence");// this.GetType().ToString();
        //    XElement histogramElement = _consequenceHistogram.WriteToXML();
        //    histogramElement.Name = "DamageHistogram";
        //    masterElement.Add(histogramElement);
        //    masterElement.SetAttributeValue("DamageCategory", _damageCategory);
        //    masterElement.SetAttributeValue("AssetCategory", _assetCategory);
        //    masterElement.SetAttributeValue("ImpactAreaID", _regionID);
        //    return masterElement;
        //}
        public XElement WriteToXML()
        {
            XElement element = new XElement(this.GetType().Name);
            PropertyInfo[] propertyList = this.GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in propertyList)
            {
                StoredAttribute storedAttribute = (StoredAttribute)propertyInfo.GetCustomAttribute(typeof(StoredAttribute));
                if (storedAttribute != null)
                {
                    element.SetAttributeValue(storedAttribute.Name, propertyInfo.GetValue(this));
                }
            }
            return element;
        }
        public static ConsequenceResult ReadFromXML(XElement xElement)
        {
            string name = xElement.Name.ToString();
            string libraryName = "Statistics";//this libraries name and the appropriate namespace.
            ObjectHandle objectHandle = System.Activator.CreateInstance(libraryName, libraryName + ".Distributions." + name);//requires empty constructor
            IHistogram iDistribution = objectHandle.Unwrap() as IHistogram;








            IHistogram damageHistogram = ThreadsafeInlineHistogram.ReadFromXML(xElement.Element("DamageHistogram"));
            string damageCategory = xElement.Attribute("DamageCategory").Value;
            string assetCategory = xElement.Attribute("AssetCategory").Value;
            int id = Convert.ToInt32(xElement.Attribute("ImpactAreaID").Value);
            return new ConsequenceResult(damageCategory, assetCategory, damageHistogram, id);
        }
        #endregion
    }
}
