using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InfinityBot
{
    public class TypeConverter
    {

        public static object ConvertParameter(ParameterInfo parameter, string param)
        {
            switch (parameter.ParameterType.Name)
            {
                case "String":
                    {
                        return param;
                    }
                case "Int32":
                    {
                        try
                        {
                            return Convert.ToInt32(param);
                        }
                        catch
                        {
                            return 0;
                        }
                    }
                case "Double":
                    {
                        try
                        {
                            return Convert.ToDouble(param);
                        }
                        catch
                        {
                            return 0;
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
