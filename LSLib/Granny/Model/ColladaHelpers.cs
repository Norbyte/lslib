using LSLib.Granny.GR2;
using OpenTK.Mathematics;

namespace LSLib.Granny.Model;

static class NodeHelpers
{
    public static Matrix4 GetTransformHierarchy(IEnumerable<node> nodes)
    {
        var accum = Matrix4.Identity;
        foreach (var node in nodes)
        {
            accum = node.GetLocalTransform() * accum;
        }

        return accum;
    }

    public static Matrix4 ToMatrix4(this matrix m)
    {
        var v = m.Values;
        return new Matrix4(
            (float)v[0], (float)v[1], (float)v[2], (float)v[3],
            (float)v[4], (float)v[5], (float)v[6], (float)v[7],
            (float)v[8], (float)v[9], (float)v[10], (float)v[11],
            (float)v[12], (float)v[13], (float)v[14], (float)v[15]
        );
    }

    public static Matrix4 ToMatrix4(this rotate r)
    {
        var axis = new Vector3((float)r.Values[0], (float)r.Values[1], (float)r.Values[2]);
        var rot = Quaternion.FromAxisAngle(axis, (float)r.Values[3]);
        return Matrix4.CreateFromQuaternion(rot);
    }

    public static Matrix4 TranslationToMatrix4(this TargetableFloat3 t)
    {
        Matrix4.CreateTranslation((float)t.Values[0], (float)t.Values[1], (float)t.Values[2], out Matrix4 trans);
        return trans;
    }

    public static Matrix4 ScaleToMatrix4(this TargetableFloat3 t)
    {
        Matrix4.CreateScale((float)t.Values[0], (float)t.Values[1], (float)t.Values[2], out Matrix4 scale);
        return scale;
    }

    public static Matrix4 GetLocalTransform(this node n)
    {
        var accum = Matrix4.Identity;

        if (n.ItemsElementName != null)
        {
            for (var i = 0; i < n.ItemsElementName.Length; i++)
            {
                var name = n.ItemsElementName[i];
                accum = name switch
                {
                    ItemsChoiceType2.matrix => (n.Items[i] as matrix).ToMatrix4() * Matrix4.Identity,
                    ItemsChoiceType2.translate => (n.Items[i] as TargetableFloat3).TranslationToMatrix4() * Matrix4.Identity,
                    ItemsChoiceType2.rotate => (n.Items[i] as rotate).ToMatrix4() * Matrix4.Identity,
                    ItemsChoiceType2.scale => (n.Items[i] as TargetableFloat3).ScaleToMatrix4() * Matrix4.Identity,
                    _ => throw new Exception("Unsupported Collada NODE transform: " + name),
                };
            }
        }

        return accum;
    }
}

class ColladaHelpers
{
    public class TransformMatrix
    {
        public Matrix4 transform;
        public string TransformSID;
    }

    public static void ApplyMatrixTransform(TransformMatrix transformMat, matrix m)
    {
        var values = m.Values;
        var mat = new Matrix4(
            (float)values[0], (float)values[1], (float)values[2], (float)values[3],
            (float)values[4], (float)values[5], (float)values[6], (float)values[7],
            (float)values[8], (float)values[9], (float)values[10], (float)values[11],
            (float)values[12], (float)values[13], (float)values[14], (float)values[15]
        );
        mat.Transpose();
        transformMat.transform *= mat;
    }

    public static void ApplyTranslation(TransformMatrix transformMat, TargetableFloat3 translation)
    {
        var translationMat = Matrix4.CreateTranslation(
            (float)translation.Values[0],
            (float)translation.Values[1],
            (float)translation.Values[2]
        );
        transformMat.transform = translationMat * transformMat.transform;

    }

    public static void ApplyRotation(TransformMatrix transformMat, rotate rotation)
    {
        var axis = new Vector3((float)rotation.Values[0], (float)rotation.Values[1], (float)rotation.Values[2]);
        var rotationMat = Matrix4.CreateFromAxisAngle(axis, (float)rotation.Values[3]);
        transformMat.transform = rotationMat * transformMat.transform;
    }

    public static void ApplyScale(TransformMatrix transformMat, TargetableFloat3 scale)
    {
        var scaleMat = Matrix4.CreateScale(
            (float)scale.Values[0],
            (float)scale.Values[1],
            (float)scale.Values[2]
        );
        transformMat.transform = scaleMat * transformMat.transform;
    }

    public static TransformMatrix TransformFromNode(node node)
    {
        var transform = new TransformMatrix
        {
            transform = Matrix4.Identity,
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
                    floats.Add(int.Parse(s[startingPos..i]));
                    startingPos = -1;
                }
            }
        }

        if (startingPos != -1)
            floats.Add(int.Parse(s[startingPos..]));

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

    public static List<Vector3> SourceToPositions(ColladaSource source)
    {
        List<Single> x = null, y = null, z = null;
        if (!source.FloatParams.TryGetValue("X", out x) ||
            !source.FloatParams.TryGetValue("Y", out y) ||
            !source.FloatParams.TryGetValue("Z", out z))
            throw new ParsingException("Position source " + source.id + " must have X, Y, Z float attributes");

        var positions = new List<Vector3>(x.Count);
        for (var i = 0; i < x.Count; i++)
        {
            positions.Add(new Vector3(x[i], y[i], z[i]));
        }

        return positions;
    }
}
