using BepInEx;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System;
using R2API;
using R2API.ContentManagement;
using BepInEx.Logging;
using ArtifactOfConsole.Artifact;

namespace ArtifactOfConsole
{
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2APIContentManager.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        public const string PluginAuthor = "HIFU";
        public const string PluginName = "ArtifactOfConsole";
        public const string PluginVersion = "1.1.0";

        public static AssetBundle artifactofconsole;

        public static ManualLogSource ACLogger;

        public void Awake()
        {
            ACLogger = base.Logger;
            artifactofconsole = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace("ArtifactOfConsole.dll", "artifactofconsole"));

            var ArtifactTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ArtifactBase)));

            foreach (var artifactType in ArtifactTypes)
            {
                ArtifactBase artifact = (ArtifactBase)Activator.CreateInstance(artifactType);
                artifact.Init(Config);
            }
        }
    }
}