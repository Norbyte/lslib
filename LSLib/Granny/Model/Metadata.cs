using System;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model
{
    public class ArtToolInfo
    {
        public String FromArtToolName;
        public Int32 ArtToolMajorRevision;
        public Int32 ArtToolMinorRevision;
        [Serialization(MinVersion = 0x80000011)]
        public Int32 ArtToolPointerSize;
        public Single UnitsPerMeter;
        [Serialization(ArraySize = 3)]
        public Single[] Origin;
        [Serialization(ArraySize = 3)]
        public Single[] RightVector;
        [Serialization(ArraySize = 3)]
        public Single[] UpVector;
        [Serialization(ArraySize = 3)]
        public Single[] BackVector;
        [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000011)]
        public object ExtendedData;

        public void SetYUp()
        {
            RightVector = new float[] { 1, 0, 0 };
            UpVector = new float[] { 0, 1, 0 };
            BackVector = new float[] { 0, 0, -1 };
        }

        public void SetZUp()
        {
            RightVector = new float[] { 1, 0, 0 };
            UpVector = new float[] { 0, 0, 1 };
            BackVector = new float[] { 0, 1, 0 };
        }
    }

    public class ExporterInfo
    {
        public String ExporterName;
        public Int32 ExporterMajorRevision;
        public Int32 ExporterMinorRevision;
        public Int32 ExporterCustomization;
        public Int32 ExporterBuildNumber;
        [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000011)]
        public object ExtendedData;
    }
}
