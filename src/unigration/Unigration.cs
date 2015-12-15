using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection; 

[InitializeOnLoad]
public class Unigration : AssetPostprocessor
{
    private static readonly string DataPath = "Assets/root.unigration";

    private static UnigrationData data { get; set; }

    static Unigration()
    {
        if (File.Exists(DataPath))
        {
            if (data == null)
            {
                data = LoadData(DataPath);

                Debug.Log("UnigrationData loaded - " + (data.nodes.Count).ToString());
                foreach(var node in data.nodes)
                {
                    Debug.Log(node.name + " / " + node.version); 
                }
            }

            return;
        }
         
        /*
        var unis = Directory.GetFiles("Assets/", "*.unigration", SearchOption.AllDirectories);

        foreach (var path in unis)
        {
            var node = LoadNode(path);
        }
        */
    }

    public static UnigrationNode GetPackage(string name)
    {
        return data.nodes.Where(m => m.name == name).First();
    }
    public static List<UnigrationNode> GetPackages()
    {
        return data.nodes;
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach(var path in importedAssets)
        {
            if (path == DataPath)
                continue;

            if (path.EndsWith(".unigration"))
            {
                Debug.Log("unigration file found - " + path);

                var node = LoadNode(path);
                var old = data.nodes.Where(m => m.name == node.name).FirstOrDefault();
                
                if (old == null)
                    continue;

                Migrate(old, node);

                old.version = node.version;
            }
        }

        SaveData(DataPath);
    }

    private static void Migrate(UnigrationNode old, UnigrationNode to)
    {
        if (!to.IsGreaterThan(old))
            return;

        Debug.Log("Migrate " + old.version + " -> " + to.version);

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.Namespace != old.container)
                continue;

            var md = new Regex("^[a-zA-Z]+_([0-9_]+)$").Match(type.Name);
            if (md.Groups.Count == 1)
            {
                Debug.Log("Malformed migration script : " + type.Namespace + "::" + type.Name);
                continue;
            }

            var version = md.Groups[1].Value.Replace('_', '.');
            var up = type.GetMethod("Up");
            var since = type.GetMethod("Since");

            if (to.IsGreaterThan(version))
            {
                if (since != null)
                    since.Invoke(null, new object[] { old.version });
            }
            if(old.IsGreaterThan(version))
            {
                if (up != null)
                    up.Invoke(null, new object[] { old.version });
            }
        }
    }

    private static UnigrationNode LoadNode(string path)
    {
        return XmlWrapper.Load<UnigrationNode>(
            File.ReadAllText(path));
    }
    private static UnigrationData LoadData(string path)
    {
        return XmlWrapper.Load<UnigrationData>(
            File.ReadAllText(path));
    }
    private static void SaveData(string path)
    {
        File.WriteAllText(path, XmlWrapper.Dump(data));
    }
} 

public class UnigrationNode
{
    public string name { get; set; }
    public string container { get; set; }
    public string path { get; set; }
    public string version { get; set; }

    public bool IsGreaterThan(UnigrationNode o)
    {
        return new Version(version) > new Version(o.version);
    }
    public bool IsGreaterThan(string v)
    {
        return new Version(version) > new Version(v);
    }
}
public class UnigrationData
{
    public List<UnigrationNode> nodes { get; set; }
}