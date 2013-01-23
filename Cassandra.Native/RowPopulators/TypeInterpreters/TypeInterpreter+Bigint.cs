﻿using System;

namespace Cassandra
{
    internal partial class TypeInterpreter
    {
        public static object ConvertFromBigint(IColumnInfo type_info, byte[] value)
        {
            Array.Reverse(value);
            return BitConverter.ToInt64(value, 0);            
        }

        public static Type GetTypeFromBigint(IColumnInfo type_info)
        {
            return typeof(long);
        }

        public static byte[] InvConvertFromBigint(IColumnInfo type_info, object value)
        {
            CheckArgument<long>(value);
            return ConversionHelper.ToBytesFromInt64((long)value);
        }
    }
}
