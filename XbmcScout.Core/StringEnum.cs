using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;

namespace XbmcScout.Core {
    public static class StringEnum {
        private static Hashtable _stringValues = new Hashtable();

        public static string GetStringValue(Enum value) {
            string output = null;
            Type type = value.GetType();

            //Check first in our cached results...

            if (_stringValues.ContainsKey(value))
                output = (_stringValues[value] as StringValueAttribute).Value;
            else {
                //Look for our 'StringValueAttribute' 

                //in the field's custom attributes

                FieldInfo fi = type.GetField(value.ToString());
                StringValueAttribute[] attrs =
                   fi.GetCustomAttributes(typeof(StringValueAttribute),
                                           false) as StringValueAttribute[];
                if (attrs.Length > 0) {
                    _stringValues.Add(value, attrs[0]);
                    output = attrs[0].Value;
                }
            }

            return output;
        }
    }
}
