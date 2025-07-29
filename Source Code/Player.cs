using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using ProtoBuf;

namespace Bot
{
    public class Player
    {
        public void Run()
        {
            string prompt = "";
            int consecutiveNonResponses = 0;
            API.Load();
            if (API.apiKey == "") return;

            API.LoadCache();
            if (API.HasPromptHistory())
            {
                Program.WriteLine("Loaded previous conversation context.");
            }


            while  (string.IsNullOrEmpty(prompt))
            {
                Program.WriteLine("Say something:\n");
                prompt = Console.ReadLine().Trim();
            }

            

            while (true)
            {
                try
                {
                    Live.LoadScreen();

                    Program.WriteLine("\nProcessing response...\n");
                    LiveActions actions = API.GetResponse(prompt);
                    prompt = "";

                    if (actions != null && actions.Any())
                    {
                        bool requestResponse = false;
                        foreach (LiveAction action in actions.liveActions)
                        {
                            Live.PerformAction(action);

                            if (action.requestResponse)
                            {
                                requestResponse = true;
                            }
                            Live.RandomDelay();
                        }

                        Program.WriteLine("From " + API.aiName + ": " + actions.explanation + "\n", true);

                        Thread.Sleep(1000);

                        if (requestResponse || consecutiveNonResponses >= API.maxNonResponse)
                        {
                            Program.WriteLine(API.aiName + " requests your response:\n");
                            Program.WriteLine(" ");
                            while (prompt == "")
                                prompt = Console.ReadLine();
                            consecutiveNonResponses = 0;
                        }
                        else consecutiveNonResponses++;
                    }
                    else
                    {
                        Program.WriteLine("No valid action received from API.\n", true);
                    }
                }
                catch (Exception e)
                {
                    Program.WriteLine("Exception: \n" + e, true);
                    Console.Read();
                }
                Thread.Sleep(API.reactionTimeMS);
            }
        }
    }
}