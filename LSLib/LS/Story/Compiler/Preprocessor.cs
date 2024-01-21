namespace LSLib.LS.Story.Compiler;

public class Preprocessor
{
    public bool Preprocess(String script, ref String preprocessed)
    {
        if (script.IndexOf("/* [OSITOOLS_ONLY]", StringComparison.Ordinal) == -1 
            && script.IndexOf("// [BEGIN_NO_OSITOOLS]", StringComparison.Ordinal) == -1)
        {
            return false;
        }

        var builder = new StringBuilder(script.Length);
        
        int pos = 0;
        while (pos < script.Length)
        {
            var next = script.IndexOf("/* [OSITOOLS_ONLY]", pos, StringComparison.Ordinal);
            if (next == -1)
            {
                builder.Append(script.Substring(pos));
                break;
            }

            var end = script.IndexOf("*/", next, StringComparison.Ordinal);
            if (end == -1)
            {
                builder.Append(script.Substring(pos));
                break;
            }

            builder.Append(script.Substring(pos, next - pos));
            builder.Append(script.Substring(next + 19, end - next - 19));
            pos = end + 2;
        }

        var ph1 = builder.ToString();
        var builderPh2 = new StringBuilder(ph1.Length);

        pos = 0;
        while (pos < ph1.Length)
        {
            int next = ph1.IndexOf("// [BEGIN_NO_OSITOOLS]", pos, StringComparison.Ordinal);
            if (next == -1)
            {
                builderPh2.Append(ph1.Substring(pos));
                break;
            }

            var end = ph1.IndexOf("// [END_NO_OSITOOLS]", next, StringComparison.Ordinal);
            if (end == -1)
            {
                builderPh2.Append(ph1.Substring(pos));
                break;
            }

            builderPh2.Append(ph1.Substring(pos, next - pos));
            pos = end + 21;
        }

        preprocessed = builderPh2.ToString();
        return true;
    }
}
