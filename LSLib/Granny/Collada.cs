using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace LSLib.Granny
{
    class ColladaUtils
    {
        public static sourceTechnique_common MakeAccessor(string type, string[] components, int stride, int elements, string arrayId)
        {
            var sourceTechnique = new sourceTechnique_common();
            var accessor = new accessor();
            var accessorParams = new List<param>();

            foreach (var component in components)
            {
                var param = new param();
                if (component.Length > 0)
                    param.name = component;
                param.type = type;
                accessorParams.Add(param);
            }

            accessor.param = accessorParams.ToArray();
            accessor.source = "#" + arrayId;
            accessor.stride = (ulong)(components.Length * stride);
            accessor.offset = 0;
            accessor.count = (ulong)(elements / stride);
            sourceTechnique.accessor = accessor;
            return sourceTechnique;
        }

        public static source MakeFloatSource(string parentName, string name, string[] components, float[] values, int stride = 1, string type = "float")
        {
            var posName = parentName + "-" + name + "-array";
            // Create a shortened source name if the length exceeds 64 bytes
            if (posName.Length > 64)
            {
                var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(parentName));
                parentName = string.Join("", hash.Select(c => ((int)c).ToString("X2")));
            }

            var positions = new float_array();
            positions.id = parentName + "-" + name + "-array";

            var source = new source();
            source.id = parentName + "-" + name;
            source.name = name;

            positions.count = (ulong)values.Length;
            positions.Values = values.Select(x => (double)x).ToArray();

            var technique = MakeAccessor(type, components, stride, values.Length / components.Length, positions.id);
            source.technique_common = technique;
            source.Item = positions;
            return source;
        }

        public static source MakeNameSource(string parentName, string name, string[] components, string[] values, string type = "name")
        {
            var names = new Name_array();
            names.id = parentName + "-" + name + "-array";

            var source = new source();
            source.id = parentName + "-" + name;
            source.name = name;

            names.count = (ulong)values.Length;
            var varNames = from v in values
                           select v.Replace(' ', '_');

            names.Values = varNames.ToArray();

            var technique = MakeAccessor(type, components, 1, values.Length / components.Length, names.id);
            source.technique_common = technique;
            source.Item = names;
            return source;
        }
    }
}
