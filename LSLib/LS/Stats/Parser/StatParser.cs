using QUT.Gppg;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LSLib.LS.Stats.StatParser
{
    public partial class StatScanner
    {
        public LexLocation LastLocation()
        {
            return new LexLocation(tokLin, tokCol, tokELin, tokECol);
        }
    }

    public abstract class StatScanBase : AbstractScanner<StatNode, LexLocation>
    {
        protected virtual bool yywrap() { return true; }

        protected StatLiteral MakeLiteral(string lit) => new StatLiteral()
        {
            Literal = lit
        };

        protected StatLiteral MakeString(string lit)
        {
            return MakeLiteral(Regex.Unescape(lit.Substring(1, lit.Length - 2)));
        }

        protected StatProperty MakeDataProperty(string lit)
        {
            var re = new Regex(@"data\s+""([^""]+)""\s+""(.*)""\s*", RegexOptions.CultureInvariant);
            var matches = re.Match(lit);
            if (!matches.Success)
            {
                throw new Exception("Stat data entry match error");
            }

            return new StatProperty
            {
                Key = matches.Groups[1].Value,
                Value = matches.Groups[2].Value
            };
        }
    }

    public partial class StatParser
    {
        public StatParser(StatScanner scnr) : base(scnr)
        {
        }

        public StatDeclarations GetDeclarations()
        {
            return CurrentSemanticValue as StatDeclarations;
        }

        private StatDeclarations MakeDeclarationList() => new StatDeclarations();

        private StatDeclarations AddDeclaration(StatNode declarations, StatNode declaration)
        {
            var decls = declarations as StatDeclarations;
            var decl = declaration as StatDeclaration;
            decls.Declarations.Add(decl);
            return decls;
        }

        private StatDeclaration MakeDeclaration() => new StatDeclaration();

        private StatDeclaration MakeDeclaration(StatProperty[] properties)
        {
            var decl = new StatDeclaration();
            foreach (var prop in properties)
            {
                AddProperty(decl, prop);
            }

            return decl;
        }

        private StatWrappedDeclaration WrapDeclaration(StatNode node) => new StatWrappedDeclaration
        {
            Declaration = node as StatDeclaration
        };

        private StatDeclaration MergeItemCombo(StatNode comboNode, StatNode resultNode)
        {
            var combo = comboNode as StatDeclaration;
            var result = resultNode as StatDeclaration;
            foreach (var kv in result.Properties)
            {
                if (kv.Key != "EntityType" && kv.Key != "Name")
                {
                    combo.Properties[kv.Key] = kv.Value;
                }
            }

            return combo;
        }

        private StatDeclaration AddProperty(StatNode declaration, StatNode property)
        {
            var decl = declaration as StatDeclaration;
            if (property is StatProperty)
            {
                var prop = property as StatProperty;
                decl.Properties[prop.Key] = prop.Value;
            }
            else if (property is StatElement)
            {
                var ele = property as StatElement;
                object cont;
                if (!decl.Properties.TryGetValue(ele.Collection, out cont))
                {
                    cont = new List<object>();
                    decl.Properties[ele.Collection] = cont;
                }

                (cont as List<object>).Add(ele.Value);
            }
            else if (property is StatDeclaration)
            {
                var otherDecl = property as StatDeclaration;
                foreach (var kv in otherDecl.Properties)
                {
                    decl.Properties[kv.Key] = kv.Value;
                }
            }
            else
            {
                throw new Exception("Unknown property type");
            }

            return decl;
        }

        private StatProperty MakeProperty(StatNode key, StatNode value) => new StatProperty()
        {
            Key = (key as StatLiteral).Literal,
            Value = (value as StatLiteral).Literal
        };

        private StatProperty MakeProperty(String key, StatNode value) => new StatProperty()
        {
            Key = key,
            Value = (value as StatLiteral).Literal
        };

        private StatProperty MakeProperty(String key, String value) => new StatProperty()
        {
            Key = key,
            Value = value
        };

        private StatElement MakeElement(String key, StatNode value)
        {
            if (value is StatLiteral)
            {
                return new StatElement()
                {
                    Collection = key,
                    Value = (value as StatLiteral).Literal
                };
            }
            else if (value is StatCollection)
            {
                return new StatElement()
                {
                    Collection = key,
                    Value = (value as StatCollection).Collection
                };
            }
            else if (value is StatDeclaration)
            {
                return new StatElement()
                {
                    Collection = key,
                    Value = (value as StatDeclaration).Properties
                };
            }
            else
            {
                throw new Exception("Unknown stat element type");
            }
        }

        private StatElement MakeElement(String key, object value) => new StatElement()
        {
            Collection = key,
            Value = value
        };

        private StatCollection MakeCollection() => new StatCollection
        {
            Collection = new List<object>()
        };

        private StatCollection AddElement(StatNode collection, StatNode element)
        {
            var coll = collection as StatCollection;
            var ele = element as StatLiteral;
            coll.Collection.Add(ele.Literal);

            return coll;
        }

        private string Unwrap(StatNode node) => (node as StatLiteral).Literal;
    }
}