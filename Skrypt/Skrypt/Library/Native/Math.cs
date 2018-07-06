﻿using System;
using SysMath = System.Math;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skrypt.Execution;

namespace Skrypt.Library.Native {
    partial class System {
        public class Math {
            public static Numeric PI = new Numeric(SysMath.PI);
            public static Numeric E = new Numeric(SysMath.E);

            public static SkryptObject Round(SkryptObject[] Values) {
                var a = TypeConverter.ToNumeric(Values, 0);

                return (Numeric)SysMath.Round(a);
            }

            public static SkryptObject Floor(SkryptObject[] Values) {
                var a = TypeConverter.ToNumeric(Values, 0);

                return (Numeric)SysMath.Floor(a);
            }

            public static SkryptObject Ceil(SkryptObject[] Values) {
                var a = TypeConverter.ToNumeric(Values, 0);

                return (Numeric)SysMath.Ceiling(a);
            }

            public static SkryptObject Abs(SkryptObject[] Values) {
                var a = TypeConverter.ToNumeric(Values, 0);

                return (Numeric)SysMath.Abs(a);
            }

            public static SkryptObject Min(SkryptObject[] Values) {
                var a = TypeConverter.ToNumeric(Values, 0);
                var b = TypeConverter.ToNumeric(Values, 1);

                return (Numeric)SysMath.Min(a, b);
            }

            public static SkryptObject Max(SkryptObject[] Values) {
                var a = TypeConverter.ToNumeric(Values, 0);
                var b = TypeConverter.ToNumeric(Values, 1);

                return (Numeric)SysMath.Max(a, b);
            }

            public static SkryptObject Clamp(SkryptObject[] Values) {
                var x = TypeConverter.ToNumeric(Values, 0);
                var a = TypeConverter.ToNumeric(Values, 1);
                var b = TypeConverter.ToNumeric(Values, 2);

                return (Numeric)SysMath.Min(SysMath.Max(x, a), b);
            }
        }
    }
}
