using System.Collections.Generic;
using Statistics;
using interfaces;
using System.Linq;
using System.Xml.Linq;
using System;

namespace paireddata
{
    public class UncertainPairedData : IPairedDataProducer, ICategory, ICanBeNull
    {
        #region Fields 
        private double[] _xvals;
        private IDistribution[] _yvals;
        private CurveMetaData _metadata;
        #endregion

        #region Properties 
        public string XLabel
        {
            get { return _metadata.XLabel; }
        }
        public string YLabel
        {
            get { return _metadata.YLabel; }
        }
        public string Name
        {
            get { return _metadata.Name; }
        }
        public string Description
        {
            get { return _metadata.Description; }
        }
        public string Category
        {
            get { return _metadata.Category; }
        }
        public bool IsNull
        {
            get { return _metadata.IsNull; }
        }
        public double[] xs()
        {
            return _xvals;
        }
        public IDistribution[] ys()
        {
            return _yvals;
        }
        #endregion

        #region Constructors 
        public UncertainPairedData()
        {
            _metadata = new CurveMetaData();
        }
        //, string xlabel, string ylabel, string name, string description, int ID
        
        public UncertainPairedData(double[] xs, IDistribution[] ys, string xlabel, string ylabel, string name, string description)
        {
            _xvals = xs;
            _yvals = ys;
            _metadata = new CurveMetaData(xlabel,ylabel,name,description);
        }
        public UncertainPairedData(double[] xs, IDistribution[] ys, string xlabel, string ylabel, string name, string description, string category)
        {
            _xvals = xs;
            _yvals = ys;
            _metadata = new CurveMetaData(xlabel, ylabel, name, description, category);
        }
        #endregion

        #region Methods 
        public IPairedData SamplePairedData(double probability)
        {
            double[] y = new double[_yvals.Length];
            for (int i = 0; i < _xvals.Length; i++)
            {
                y[i] = _yvals[i].InverseCDF(probability);
            }
            PairedData pairedData = new PairedData(_xvals, y, _metadata);//mutability leakage on xvals
            pairedData.Validate();
            if (pairedData.HasErrors){
                
                if (pairedData.RuleMap[nameof(pairedData.Yvals)].ErrorLevel > Base.Enumerations.ErrorLevel.Unassigned)
                {
                    Array.Sort(pairedData.Yvals);//sorts but doesnt solve the problem of repeated values.
                }
                if (pairedData.RuleMap[nameof(pairedData.Xvals)].ErrorLevel > Base.Enumerations.ErrorLevel.Unassigned)
                {
                    Array.Sort(pairedData.Xvals);//bad news.
                }
                pairedData.Validate();
                if (pairedData.HasErrors)
                {
                   // throw new Exception("the produced paired data is not monotonically increasing.");
                }

                
            }
            return pairedData;
        }

        public XElement WriteToXML()
        {
            XElement masterElement = new XElement("UncertainPairedData");
            masterElement.SetAttributeValue("Category", Category);
            masterElement.SetAttributeValue("XLabel", XLabel);
            masterElement.SetAttributeValue("YLabel", YLabel);
            masterElement.SetAttributeValue("Name", Name);
            masterElement.SetAttributeValue("Description", Description);
            masterElement.SetAttributeValue("Ordinate_Count", _xvals.Length);
            for (int i = 0; i < _xvals.Length; i++)
            {
                XElement rowElement = new XElement("Coordinate");
                XElement xElement = new XElement("X");
                xElement.SetAttributeValue("Value", _xvals[i]);
                XElement yElement = _yvals[i].ToXML();
                rowElement.Add(xElement);
                rowElement.Add(yElement);
                masterElement.Add(rowElement);
            }
            return masterElement;
        }

        public static UncertainPairedData ReadFromXML(XElement element)
        {
            string category = element.Attribute("Category").Value;
            string xLabel = element.Attribute("XLabel").Value;
            string yLabel = element.Attribute("YLabel").Value;
            string name = element.Attribute("Name").Value;
            string description = element.Attribute("Description").Value;
            int size = Convert.ToInt32(element.Attribute("Ordinate_Count").Value);
            double[] xValues = new double[size];
            IDistribution[] yValues = new IDistribution[size];
            int i = 0;
            foreach (XElement coordinateElement in element.Elements())
            {
                foreach (XElement ordinateElements in coordinateElement.Elements())
                {
                    if (ordinateElements.Name.ToString().Equals("X"))
                    {
                        xValues[i] = Convert.ToDouble(ordinateElements.Attribute("Value").Value);
                    }
                    else
                    {
                        yValues[i] = Statistics.ContinuousDistribution.FromXML(ordinateElements);
                    }
                }
                i++;
            }
            return new UncertainPairedData(xValues, yValues, xLabel, yLabel, name, description, category);
        }
        #endregion
    }
}