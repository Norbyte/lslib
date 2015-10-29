using Collada141;
using LSLib.Granny.GR2;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static TransformMatrix TransformFromNode(node node)
        {
            var transform = new Transform();
            var matrix = Matrix4.Identity;
            string transformSID = null;

            for (int i = 0; i < node.ItemsElementName.Length; i++)
            {
                var name = node.ItemsElementName[i];
                var item = node.Items[i];

                switch (name)
                {
                    case ItemsChoiceType2.translate:
                        {
                            var trans = item as TargetableFloat3;
                            transform.Flags |= (int)Transform.TransformFlags.HasTranslation;
                            transform.Translation.X += (float)trans.Values[0];
                            transform.Translation.Y += (float)trans.Values[1];
                            transform.Translation.Z += (float)trans.Values[2];

                            matrix *= Matrix4.CreateTranslation(
                                (float)trans.Values[0],
                                (float)trans.Values[1],
                                (float)trans.Values[2]
                            );
                            break;
                        }

                    case ItemsChoiceType2.rotate:
                        {
                            var rot = item as rotate;
                            transform.Flags |= (int)Transform.TransformFlags.HasRotation;
                            var axis = new Vector3((float)rot.Values[0], (float)rot.Values[1], (float)rot.Values[2]);
                            // TODO: rad -> deg?
                            var rotation = Quaternion.FromAxisAngle(axis, (float)rot.Values[3]);
                            transform.Rotation *= rotation;

                            matrix *= Matrix4.CreateFromAxisAngle(axis, (float)rot.Values[3]);
                            break;
                        }

                    case ItemsChoiceType2.scale:
                        {
                            var trans = item as TargetableFloat3;
                            transform.Flags |= (int)Transform.TransformFlags.HasScaleShear;
                            transform.ScaleShear[0, 0] *= (float)trans.Values[0];
                            transform.ScaleShear[1, 1] *= (float)trans.Values[1];
                            transform.ScaleShear[2, 2] *= (float)trans.Values[2];

                            matrix *= Matrix4.CreateScale(
                                (float)trans.Values[0],
                                (float)trans.Values[1],
                                (float)trans.Values[2]
                            );
                            break;
                        }

                    case ItemsChoiceType2.matrix:
                        {
                            var trans = item as matrix;
                            transformSID = trans.sid;
                            var values = trans.Values;
                            var mat = new Matrix4(
                                (float)values[0], (float)values[1], (float)values[2], (float)values[3],
                                (float)values[4], (float)values[5], (float)values[6], (float)values[7],
                                (float)values[8], (float)values[9], (float)values[10], (float)values[11],
                                (float)values[12], (float)values[13], (float)values[14], (float)values[15]
                            );
                            mat.Transpose();

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

                            matrix *= mat;
                            break;
                        }
                }
            }

            return new TransformMatrix
            {
                matrix = matrix,
                transform = transform,
                TransformSID = transformSID
            };
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
