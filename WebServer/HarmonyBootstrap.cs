using HarmonyLib;
using ParadimeWeb.WorkflowGen.WebServer;
using System;
using System.Reflection;
using System.Web;

[assembly: PreApplicationStartMethod(typeof(HarmonyBootstrap), "Start")]

namespace ParadimeWeb.WorkflowGen.WebServer
{
    public static class HarmonyBootstrap
    {
        static bool _done;
        public static void Start()
        {
            if (_done) return; _done = true;
            new Harmony("com.paradimeweb.wfgen.jwtpatch").PatchAll();
        }
    }
}
