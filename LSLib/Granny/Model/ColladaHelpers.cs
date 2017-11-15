using LSLib.Granny.GR2;
using OpenTK;
using System;
using System.Collections.Generic;

namespace LSLib.Granny.Model
{

    class ColladaHelpers
    {
        public struct TransformMatrix
        {
            public Transform transform;
            public Matrix4 matrix;
            public string TransformSID;
        }

        public static void ApplyMatrixTransform(TransformMatrix transformMat, matrix m)
        {
            var transform = transformMat.transform;
            var values = m.Values;
            var mat = new Matrix4(
                (float)values[0], (float)values[1], (float)values[2], (float)values[3],
                (float)values[4], (float)values[5], (float)values[6], (float)values[7],
                (float)values[8], (float)values[9], (float)values[10], (float)values[11],
                (float)values[12], (float)values[13], (float)values[14], (float)values[15]
            );
            mat.Transpose();
            transformMat.matrix *= mat;

            var translation = mat.ExtractTranslation();
            transform.Translation += translation;

            if (translation != Vector3.Zero)
                transform.Flags |= (int)Transform.TransformFlags.HasTranslation;

            var rotation = mat.ExtractRotation();
            transform.Rotation *= rotation;

            if (rotation != Quaternion.Identity)
                transform.Flags |= (int)Transform.TransformFlags.HasRotation;

            var scale = mat.ExtractScale();
            transform.ScaleShear[0, 0] *= scale[0];
            transform.ScaleShear[1, 1] *= scale[1];
            transform.ScaleShear[2, 2] *= scale[2];

            if (transform.ScaleShear != Matrix3.Identity)
                transform.Flags |= (int)Transform.TransformFlags.HasScaleShear;
        }

        public static void ApplyTranslation(TransformMatrix transformMat, TargetableFloat3 translation)
        {
            var transform = transformMat.transform;
            transform.Flags |= (int)Transform.TransformFlags.HasTranslation;
            transform.Translation.X += (float)translation.Values[0];
            transform.Translation.Y += (float)translation.Values[1];
            transform.Translation.Z += (float)translation.Values[2];

            transformMat.matrix *= Matrix4.CreateTranslation(
                (float)translation.Values[0],
                (float)translation.Values[1],
                (float)translation.Values[2]
            );
        }

        public static void ApplyRotation(TransformMatrix transformMat, rotate rotation)
        {
            var transform = transformMat.transform;
            transform.Flags |= (int)Transform.TransformFlags.HasRotation;
            var axis = new Vector3((float)rotation.Values[0], (float)rotation.Values[1], (float)rotation.Values[2]);
            // TODO: rad -> deg?
            var quat = Quaternion.FromAxisAngle(axis, (float)rotation.Values[3]);
            transform.Rotation *= quat;

            transformMat.matrix *= Matrix4.CreateFromAxisAngle(axis, (float)rotation.Values[3]);
        }

        public static void ApplyScale(TransformMatrix transformMat, TargetableFloat3 scale)
        {
            var transform = transformMat.transform;
            transform.Flags |= (int)Transform.TransformFlags.HasScaleShear;
            transform.ScaleShear[0, 0] *= (float)scale.Values[0];
            transform.ScaleShear[1, 1] *= (float)scale.Values[1];
            transform.ScaleShear[2, 2] *= (float)scale.Values[2];

            transformMat.matrix *= Matrix4.CreateScale(
                (float)scale.Values[0],
                (float)scale.Values[1],
                (float)scale.Values[2]
            );
        }

        public static TransformMatrix TransformFromNode(node node)
        {
            var transform = new TransformMatrix
            {
                matrix = Matrix4.Identity,
                transform = new Transform(),
                TransformSID = null
            };

            if (node.ItemsElementName != null)
            {
                for (int i = 0; i < node.ItemsElementName.Length; i++)
                {
                    var name = node.ItemsElementName[i];
                    var item = node.Items[i];

                    switch (name)
                    {
                        case ItemsChoiceType2.translate:
                            {
                                var translation = item as TargetableFloat3;
                                ApplyTranslation(transform, translation);
                                break;
                            }

                        case ItemsChoiceType2.rotate:
                            {
                                var rotation = item as rotate;
                                ApplyRotation(transform, rotation);
                                break;
                            }

                        case ItemsChoiceType2.scale:
                            {
                                var scale = item as TargetableFloat3;
                                ApplyScale(transform, scale);
                                break;
                            }

                        case ItemsChoiceType2.matrix:
                            {
                                var mat = item as matrix;
                                transform.TransformSID = mat.sid;
                                ApplyMatrixTransform(transform, mat);
                                break;
                            }
                    }
                }
            }

            return transform;
        }

        public static List<int> StringsToIntegers(String s)
        {
            var floats = new List<int>(s.Length / 6);
            int startingPos = -1;
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] != ' ')
                {
                    if (startingPos == -1)
                        startingPos = i;
                }
                else
                {
                    if (startingPos != -1)
                    {
                        floats.Add(int.Parse(s.Substring(startingPos, i - startingPos)));
                        startingPos = -1;
                    }
                }
            }

            if (startingPos != -1)
                floats.Add(int.Parse(s.Substring(startingPos, s.Length - startingPos)));

            return floats;
        }

        public static Matrix4 FloatsToMatrix(float[] items)
        {
            return new Matrix4(
                items[0], items[1], items[2], items[3],
                items[4], items[5], items[6], items[7],
                items[8], items[9], items[10], items[11],
                items[12], items[13], items[14], items[15]
            );
        }
    }
}
