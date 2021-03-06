﻿using System.Linq;
using Skrypt.Engine;
using Skrypt.Execution;

namespace Skrypt.Library.Native
{
    partial class System
    {
        [Constant, Static]
        public class Statistics : SkryptObject
        {
            [Constant]
            public static SkryptObject Mode(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var v = a.List;

                var query = v.GroupBy(x => ((Numeric)x).Value)
                    .Select(group => new { Value = group.Key, Count = group.Count() })
                    .OrderByDescending(x => x.Count);

                var item = query.First();

                return engine.Create<Numeric>(item.Value);
            }

            [Constant]
            public static SkryptObject Mean(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var total = 0d;

                for (int i = 0; i < a.List.Count; i++)
                {
                    total += (Numeric)a.List[i];
                }

                return engine.Create<Numeric>(total / a.List.Count);
            }

            [Constant]
            public static SkryptObject Range(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);

                var sorted = a.Sort(engine, a, null);

                double high = (Numeric)((Array)sorted).List.Last();
                double low = (Numeric)((Array)sorted).List.First();

                return engine.Create<Numeric>(high - low);
            }

            [Constant]
            public static SkryptObject Sort(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);

                return a.Sort(engine, a, null);
            }

            [Constant]
            public static SkryptObject CountNotEmpty(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var notEmpty = 0;
                for (int i = 0; i < a.List.Count; i++)
                {
                    if (a.List[i] == null || a.List[i] == (String)"")
                    {
                        notEmpty--;
                    }
                    else
                    {
                        notEmpty++;
                    }
                }

                return engine.Create<Numeric>(notEmpty);
            }

            [Constant]
            public static SkryptObject CountEmpty(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var empty = 0;

                for (int i = 0; i < a.List.Count; i++)
                {
                    if (a.List[i] == null || a.List[i] == (String)"")
                    {
                        empty++;
                    }
                    else
                    {
                        empty--;
                    }
                }

                return engine.Create<Numeric>(empty);
            }

            [Constant]
            public static SkryptObject Large(SkryptEngine engine, SkryptObject self, SkryptObject[] values) {
                var a = (Array)TypeConverter.ToArray(values, 0).Clone(); // Copy array so we don't affect the original one by sorting it
                var k = TypeConverter.ToNumeric(values, 1);

                a.List.Sort((x, y) => {
                    if ((Numeric)x > (Numeric)y) {
                        return 1;
                    }
                    else {
                        return -1;
                    }
                });

                return engine.Create<Numeric>(a.List[(int)k]);
            }

            [Constant]
            public static SkryptObject Small(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = (Array)TypeConverter.ToArray(values, 0).Clone(); // Copy array so we don't affect the original one by sorting it
                var k = TypeConverter.ToNumeric(values, 1);

                a.List.Sort((x, y) => {
                    if ((Numeric)x > (Numeric)y) {
                        return 1;
                    }
                    else {
                        return -1;
                    }
                });

                return engine.Create<Numeric>(a.List[a.List.Count-(int)k-1]);
            }

            [Constant]
            public static SkryptObject Min(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var b = double.MaxValue;

                for (int i = 0; i < a.List.Count; i++) {
                    if ((Numeric)a.List[i] < b) {
                        b = (Numeric)a.List[i];
                    }
                }

                return engine.Create<Numeric>(b);
            }

            [Constant]
            public static SkryptObject Max(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var b = -double.MaxValue;
                for (int i = 0; i < a.List.Count; i++) {
                    if ((Numeric)a.List[i] > b) {
                        b = (Numeric)a.List[i];
                    }
                }

                return engine.Create<Numeric>(b);
            }

            [Constant]
            public static SkryptObject MinIndex(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var b = double.MaxValue;
                var c = 0;

                for (int i = 0; i < a.List.Count; i++)
                {
                    if ((Numeric)a.List[i] < b)
                    {
                        b = (Numeric)a.List[i];
                        c = i;
                    }
                }

                return engine.Create<Numeric>(c);
            }

            [Constant]
            public static SkryptObject MaxIndex(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var b = -double.MaxValue;
                var c = 0;

                for (int i = 0; i < a.List.Count; i++)
                {
                    if ((Numeric)a.List[i] > b)
                    {
                        b = (Numeric)a.List[i];
                        c = i;
                    }
                }

                return engine.Create<Numeric>(c);
            }


            [Constant]
            public SkryptObject Sum(SkryptEngine engine, SkryptObject self, SkryptObject[] values)
            {
                var a = TypeConverter.ToArray(values, 0);
                var b = 0d;
                for (int i = 0; i < a.List.Count; i++) {
                    b += (Numeric)a.List[i];
                }

                return engine.Create<Numeric>(b);
            }
        }
    }
}