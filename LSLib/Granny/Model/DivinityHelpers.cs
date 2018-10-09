using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using LSLib.LS;

namespace LSLib.Granny.Model
{
    public enum DivinityModelType
    {
        Undefined,
        Normal,
        Rigid,
        Cloth
    };

    public class DivinityHelpers
    {
        public const string UserDefinedProperties_Rigid = "Rigid = true";
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
    }
}
