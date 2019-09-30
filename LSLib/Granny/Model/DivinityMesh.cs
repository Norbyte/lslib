using LSLib.Granny.GR2;
using System;
using System.Collections.Generic;

namespace LSLib.Granny.Model
{
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

    public static class DivinityModelFlagMethods
    {
        public static bool IsMeshProxy(this DivinityModelFlag flag)
        {
            return (flag & DivinityModelFlag.MeshProxy) == DivinityModelFlag.MeshProxy;
        }

        public static bool IsCloth(this DivinityModelFlag flag)
        {
            return (flag & DivinityModelFlag.Cloth) == DivinityModelFlag.Cloth;
        }

        public static bool IsRigid(this DivinityModelFlag flag)
        {
            return (flag & DivinityModelFlag.Rigid) == DivinityModelFlag.Rigid;
        }
    }

    public enum DivinityVertexUsage
    {
        None = 0,
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

        public static string ModelFlagsToUserDefinedProperties(DivinityModelFlag modelFlags)
        {
            List<string> properties = new List<string>();
            if (modelFlags.IsRigid())
            {
                properties.Add(UserDefinedProperties_Rigid);
            }

            if (modelFlags.IsCloth())
            {
                properties.Add(UserDefinedProperties_Cloth);
            }

            if (modelFlags.IsMeshProxy())
            {
                properties.Add(UserDefinedProperties_MeshProxy);
            }

            return String.Join("\n", properties);
        }

        public static DivinityModelFlag UserDefinedPropertiesToModelType(string userDefinedProperties)
        {
            // The D:OS 2 editor uses the ExtendedData attribute to determine whether a model can be 
            // bound to a character.
            // The "Rigid = true" user defined property is checked for rigid bodies (e.g. weapons), the "Cloth=true"
            // user defined property is checked for clothes.
            DivinityModelFlag flags = 0;
            if (userDefinedProperties.Contains("Rigid"))
            {
                flags |= DivinityModelFlag.Rigid;
            }

            if (userDefinedProperties.Contains("Cloth"))
            {
                flags |= DivinityModelFlag.Cloth;
            }

            if (userDefinedProperties.Contains("MeshProxy"))
            {
                flags |= DivinityModelFlag.MeshProxy | DivinityModelFlag.HasProxyGeometry;
            }

            return flags;
        }
        
        public static DivinityModelFlag DetermineModelFlags(Mesh mesh, out bool hasDefiniteModelType)
        {
            DivinityModelFlag flags = 0;

            if (mesh.HasDefiniteModelType)
            {
                flags = mesh.ModelType;
                hasDefiniteModelType = true;
            }
            else if (mesh.ExtendedData != null
                && mesh.ExtendedData.LSMVersion >= 1
                && mesh.ExtendedData.UserMeshProperties != null)
            {
                flags = mesh.ExtendedData.UserMeshProperties.MeshFlags;
                hasDefiniteModelType = true;
            }
            else if (mesh.ExtendedData != null
                && mesh.ExtendedData.UserDefinedProperties != null)
            {
                flags = UserDefinedPropertiesToModelType(mesh.ExtendedData.UserDefinedProperties);
                hasDefiniteModelType = true;
            }
            else
            {
                // Only mark model as cloth if it has colored vertices
                if (mesh.VertexFormat.ColorMaps > 0)
                {
                    flags |= DivinityModelFlag.Cloth;
                }

                if (!mesh.VertexFormat.HasBoneWeights)
                {
                    flags |= DivinityModelFlag.Rigid;
                }

                hasDefiniteModelType = false;
            }

            return flags;
        }

        public static DivinityMeshExtendedData MakeMeshExtendedData(Mesh mesh, DivinityModelInfoFormat format,
            DivinityModelFlag modelFlagOverrides)
        {
            var extendedData = DivinityMeshExtendedData.Make();
            DivinityModelFlag modelFlags = modelFlagOverrides;

            if (mesh.HasDefiniteModelType)
            {
                modelFlags = mesh.ModelType;
            }

            if (mesh.VertexFormat.HasBoneWeights)
            {
                modelFlags |= DivinityModelFlag.Skinned;
            }

            if (mesh.VertexFormat.ColorMaps > 0)
            {
                modelFlags |= DivinityModelFlag.HasColor;
            }
            else
            {
                modelFlags &= ~DivinityModelFlag.Cloth;
            }

            extendedData.UserDefinedProperties =
               DivinityHelpers.ModelFlagsToUserDefinedProperties(modelFlags);

            if (format == DivinityModelInfoFormat.UserDefinedProperties)
            {
                extendedData.LSMVersion = 0;
                extendedData.UserMeshProperties = null;
            }
            else
            {
                extendedData.UserMeshProperties.MeshFlags = modelFlags;

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
