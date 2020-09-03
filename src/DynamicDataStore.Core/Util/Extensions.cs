using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicDataStore.Core.Util
{
    public static class DynamicExtensions
    {
        public static IEnumerable<dynamic> AsDynamic(this IEnumerable list)
        {
            foreach (var obj in list)
            {
                yield return obj;
            }
        }

        public static MethodBuilder OverrideOnConfiguring(this TypeBuilder tb, string cString)
        {
            MethodBuilder onConfiguringMethod = tb.DefineMethod("OnConfiguring",
                MethodAttributes.Public
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot
                | MethodAttributes.Virtual,
                CallingConventions.HasThis,
                null,
                new[] { typeof(DbContextOptionsBuilder) });

            // the easiest method to pick will be
            // .UseSqlServer(this DbContextOptionsBuilder optionsBuilder, string connectionString, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
            // but since constructing generic delegate seems a bit too much effort we'd rather filter everything else out
            var useSqlServerDatabaseMethodSignature = typeof(SqlServerDbContextOptionsExtensions)
                .GetMethods()
                .Where(m => m.Name == "UseSqlServer")
                .Where(m => m.GetParameters().Length == 3)
                .Where(m => m.GetParameters().Select(p => p.ParameterType).Contains(typeof(DbContextOptionsBuilder)))
                .Single(m => m.GetParameters().Select(p => p.ParameterType).Contains(typeof(string)));

            var enableSensitiveLogging = typeof(DbContextOptionsBuilder)
                .GetMethods()
                .Where(m => m.Name == "EnableSensitiveDataLogging")
                .Where(m => m.GetParameters().Length == 1).Single(p =>
                    p.GetParameters().Select(mp => mp.ParameterType).Contains(typeof(bool)));

            var useLoggerFactory = typeof(DbContextOptionsBuilder)
                .GetMethods().Single(m => m.Name == "UseLoggerFactory");


            var ilCode = onConfiguringMethod.GetILGenerator();
            ilCode.Emit(OpCodes.Ldarg_1);
            ilCode.Emit(OpCodes.Ldstr, cString);
            ilCode.Emit(OpCodes.Ldnull);
            ilCode.Emit(OpCodes.Call, useSqlServerDatabaseMethodSignature);

            ilCode.Emit(OpCodes.Ldc_I4_1);
            ilCode.Emit(OpCodes.Call, enableSensitiveLogging);

            ilCode.Emit(OpCodes.Call, useLoggerFactory);

            ilCode.Emit(OpCodes.Pop);
            ilCode.Emit(OpCodes.Ret);

            return onConfiguringMethod;
        }
    }
}
