using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    static class LinqHelper
    {
        static MethodInfo AnyMethod;
     
        public static System.Linq.Expressions.Expression GetPropertyExpression(ParameterExpression param, Type dataType, string propertyName, out PropertyInfo propertyInfo)
        {
            System.Linq.Expressions.Expression left = null;
            string[] dataFieldArr = propertyName.Split('.');
            System.Linq.Expressions.Expression lastObjectExpress = param;
            Type currentObjType = dataType;
            propertyInfo = null;
            for (int i = 0; i < dataFieldArr.Length; i++)
            {
                propertyInfo = currentObjType.GetProperty(dataFieldArr[i], BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new Exception("属性" + dataFieldArr[i] + "无效");
                left = System.Linq.Expressions.Expression.Property(lastObjectExpress, propertyInfo);
                if (i < dataFieldArr.Length - 1)
                {
                    currentObjType = propertyInfo.PropertyType;
                    lastObjectExpress = left;
                }
            }
            return left;
        }

        public static object InvokeWhereEquals(object linqQuery, string propertyName, object value)
        {
            Type dataType = linqQuery.GetType().GetGenericArguments()[0];
            ParameterExpression param = System.Linq.Expressions.Expression.Parameter(dataType, "n");
            System.Reflection.PropertyInfo pinfo;
            System.Linq.Expressions.Expression left, right;

            left = GetPropertyExpression(param, dataType, propertyName, out pinfo);
            if (pinfo.PropertyType.GetTypeInfo().IsGenericType)
            {
                Type ptype = pinfo.PropertyType.GetGenericArguments()[0];
                left = System.Linq.Expressions.Expression.Convert(left, ptype);
                //等式右边的值
                right = System.Linq.Expressions.Expression.Constant(Convert.ChangeType(value, ptype));
            }
            else
            {
                //等式右边的值
                right = System.Linq.Expressions.Expression.Constant(Convert.ChangeType(value, pinfo.PropertyType));
            }

            System.Linq.Expressions.Expression expression = System.Linq.Expressions.Expression.Equal(left, right);
            expression = System.Linq.Expressions.Expression.Lambda(expression, param);

            Type queryableType = typeof(System.Linq.Queryable);
            var methods = queryableType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            foreach (System.Reflection.MethodInfo method in methods)
            {
                if (method.Name == "Where" && method.IsGenericMethod)
                {
                    System.Reflection.MethodInfo mmm = method.MakeGenericMethod(dataType);
                    return mmm.Invoke(null, new object[] { linqQuery, expression });
                }
            }
            return null;
        }

        public static bool InvokeAny(object linqQuery)
        {
            Type dataType = linqQuery.GetType().GetGenericArguments()[0];
            if (AnyMethod == null)
            {
                Type queryType = typeof(System.Linq.Queryable);
                var methods = queryType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(m => m.Name == "Any").ToArray();
                foreach (System.Reflection.MethodInfo method in methods)
                {
                    if (method.IsGenericMethod)
                    {
                        AnyMethod = method;
                        break;
                    }
                }
            }
            if (AnyMethod == null)
                throw new Exception("找不到泛型Any方法");

            System.Reflection.MethodInfo mmm = AnyMethod.MakeGenericMethod(dataType);
            if (mmm != null)
            {
                return (bool)mmm.Invoke(null, new object[] { linqQuery });
            }
            else
                throw new Exception(AnyMethod.Name + ".MakeGenericMethod失败，参数类型：" + dataType.FullName);
        }

    }
}
