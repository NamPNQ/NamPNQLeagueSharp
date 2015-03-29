using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;
using System.Diagnostics;

using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Sandbox;
using System.Drawing;
using SharpDX;

using Microsoft.Build.Evaluation;

namespace EasyAssemblyManager
{
    internal class Program
    {
        private static string WelcomeMsg = ("<font color = '#cc33cc'>EasyAssemblyManager</font><font color='#FFFFFF'> by NamPNQ.</font> <font color = '#66ff33'> ~~ LOADED ~~</font>");
        private static Menu _menu;
        private static string _leagueSharpDirectory;
        private static string _assembliesDirectory;
        private static Domain _domainProxy;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Game.PrintChat(WelcomeMsg);
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            
            try
            {
                Process loaderProcess = ( from p in Process.GetProcesses()
                    where (p.ProcessName == "loader" && p.MainWindowTitle == "LeagueSharp")
                    select p ).FirstOrDefault();
                if (loaderProcess == null){
                    throw new Exception("Not found loader process");
                }
                _leagueSharpDirectory = Path.GetDirectoryName(loaderProcess.MainModule.FileName);
            }
            catch (Exception ee)
            {
                Console.WriteLine(@"Could not resolve LeagueSharp directory: " + ee);
                return;
            }
            Console.WriteLine(@"[Debug]: _leagueSharpDirectory " + _leagueSharpDirectory);

            try{
                _assembliesDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _assembliesDirectory = Directory.GetParent(Path.GetDirectoryName(_assembliesDirectory)).FullName;
                _assembliesDirectory = Path.Combine(_assembliesDirectory, "1");
            }
            catch (Exception ee)
            {
                Console.WriteLine(@"Could not resolve assemblies directory: " + ee);
                return;
            }
            
            Console.WriteLine(@"[Debug]: _assembliesDirectory " + _assembliesDirectory);

            _menu = new Menu("EasyAssemblyManager", "menu", true);
            _domainProxy =  new Domain();

            var configFile = Path.Combine(_leagueSharpDirectory, "config.xml");
            try
            {
                if (File.Exists(configFile))
                {
                    var config = new XmlDocument();
                    config.Load(configFile);
                    var node = config.DocumentElement.SelectSingleNode("/Config/SelectedProfile/InstalledAssemblies");
                    var indexItem = 0;
                    foreach (XmlElement element in node.ChildNodes)
                    {
                        var _childNodes = element.ChildNodes.Cast<XmlElement>();
                        var _assemblyName = _childNodes.First(e => e.Name == "Name").InnerText;
                        var _assemblyInjectChecked = bool.Parse(_childNodes.First(e => e.Name == "InjectChecked").InnerText);
                        var _assemblyPathToProjectFile = _childNodes.First(e => e.Name == "PathToProjectFile").InnerText;
                        var _project = new Project(_assemblyPathToProjectFile);
                        if (!_project.GetPropertyValue("OutputType").ToLower().Contains("exe")){
                            continue;
                        }
                        string[] filePaths = Directory.GetFiles(_assembliesDirectory, '*' + GetOutputFile(_project));
                        if (filePaths.Length <= 0){
                            continue;
                        }
                        var _assemblyPathToBinary = filePaths[0];
                        var _assemblyMenuItem = new MenuItem(_assemblyName, _assemblyName).SetValue(_assemblyInjectChecked);
                        _assemblyMenuItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                        {
                            if (eventArgs.GetNewValue<bool>()){
                                _domainProxy.Load(_assemblyPathToBinary, new string[0]);
                            }
                            else{
                                Game.PrintChat("Not found function for unload assembly. Waiting in future...");
                            }
                            
                        };
                        _menu.SubMenu("Assemblies "+ (((int) indexItem/10) +1)).AddItem(_assemblyMenuItem);
                        indexItem++;
                    }
                }
                else{
                    Console.WriteLine(@"[WARNING]: Not found" + configFile);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(@"[ERROR]: " + e.ToString());
            }

            _menu.AddToMainMenu();
           
        }

        public static string GetOutputFile(Project project)
        {
            if (project != null)
            {
                var extension = project.GetPropertyValue("OutputType").ToLower().Contains("exe")
                    ? ".exe"
                    : (project.GetPropertyValue("OutputType").ToLower() == "library" ? ".dll" : string.Empty);
                return project.GetPropertyValue("AssemblyName") + extension;
            }
            return string.Empty;
        }

    }
}

