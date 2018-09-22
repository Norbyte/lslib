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

        public static DivinityModelType DetermineModelType(Root root)
        {
            // Check if one of the meshes already has a UserDefinedProperties attribute.
            if (root.Meshes != null)
            {
                foreach (var mesh in root.Meshes)
                {
                    if (mesh.ExtendedData != null
                        && mesh.ExtendedData.UserDefinedProperties != null
                        && mesh.ExtendedData.UserDefinedProperties.Length > 0)
                    {
                        return UserDefinedPropertiesToModelType(mesh.ExtendedData.UserDefinedProperties);
                    }
                }
            }

            // Check if one of the bones already has a UserDefinedProperties attribute.
            if (root.Skeletons != null)
            {
                foreach (var skeleton in root.Skeletons)
                {
                    if (skeleton.Bones != null)
                    {
                        foreach (var bone in skeleton.Bones)
                        {
                            if (bone.ExtendedData != null
                                && bone.ExtendedData.UserDefinedProperties != null
                                && bone.ExtendedData.UserDefinedProperties.Length > 0)
                            {
                                return UserDefinedPropertiesToModelType(bone.ExtendedData.UserDefinedProperties);
                            }
                        }
                    }
                }
            }

            // Check if any of the meshes has a rigid vertex format
            if (root.Meshes != null)
            {
                foreach (var mesh in root.Meshes)
                {
                    if (mesh.VertexFormat.DiffuseColors > 0)
                    {
                        return DivinityModelType.Cloth;
                    }

                    var isSkinned = mesh.VertexFormat.HasBoneWeights;
                    if (!isSkinned)
                    {
                        return DivinityModelType.Rigid;
                    }
                }
            }

            return DivinityModelType.Normal;
        }
    }
}
