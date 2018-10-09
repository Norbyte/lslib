using LSLib.Granny.GR2;
using System;
using System.Collections.Generic;

namespace LSLib.Granny.Model
{
    public enum DivinityModelType
    {
        Undefined,
        Normal,
        Rigid,
        Cloth
    };

    public enum DivinityVertexUsage
    {
        Position = 1,
        TexCoord = 2,
        QTangent = 3,
        BoneWeights = 6,
        BoneIndices = 7,
        Color = 8
    };

    public enum DivinityVertexFormat
    {
        Float32 = 0,
        Float16 = 3,
        NormalInt16 = 6,
        NormalUInt8 = 8,
        UInt8 = 9
    };

    public class DivinityFormatDesc
    {
        [Serialization(ArraySize = 1)]
        public SByte[] Stream;
        [Serialization(ArraySize = 1)]
        public Byte[] Usage;
        [Serialization(ArraySize = 1)]
        public Byte[] UsageIndex;
        [Serialization(ArraySize = 1)]
        public Byte[] RefType;
        [Serialization(ArraySize = 1)]
        public Byte[] Format;
        [Serialization(ArraySize = 1)]
        public Byte[] Size;

        private static DivinityFormatDesc Make(DivinityVertexUsage usage, DivinityVertexFormat format, Byte size, Byte usageIndex = 0)
        {
            return new DivinityFormatDesc
            {
                Stream = new SByte[] { 0 },
                Usage = new Byte[] { (byte)usage },
                UsageIndex = new Byte[] { usageIndex },
                RefType = new Byte[] { 0 },
                Format = new Byte[] { (byte)format },
                Size = new Byte[] { size }
            };
        }

        public static List<DivinityFormatDesc> FromVertexFormat(VertexDescriptor format)
        {
            var formats = new List<DivinityFormatDesc>();
            if (format.HasPosition)
            {
                formats.Add(Make(DivinityVertexUsage.Position, DivinityVertexFormat.Float32, 3));
            }

            if (format.HasBoneWeights)
            {
                formats.Add(Make(DivinityVertexUsage.BoneWeights, DivinityVertexFormat.NormalUInt8, (byte)format.NumBoneInfluences));
                formats.Add(Make(DivinityVertexUsage.BoneIndices, DivinityVertexFormat.UInt8, (byte)format.NumBoneInfluences));
            }

            if (format.DiffuseType != DiffuseColorType.None)
            {
                if (format.DiffuseType == DiffuseColorType.Byte4)
                {
                    for (int i = 0; i < format.DiffuseColors; i++)
                    {
                        formats.Add(Make(DivinityVertexUsage.Color, DivinityVertexFormat.UInt8, 4, (byte)i));
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Color format not supported in LSM: {format.DiffuseType}");
                }
            }

            if (format.NormalType != NormalType.None)
            {
                if (format.NormalType == NormalType.QTangent)
                {
                    formats.Add(Make(DivinityVertexUsage.QTangent, DivinityVertexFormat.NormalInt16, 4));
                }
                else
                {
                    throw new InvalidOperationException($"Normal format not supported in LSM: {format.NormalType}");
                }
            }

            if (format.TextureCoordinateType != TextureCoordinateType.None)
            {
                if (format.TextureCoordinateType == TextureCoordinateType.Half2)
                {
                    for (int i = 0; i < format.TextureCoordinates; i++)
                    {
                        formats.Add(Make(DivinityVertexUsage.TexCoord, DivinityVertexFormat.Float16, 2, (byte)i));
                    }
                }
                else
                {
                    throw new InvalidOperationException($"UV format not supported in LSM: {format.TextureCoordinateType}");
                }
            }

            return formats;
        }
    }

    public class DivinityMeshProperties
    {
        [Serialization(ArraySize = 4)]
        public UInt32[] Flags;
        [Serialization(ArraySize = 1)]
        public Int32[] Lod;
        public List<DivinityFormatDesc> FormatDescs;
        [Serialization(Type = MemberType.VariantReference)]
        public object ExtendedData;
    }

    public class DivinityMeshExtendedData
    {
        const Int32 CurrentLSMVersion = 1;

        public string UserDefinedProperties;
        public DivinityMeshProperties UserMeshProperties;
        public Int32 LSMVersion;

        public static DivinityMeshExtendedData Make()
        {
            return new DivinityMeshExtendedData
            {
                UserDefinedProperties = "",
                UserMeshProperties = new DivinityMeshProperties
                {
                    Flags = new UInt32[] { 0, 0, 0, 0 },
                    Lod = new Int32[] { -1 },
                    FormatDescs = null,
                    ExtendedData = null
                },
                LSMVersion = CurrentLSMVersion
            };
        }
    }

    public static class DivinityHelpers
    {
        public const string UserDefinedProperties_Rigid = "Rigid=true";
        public const string UserDefinedProperties_Cloth = "Cloth=true";

        public static string ModelTypeToUserDefinedProperties(DivinityModelType modelType)
        {
            switch (modelType)
            {
                case DivinityModelType.Normal: return "";
                case DivinityModelType.Rigid: return UserDefinedProperties_Rigid;
                case DivinityModelType.Cloth: return UserDefinedProperties_Cloth;
                default: throw new ArgumentException();
            }
        }

        public static DivinityModelType UserDefinedPropertiesToModelType(string userDefinedProperties)
        {
            // The D:OS 2 editor uses the ExtendedData attribute to determine whether a model can be 
            // bound to a character.
            // The "Rigid=true" user defined property is checked for rigid bodies (e.g. weapons), the "Cloth=true"
            // user defined property is checked for clothes.
            if (userDefinedProperties.Contains(UserDefinedProperties_Rigid))
            {
                return DivinityModelType.Rigid;
            }

            if (userDefinedProperties.Contains(UserDefinedProperties_Cloth))
            {
                return DivinityModelType.Cloth;
            }

            return DivinityModelType.Normal;
        }

        public static DivinityModelType UserMeshFlagsToModelType(UInt32 flags)
        {
            if ((flags & 0x20) != 0)
            {
                return DivinityModelType.Rigid;
            }

            if ((flags & 0x02) != 0)
            {
                return DivinityModelType.Cloth;
            }

            return DivinityModelType.Normal;
        }

        public static DivinityModelType DetermineModelType(Mesh mesh)
        {
            if (mesh.ExtendedData != null
                && mesh.ExtendedData.LSMVersion >= 1
                && mesh.ExtendedData.UserMeshProperties != null)
            {
                return UserMeshFlagsToModelType(mesh.ExtendedData.UserMeshProperties.Flags[0]);
            }
            else if (mesh.ExtendedData != null
                && mesh.ExtendedData.UserDefinedProperties != null)
            {
                return UserDefinedPropertiesToModelType(mesh.ExtendedData.UserDefinedProperties);
            }
            // Only mark model as cloth if it has colored vertices
            else if (mesh.VertexFormat.DiffuseColors > 0)
            {
                return DivinityModelType.Cloth;
            }
            else if (!mesh.VertexFormat.HasBoneWeights)
            {
                return DivinityModelType.Rigid;
            }

            return DivinityModelType.Normal;
        }

        public static DivinityModelType DetermineModelType(Root root)
        {
            var modelType = DivinityModelType.Undefined;

            if (root.Meshes != null)
            {
                foreach (var mesh in root.Meshes)
                {
                    var meshType = DetermineModelType(mesh);
                    if (modelType == DivinityModelType.Undefined
                        || (modelType == DivinityModelType.Normal && meshType == DivinityModelType.Cloth))
                    {
                        modelType = meshType;
                    }
                }
            }

            return modelType;
        }

        public static DivinityMeshExtendedData MakeMeshExtendedData(Mesh mesh, DivinityModelInfoFormat format)
        {
            var extendedData = DivinityMeshExtendedData.Make();
            var meshModelType = DivinityHelpers.DetermineModelType(mesh);

            if (format == DivinityModelInfoFormat.UserDefinedProperties)
            {
                extendedData.UserDefinedProperties =
                   DivinityHelpers.ModelTypeToUserDefinedProperties(meshModelType);
            }
            else
            {
                extendedData.LSMVersion = 1;
                switch (meshModelType)
                {
                    case DivinityModelType.Normal:
                        extendedData.UserMeshProperties.Flags[0] |= 0x10;
                        break;

                    case DivinityModelType.Cloth:
                        extendedData.UserMeshProperties.Flags[0] |= 0x02 | 0x10 | 0x08;
                        break;

                    case DivinityModelType.Rigid:
                        extendedData.UserMeshProperties.Flags[0] |= 0x20;
                        break;
                }

                extendedData.UserMeshProperties.FormatDescs = DivinityFormatDesc.FromVertexFormat(mesh.VertexFormat);
            }

            return extendedData;
        }
    }
}
