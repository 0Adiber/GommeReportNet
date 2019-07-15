using System;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Collections.Generic;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.Protocols.Classes.Base;

namespace GommeRepoNet_Master.Tasks
{
    class Order : ITask {

        readonly List<string> accounts = new List<string>();
        readonly string[] keys;
        readonly string[] authorised;
        readonly string cmd;

        bool waiting = false;
        int recY = 0;
        int recN = 0;
        int sent = 0;

        string command;
        string badGuy;

        List<string> tempAccs = new List<string>();
        
        public Order(string[] accounts, string[] keys, string cmd, string[] authorised) {
            this.accounts = accounts.ToList<string>();
            this.keys = keys;
            this.cmd = cmd;
            this.authorised = authorised;
        }

        public override bool Exec()
        {
           return true;
        }

        public override void Start() { player.events.onChat += OnChat; }
        public override void Stop() { player.events.onChat -= OnChat; }

        private void OnChat(IPlayer player, IChat message, byte position)
        {
            string msg = message.GetText();

            if (msg.StartsWith("[Clans]"))
            {
                if (!msg.Contains(":")) return;
                string[] parts = msg.Split(new char[] { ':' });
                if (!parts[1].Trim().StartsWith(".")) return;

                string com = null;
                string sender = "";

                //check if the reporter is authorised and get the person to report
                for (int i = 0; i < authorised.Length; i++)
                {
                    if (!parts[0].ToLower().Contains(authorised[i])) continue;
                    sender = authorised[i];
                    for (int j = 0; j < keys.Length; j++)
                    {
                        if (!parts[1].Trim().ToLower().Contains(keys[j])) continue;
                        com = parts[1].Trim().ToLower().Split(new char[] { ' ' })[1];
                        break;
                    }
                }

                //check if already report in progress
                if(waiting)
                {
                    player.functions.Chat("/cc [GommeReportNet] Es befindet sich gerade ein Report in arbeit. Bitte warte noch!");
                    return;
                }

                //check if there is a person to be reported
                if (!string.IsNullOrEmpty(com))
                {
                    //check if someone from the authorised users get reportet
                    if(authorised.Contains(com))
                    {
                        player.functions.Chat("/cc [GommeReportNet] Nicht cool...");
                        return;
                    }

                    player.functions.Chat("/clan jump " + sender);

                    Thread.Sleep(2000);//sleep, to make sure he joined the game serve

                    //if (1 == 2)
                    //{
                    //    player.functions.Chat("/cc Safe:" + safe.ToString());
                    //    player.functions.Chat("/cc Current:" + current.ToString());

                    //    player.functions.Chat("/cc [GommeReportNet] Konnte dem Gameserver nicht beitreten :( - Bitte versuche es später erneut.");
                    //    return;
                    //}

                    player.functions.Chat("/" + cmd.Replace("%to_report%", com));
                    sent = 0;
                    recY = 0;
                    recN = 0;

                    tempAccs.Clear();
                    tempAccs = new List<string>(accounts);

                    command = cmd;
                    badGuy = com;

                    sendReport(player, command, badGuy, tempAccs.ElementAt(0));
                    
                    waiting = true;
                }

            } else if(msg.StartsWith("[Friends]"))
            {
                msg = msg.Replace("[Friends]", "");
                string[] parts = msg.Split(new char[] { ':' });
                if (msg.Trim().Equals("yes"))
                {
                    recY++;
                }
                if (msg.Trim().Equals("no"))
                {
                    recN++;
                }

                if (tempAccs.Count == 0)
                {
                    player.functions.Chat("/hub");
                    player.functions.Chat("/cc [GommeReportNet] " + (recY + recN) + "/" + sent + " Bots haben geantwortet.");
                    player.functions.Chat("/cc [GommeReportNet] " + recY + "/" + sent + " Bots konnten reporten.");
                    player.functions.Chat("/cc [GommeReportNet] " + recN + "/" + sent + " Bots konnten nicht reporten.");

                    waiting = false;
                    return;
                }
                sendReport(player, command, badGuy, tempAccs.ElementAt(0));
            }
        }

        private void sendReport(IPlayer player, string cmd, string com, string bot)
        {
            player.functions.Chat("/msg " + bot + " /" + cmd.Replace("%to_report%", com));
            sent++;
            tempAccs.RemoveAt(0);
        }

    }
}
