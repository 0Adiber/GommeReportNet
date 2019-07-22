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
        //sent to bot, but check if offline
        bool trying_to_report = false;
        int recY = 0;
        int recN = 0;
        int sent = 0;

        string command;
        string badGuy;

        long last_heard = 0;

        List<string> tempAccs = new List<string>();


        List<string> accounts_already_responded = new List<string>();
        string account_to_respond = "";


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

        public override void Start() { player.events.onChat += OnChat; player.events.onTick += onTick; }
        public override void Stop() { player.events.onChat -= OnChat; player.events.onTick -= onTick; }

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

                    //".report stop" -> stop report process
                    if (parts[1].Trim().ToLower().Split(new char[] { ' ' })[1].Equals("stop"))    //kann abstürzen, weil keine überprüfung, ob zwei teile vorhanden
                    {
                        waiting = false;
                        trying_to_report = false;
                        player.functions.Chat("/cc [GommeReportNet] Stopped Report Process.");
                        return;
                    }

                    for (int j = 0; j < keys.Length; j++)
                    {
                        if (!parts[1].Trim().ToLower().Contains(keys[j])) continue;
                        com = parts[1].Trim().ToLower().Split(new char[] { ' ' })[1];   //kann abstürzen, weil keine überprüfung, ob zwei teile vorhanden
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

                    Thread.Sleep(2000);//sleep, to make sure he joined the game server

                    command = cmd;
                    badGuy = com;
                    waiting = true;
                    player.functions.Chat("/" + cmd.Replace("%to_report%", com));
                }

            } else if(msg.StartsWith("[Friends]"))
            {
                //check if the bot is offline
                if(msg.Contains("currently offline"))
                {
                    trying_to_report = false;
                } else if (!msg.Contains(":")) return;

                msg = msg.Replace("[Friends]", "");
                string[] parts = msg.Split(new char[] { ':' });

                if (parts.Length > 1)   //make sure, that there are two+ parts
                {
                    if(accounts_already_responded.Contains(parts[0].Trim()))
                    {
                        return;
                    }
                    if (parts[1].Contains("yes"))
                    {
                        recY++;
                        trying_to_report = false;
                    }
                    if (parts[1].Contains("no"))
                    {
                        recN++;
                        trying_to_report = false;
                    }
                }

                //ending
                if (tempAccs.Count == 0 && waiting && !trying_to_report)
                {
                    player.functions.Chat("/hub");
                    player.functions.Chat("/cc [GommeReportNet] " + (recY + recN) + "/" + sent + " Bots haben geantwortet.");
                    player.functions.Chat("/cc [GommeReportNet] " + recY + "/" + sent + " Bots konnten reporten.");
                    player.functions.Chat("/cc [GommeReportNet] " + recN + "/" + sent + " Bots konnten nicht reporten.");

                    waiting = false;
                    return;
                }

                //"loop"
                if (tempAccs.Count > 0 && waiting && !trying_to_report)
                {
                    sendReport(player, command, badGuy);
                }
            } else if(msg.StartsWith("[Guardian]"))
            {
                //check if player is reportable
                if(msg.Contains("Thank you for your report"))
                {
                    startReportProcess(player);
                }
                //check if player wanted to report is there
            } else if(msg.Contains("This player could not be found."))
            {
                player.functions.Chat("/cc [GommeReportNet] Stopping Report: <Player not found>");
                waiting = false;
                return;
            }
        }

        private void sendReport(IPlayer player, string cmd, string com)
        {
            player.functions.Chat("/msg " + tempAccs.ElementAt(0) + " /" + cmd.Replace("%to_report%", com));
            account_to_respond = tempAccs.ElementAt(0);//we expect a respond from this guy within 20 seconds
            last_heard = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            sent++;
            tempAccs.RemoveAt(0);
            trying_to_report = true;
        }

        private void startReportProcess(IPlayer player)
        {
            sent = 0;
            recY = 0;
            recN = 0;

            tempAccs.Clear();
            tempAccs = new List<string>(accounts);

            sendReport(player, command, badGuy);
        }

        private void onTick(IPlayer player)
        {
            if(waiting && trying_to_report)
            {
                if((last_heard+20000) <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    trying_to_report = false;
                    accounts_already_responded.Add(account_to_respond);
                    sendReport(player, cmd, badGuy);
                }
            }
        }

    }
}
