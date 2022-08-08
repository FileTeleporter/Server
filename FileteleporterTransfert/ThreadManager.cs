using System;
using System.Collections.Generic;
using System.Text;
using FileteleporterTransfert.Tools;

namespace FileteleporterTransfert
{
    class ThreadManager
    {
        private static readonly List<Action> executeOnMainThread = new();
        private static readonly List<Action> executeCopiedOnMainThread = new();
        private static bool _actionToExecuteOnMainThread;

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action action)
        {
            if (action == null)
            {
                EZConsole.WriteLine("ThreadManager", "No action to execute on main thread!");
                return;
            }

            lock (executeOnMainThread)
            {
                executeOnMainThread.Add(action);
                _actionToExecuteOnMainThread = true;
            }
        }

        /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
        public static void UpdateMain()
        {
            if (!_actionToExecuteOnMainThread)
            {
                return;
            }

            executeCopiedOnMainThread.Clear();
            lock (executeOnMainThread)
            {
                executeCopiedOnMainThread.AddRange(executeOnMainThread);
                executeOnMainThread.Clear();
                _actionToExecuteOnMainThread = false;
            }

            foreach (var t in executeCopiedOnMainThread)
            {
                t();
            }

        }
    }
}