﻿using paireddata;
using Statistics;
using System;

namespace structures
{ //TODO: add messaging and validation 
    public class OccupancyType
    {
        #region Fields
        //fundamental traits
        private string name;
        private string damcat;

        //configuration flags 
        private bool _computeStructureDamage = false;
        private bool _computeContentDamage = false;
        private bool _computeVehicleDamage = false;
        private bool _computeOtherDamage = false;

        private bool _useContentToStructureValueRatio = false;
        private bool _useOtherToStructureValueRatio = false;

        //damage functions
        private UncertainPairedData _structureDepthPercentDamageFunction;
        private UncertainPairedData _contentDepthPercentDamageFunction;
        private UncertainPairedData _vehicleDepthPercentDamageFunction;
        private UncertainPairedData _OtherDepthPercentDamageFunction;

        //error parameters
        private FirstFloorElevationUncertainty _foundationHeightError;
        private ValueUncertainty _structureValueError;
        private ValueUncertainty _contentValueError;
        private ValueUncertainty _vehicleValueError;
        private ValueUncertainty _otherValueError;

        //value ratios
        private ValueRatioWithUncertainty _contentToStructureValueRatio;
        private ValueRatioWithUncertainty _otherToStructureValueRatio;
        #endregion

        #region Properties 
        internal bool UseContentToStructureValueRatio
        {
            get
            {
                return _useContentToStructureValueRatio;
            }
        }
        internal bool UseOtherToStructureValueRatio
        {
            get
            {
                return _useOtherToStructureValueRatio;
            }
        }
        internal bool ComputeStructureDamage
        {
            get { return _computeStructureDamage; }
        }
        internal bool ComputeContentDamage
        {
            get { return _computeContentDamage; }
        }
        internal bool ComputeOtherDamage
        {
            get { return _computeOtherDamage; }
        }
        internal bool ComputeVehicleDamage
        {
            get { return _computeVehicleDamage; }
        }

        #endregion
        #region Constructor
        internal OccupancyType()
        {
            _structureDepthPercentDamageFunction = new UncertainPairedData();
            _contentDepthPercentDamageFunction = new UncertainPairedData(); 
            _vehicleDepthPercentDamageFunction= new UncertainPairedData();
            _OtherDepthPercentDamageFunction = new UncertainPairedData();
            _foundationHeightError = new FirstFloorElevationUncertainty();
            _structureValueError = new ValueUncertainty();
            _contentValueError = new ValueUncertainty();
            _vehicleValueError = new ValueUncertainty();
            _otherValueError = new ValueUncertainty();
            _contentToStructureValueRatio = new ValueRatioWithUncertainty();
            _otherToStructureValueRatio = new ValueRatioWithUncertainty();
        }
        #endregion
        #region Methods
        public SampledStructureParameters Sample(int seed)
        {
            Random random = new Random(seed);
            //damage functions
            IPairedData structDamagePairedData = _structureDepthPercentDamageFunction.SamplePairedData(random.NextDouble());
            IPairedData contentDamagePairedData = _contentDepthPercentDamageFunction.SamplePairedData(random.NextDouble());
            IPairedData vehicleDamagePairedData = _vehicleDepthPercentDamageFunction.SamplePairedData(random.NextDouble());
            IPairedData otherDamagePairedData = _OtherDepthPercentDamageFunction.SamplePairedData(random.NextDouble());

            //errors
            double foundationHeightError = _foundationHeightError.InverseCDF(random.NextDouble());
            double structureValueError = _structureValueError.InverseCDF(random.NextDouble());
            double contentValueError = _contentValueError.InverseCDF(random.NextDouble());
            double vehicleValueError = _vehicleValueError.InverseCDF(random.NextDouble());
            double otherValueError = _otherValueError.InverseCDF(random.NextDouble());

            //ratios
            double contentToStructureValueRatio = _contentToStructureValueRatio.InverseCDF(random.NextDouble());
            double otherToStructureValueRatio = _otherToStructureValueRatio.InverseCDF(random.NextDouble());
            
            return new SampledStructureParameters(name, damcat, structDamagePairedData, contentDamagePairedData, vehicleDamagePairedData, otherDamagePairedData, foundationHeightError, structureValueError, contentValueError, vehicleValueError, otherValueError, contentToStructureValueRatio, otherToStructureValueRatio);
        }
        #endregion

        public class OccupancyTypeBuilder
        {
            private OccupancyType _occupancyType; 
            internal OccupancyTypeBuilder(OccupancyType occupancyType)
            {
                _occupancyType = occupancyType;
            }
            public OccupancyType build()
            {
                //validate
                return _occupancyType;
            }

            public OccupancyTypeBuilder withStructureDepthPercentDamage(UncertainPairedData structureDepthPercentDamage)
            {
                _occupancyType._structureDepthPercentDamageFunction = structureDepthPercentDamage;
                _occupancyType._computeStructureDamage = true;
                return new OccupancyTypeBuilder(_occupancyType);
            }
            
            public OccupancyTypeBuilder withContentDepthPercentDamage(UncertainPairedData contentDepthPercentDamage)
            {
                _occupancyType._contentDepthPercentDamageFunction = contentDepthPercentDamage;
                _occupancyType._computeContentDamage = true;
                return new OccupancyTypeBuilder(_occupancyType);
            }

            public OccupancyTypeBuilder withVehicleDepthPercentDamage(UncertainPairedData vehicleDepthPercentDamage)
            {
                _occupancyType._vehicleDepthPercentDamageFunction = vehicleDepthPercentDamage;
                _occupancyType._computeVehicleDamage = true;
                return new OccupancyTypeBuilder(_occupancyType);
            }

            public OccupancyTypeBuilder withOtherDepthPercentDamage(UncertainPairedData otherDepthPercentDamage)
            {
                _occupancyType._OtherDepthPercentDamageFunction = otherDepthPercentDamage;
                _occupancyType._computeOtherDamage = true;
                return new OccupancyTypeBuilder(_occupancyType);
            }

            public OccupancyTypeBuilder withStructureValueUncertainty(ValueUncertainty valueUncertainty)
            {
                _occupancyType._structureValueError = valueUncertainty;
                return new OccupancyTypeBuilder(_occupancyType);
            }

            public OccupancyTypeBuilder withContentValueUncertainty(ValueUncertainty valueUncertainty)
            {
                _occupancyType._contentValueError = valueUncertainty;
                return new OccupancyTypeBuilder(_occupancyType);
            }
            public OccupancyTypeBuilder withVehicleValueUncertainty(ValueUncertainty valueUncertainty)
            {
                _occupancyType._vehicleValueError = valueUncertainty;
                return new OccupancyTypeBuilder(_occupancyType);
            }
            public OccupancyTypeBuilder withOtherValueUncertainty(ValueUncertainty valueUncertainty)
            {
                _occupancyType._otherValueError = valueUncertainty;
                return new OccupancyTypeBuilder(_occupancyType);
            }
            public OccupancyTypeBuilder withContentToStructureValueRatio(ValueRatioWithUncertainty valueRatioWithUncertainty)
            {
                _occupancyType._contentToStructureValueRatio = valueRatioWithUncertainty;
                return new OccupancyTypeBuilder(_occupancyType);
            }
            public OccupancyTypeBuilder withOtherToStructureValueRatio(ValueRatioWithUncertainty valueRatioWithUncertainty)
            {
                _occupancyType._otherToStructureValueRatio = valueRatioWithUncertainty;
                return new OccupancyTypeBuilder(_occupancyType);
            }
        }
    }
}