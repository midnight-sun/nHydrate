#pragma warning disable 0168
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using nHydrate.Dsl;

namespace nHydrate.Dsl.Design.Converters
{
    internal class TextLengthConverter : TypeConverter
    {
        public TextLengthConverter()
        {
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            try
            {
                if (destinationType == typeof(string))
                {
                    var retval = string.Empty;
                    if (context.Instance is nHydrate.Dsl.Field)
                    {
                        var column = context.Instance as nHydrate.Dsl.Field;
                        if (column.DataType.SupportsMax())
                        {
                            if (column.Length == 0) retval = "max";
                            else retval = column.Length.ToString();
                        }
                        else if (column.DataType.GetPredefinedSize() != -1)
                        {
                            retval = "predefined";
                        }
                        else
                        {
                            retval = column.Length.ToString();
                        }
                    }
                    else if (context.Instance is nHydrate.Dsl.StoredProcedureField)
                    {
                        var column = context.Instance as nHydrate.Dsl.StoredProcedureField;
                        if (column.DataType.SupportsMax())
                        {
                            if (column.Length == 0) retval = "max";
                            else retval = column.Length.ToString();
                        }
                        else if (column.DataType.GetPredefinedSize() != -1)
                        {
                            retval = "predefined";
                        }
                        else
                        {
                            retval = column.Length.ToString();
                        }
                    }
                    else if (context.Instance is nHydrate.Dsl.StoredProcedureParameter)
                    {
                        var column = context.Instance as nHydrate.Dsl.StoredProcedureParameter;
                        if (column.DataType.SupportsMax())
                        {
                            if (column.Length == 0) retval = "max";
                            else retval = column.Length.ToString();
                        }
                        else if (column.DataType.GetPredefinedSize() != -1)
                        {
                            retval = "predefined";
                        }
                        else
                        {
                            retval = column.Length.ToString();
                        }
                    }
                    else if (context.Instance is nHydrate.Dsl.ViewField)
                    {
                        var column = context.Instance as nHydrate.Dsl.ViewField;
                        if (column.DataType.SupportsMax())
                        {
                            if (column.Length == 0) retval = "max";
                            else retval = column.Length.ToString();
                        }
                        else if (column.DataType.GetPredefinedSize() != -1)
                        {
                            retval = "predefined";
                        }
                        else
                        {
                            retval = column.Length.ToString();
                        }
                    }
                    else if (context.Instance is nHydrate.Dsl.SecurityFunctionParameter)
                    {
                        var column = context.Instance as nHydrate.Dsl.SecurityFunctionParameter;
                        if (column.DataType.SupportsMax())
                        {
                            if (column.Length == 0) retval = "max";
                            else retval = column.Length.ToString();
                        }
                        else if (column.DataType.GetPredefinedSize() != -1)
                        {
                            retval = "predefined";
                        }
                        else
                        {
                            retval = column.Length.ToString();
                        }
                    }

                    return retval;
                }
            }
            catch (Exception ex) { }
            return null;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (context.Instance is nHydrate.Dsl.Field)
            {
                var column = context.Instance as nHydrate.Dsl.Field;
                if (sourceType == typeof(string))
                    return true;
                else if (sourceType == typeof(int))
                    return true;
                else
                    return false;
            }
            else if (context.Instance is nHydrate.Dsl.StoredProcedureField)
            {
                var column = context.Instance as nHydrate.Dsl.StoredProcedureField;
                if (sourceType == typeof(string))
                    return true;
                else if (sourceType == typeof(int))
                    return true;
                else
                    return false;
            }
            else if (context.Instance is nHydrate.Dsl.StoredProcedureParameter)
            {
                var column = context.Instance as nHydrate.Dsl.StoredProcedureParameter;
                if (sourceType == typeof(string))
                    return true;
                else if (sourceType == typeof(int))
                    return true;
                else
                    return false;
            }
            else if (context.Instance is nHydrate.Dsl.ViewField)
            {
                var column = context.Instance as nHydrate.Dsl.ViewField;
                if (sourceType == typeof(string))
                    return true;
                else if (sourceType == typeof(int))
                    return true;
                else
                    return false;
            }
            else if (context.Instance is nHydrate.Dsl.SecurityFunctionParameter)
            {
                var column = context.Instance as nHydrate.Dsl.SecurityFunctionParameter;
                if (sourceType == typeof(string))
                    return true;
                else if (sourceType == typeof(int))
                    return true;
                else
                    return false;
            }

            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                int v;
                if (int.TryParse(value.ToString(), out v))
                {
                    return v;
                }
                else if (value.ToString().ToLower() == "max")
                {
                    return 0;
                }
            }
            return 0;
        }

    }
}