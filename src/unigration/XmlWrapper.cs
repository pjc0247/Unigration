using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

public class XmlWrapper
{
    public static T Load<T>(string xml)
    {
        var serializer = new XmlSerializer(typeof(T));
        
        return (T)serializer.Deserialize(new StringReader(xml));
    }
    public static string Dump(object obj)
    {
        var serializer = new XmlSerializer(obj.GetType());
        var sw = new StringWriter();

        serializer.Serialize(sw, obj);

        return sw.GetStringBuilder().ToString();
    }
}