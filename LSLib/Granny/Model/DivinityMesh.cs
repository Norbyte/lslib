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
        Cloth,
        MeshProxy
    };

    [Flags]
    public enum DivinityModelFlag
    {
        MeshProxy = 0x01,
        Cloth = 0x02,
        HasProxyGeometry = 0x04,
        HasColor = 0x08,
        Skinned = 0x10,
        Rigid = 0x20
    };

    public enum DivinityVertexUsage
    {
        Position = 1,
        TexCoord = 2,
        QTangent = 3,
        Normal = 3, // The same value is reused for QTangents
        Tangent = 4,
        Binormal = 5,
        BoneWeights = 6,
        BoneIndices = 7,
        Color = 8
    };

    public enum DivinityVertexAttributeFormat
    {
        Real32 = 0,
        UInt32 = 1,
        Int32 = 2,
        Real16 = 3,
        NormalUInt16 = 4,
        UInt16 = 5,
        BinormalInt16 = 6,
        Int16 = 7,
        NormalUInt8 = 8,
        UInt8 = 9,
        BinormalInt8 = 10,
        Int8 = 11
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

        private static DivinityFormatDesc Make(DivinityVertexUsage usage, DivinityVertexAttributeFormat format, Byte size, Byte usageIndex = 0)
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
                formats.Add(Make(DivinityVertexUsage.Position, DivinityVertexAttributeFormat.Real32, 3));
            }

            if (format.HasBoneWeights)
            {
                formats.Add(Make(DivinityVertexUsage.BoneWeights, DivinityVertexAttributeFormat.NormalUInt8, (byte)format.NumBoneInfluences));
                formats.Add(Make(DivinityVertexUsage.BoneIndices, DivinityVertexAttributeFormat.UInt8, (byte)format.NumBoneInfluences));
            }

            if (format.NormalType != NormalType.None)
            {
                if (format.NormalType == NormalType.QTangent)
                {
                    formats.Add(Make(DivinityVertexUsage.QTangent, DivinityVertexAttributeFormat.BinormalInt16, 4));
                }
                else if (format.NormalType == NormalType.Float3)
                {
                    formats.Add(Make(DivinityVertexUsage.Normal, DivinityVertexAttributeFormat.Real32, 3));
                    if (format.TangentType == NormalType.Float3)
                    {
                        formats.Add(Make(DivinityVertexUsage.Tangent, DivinityVertexAttributeFormat.Real32, 3));
                    }
                    if (format.BinormalType == NormalType.Float3)
                    {
                        formats.Add(Make(DivinityVertexUsage.Binormal, DivinityVertexAttributeFormat.Real32, 3));
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Normal format not supported in LSM: {format.NormalType}");
                }
            }

            if (format.ColorMapType != ColorMapType.None)
            {
                if (format.ColorMapType == ColorMapType.Byte4)
                {
                    for (int i = 0; i < format.ColorMaps; i++)
                    {
                        formats.Add(Make(DivinityVertexUsage.Color, DivinityVertexAttributeFormat.NormalUInt8, 4, (byte)i));
                    }
                }
                else if (format.ColorMapType == ColorMapType.Float4)
                {
                    for (int i = 0; i < format.ColorMaps; i++)
                    {
                        formats.Add(Make(DivinityVertexUsage.Color, DivinityVertexAttributeFormat.Real32, 4, (byte)i));
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Color format not supported in LSM: {format.ColorMapType}");
                }
            }

            if (format.TextureCoordinateType != TextureCoordinateType.None)
            {
                if (format.TextureCoordinateType == TextureCoordinateType.Half2)
                {
                    for (int i = 0; i < format.TextureCoordinates; i++)
                    {
                        formats.Add(Make(DivinityVertexUsage.TexCoord, DivinityVertexAttributeFormat.Real16, 2, (byte)i));
                    }
                }
                else if (format.TextureCoordinateType == TextureCoordinateType.Float2)
                {
                    for (int i = 0; i < format.TextureCoordinates; i++)
                    {
                        formats.Add(Make(DivinityVertexUsage.TexCoord, DivinityVertexAttributeFormat.Real32, 2, (byte)i));
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

        public DivinityModelFlag MeshFlags
        {
            get { return (DivinityModelFlag)Flags[0]; }
            set { Flags[0] = (UInt32)value; }
        }
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
        // The GR2 loader checks for this exact string, including spaces.
        public const string UserDefinedProperties_Rigid = "Rigid = true";
        // The GR2 loader checks for this exact string.
        public const string UserDefinedProperties_Cloth = "Cloth=true";
        public const string UserDefinedProperties_MeshProxy = "MeshProxy=true";

        public static string ModelTypeToUserDefinedProperties(DivinityModelType modelType)
        {
            switch (modelType)
            {
                case DivinityModelType.Normal: return "";
                case DivinityModelType.Rigid: return UserDefinedProperties_Rigid;
                case DivinityModelType.Cloth: return UserDefinedProperties_Cloth;
                case DivinityModelType.MeshProxy: return UserDefinedProperties_MeshProxy;
                default: throw new ArgumentException();
            }
        }

        public static DivinityModelType UserDefinedPropertiesToModelType(string userDefinedProperties)
        {
            // The D:OS 2 editor uses the ExtendedData attribute to determine whether a model can be 
            // bound to a character.
            // The "Rigid = true" user defined property is checked for rigid bodies (e.g. weapons), the "Cloth=true"
            // user defined property is checked for clothes.
            if (userDefinedProperties.Contains("Rigid"))
            {
                return DivinityModelType.Rigid;
            }

            if (userDefinedProperties.Contains("Cloth"))
            {
                return DivinityModelType.Cloth;
            }

            if (userDefinedProperties.Contains("MeshProxy"))
            {
                return DivinityModelType.MeshProxy;
            }

            return DivinityModelType.Normal;
        }

        public static DivinityModelType UserMeshFlagsToModelType(DivinityModelFlag flags)
        {
            if ((flags & DivinityModelFlag.Rigid) == DivinityModelFlag.Rigid)
            {
                return DivinityModelType.Rigid;
            }

            if ((flags & DivinityModelFlag.MeshProxy) == DivinityModelFlag.MeshProxy)
            {
                return DivinityModelType.MeshProxy;
            }

            if ((flags & DivinityModelFlag.Cloth) == DivinityModelFlag.Cloth)
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
                return UserMeshFlagsToModelType(mesh.ExtendedData.UserMeshProperties.MeshFlags);
            }
            else if (mesh.ExtendedData != null
                && mesh.ExtendedData.UserDefinedProperties != null)
            {
                return UserDefinedPropertiesToModelType(mesh.ExtendedData.UserDefinedProperties);
            }
            // Only mark model as cloth if it has colored vertices
            else if (mesh.VertexFormat.ColorMaps > 0)
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

        public static DivinityMeshExtendedData MakeMeshExtendedData(Mesh mesh, DivinityModelInfoFormat format,
            DivinityModelType meshModelType)
        {
            var extendedData = DivinityMeshExtendedData.Make();
            if (meshModelType == DivinityModelType.Undefined)
            {
                meshModelType = DivinityHelpers.DetermineModelType(mesh);
            }

            extendedData.UserDefinedProperties =
               DivinityHelpers.ModelTypeToUserDefinedProperties(meshModelType);

            if (format == DivinityModelInfoFormat.UserDefinedProperties)
            {
                extendedData.LSMVersion = 0;
                extendedData.UserMeshProperties = null;
            }
            else
            {
                DivinityModelFlag flags = 0;

                if (mesh.VertexFormat.HasBoneWeights)
                {
                    flags |= DivinityModelFlag.Skinned;
                }

                if (mesh.VertexFormat.ColorMaps > 0)
                {
                    flags |= DivinityModelFlag.HasColor;
                }
                
                switch (meshModelType)
                {
                    case DivinityModelType.Normal:
                        // No special flag should be set here
                        break;

                    case DivinityModelType.Cloth:
                        flags |= DivinityModelFlag.Cloth;
                        break;

                    case DivinityModelType.Rigid:
                        flags |= DivinityModelFlag.Rigid;
                        break;

                    case DivinityModelType.MeshProxy:
                        flags |= DivinityModelFlag.MeshProxy;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                extendedData.UserMeshProperties.MeshFlags = flags;

                if (format == DivinityModelInfoFormat.LSMv1)
                {
                    extendedData.LSMVersion = 1;
                    extendedData.UserMeshProperties.FormatDescs = DivinityFormatDesc.FromVertexFormat(mesh.VertexFormat);
                }
                else
                {
                    extendedData.LSMVersion = 0;
                    extendedData.UserMeshProperties.FormatDescs = new List<DivinityFormatDesc>();
                }
            }

            return extendedData;
        }
    }
}
