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
        readonly string[] authorised;
        readonly string cmd;

        readonly string manu_acc = "MrHunh";

        bool waiting = false;
        //sent to bot, but check if offline
        bool trying_to_report = false;
        int recY = 0;
        int recN = 0;
        int sent = 0;

        //TESTING
        bool testing_net = false; //indicates wheter network is being tested
        Dictionary<string, long> testing_results = new Dictionary<string, long>();
        long testing_time = 0;
        string current_bot_testing = "";
        List<string> testing_accounts = new List<string>();

        string command;
        string badGuy;

        long last_heard = 0;

        List<string> tempAccs = new List<string>();


        List<string> accounts_already_responded = new List<string>();
        string account_to_respond = ""; //also used for bot_not_answered

        //if bot does not answer 3 times in a row
        Dictionary<string, int> bot_not_answered = new Dictionary<string, int>();

        Boolean already_reported_answer_waiting = false;
        long time_already_reported_answer_waiting_asked = 0;


        public Order(string[] accounts, string cmd, string[] authorised) {
            this.accounts = accounts.ToList<string>();
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

                if (parts.Length != 2) return;

                string lCmd = parts[1].Trim().ToLower();

                if (lCmd.Split(new char[] { ' ' }).Length != 2) return;

                if (!lCmd.StartsWith(".report")) return;

                //".report stop" -> stop report process
                if (lCmd.Equals(".report stop"))
                {
                    waiting = false;
                    trying_to_report = false;
                    already_reported_answer_waiting = false;
                    player.functions.Chat("/hub");
                    player.functions.Chat("/cc [GommeReportNet] Report Prozess abgebrochen.");
                    return;
                }

                //".report status" -> status vom report

                if(lCmd.Equals(".report status"))
                {
                    if(waiting != true)
                    {
                        player.functions.Chat("/cc [GommeReportNet] Es ist kein Report in Arbeit!");
                        return;
                    }
                    player.functions.Chat("/cc [GommeReportNet] Aktuell wird " + badGuy + " reportet.");
                    player.functions.Chat("/cc [GommeReportNet] " + sent + "/" + accounts.Count + " Bots haben bis jetzt geantwortet.");
                    return;
                }
                //check if ".report confirm"
                if(lCmd.Equals(".report confirm"))
                {
                    if(already_reported_answer_waiting)
                    {
                        player.functions.Chat("/cc [GommeReportNet] Starte Report...");
                        startReportProcess(player);
                    } else
                    {
                        player.functions.Chat("/cc [GommeReportNet] Dir wurde keine Ja/Nein Frage gestellt!");
                    }
                    already_reported_answer_waiting = false;
                    return;
                }

                //or if ".report deny"
                if(lCmd.Equals(".report deny"))
                {
                    if(already_reported_answer_waiting)
                    {
                        player.functions.Chat("/cc [GommeReportNet] Ok, dann nicht.");
                        player.functions.Chat("/hub");
                    } else
                    {
                        player.functions.Chat("/cc [GommeReportNet] Dir wurde keine Ja/Nein Frage gestellt!");
                    }
                    already_reported_answer_waiting = false;
                    return;
                }

                //or if ".report test" -> test bots and send mrhuhn
                if(lCmd.Equals(".report test"))
                {
                    if(testing_net)
                    {
                        player.functions.Chat("/msg " + manu_acc + " [GommeReportNet] Ich teste schon!");
                        return;
                    }
                    testing_net = true;
                    player.functions.Chat("/msg "+ manu_acc +" [GommeReportNet] Bot-Test gestartet. Max-Response-Time: 10 Sekunden.");

                    testing_accounts = new List<string>(accounts);

                    sendTest(player);

                    while(testing_net)
                    {
                        Thread.Sleep(1000);
                    }

                    //sending it to mrhuhn
                    string not_answered = "";
                    foreach (KeyValuePair<string, long> kvp in testing_results)
                    {
                        if (kvp.Value.Equals(-1))
                        {
                            not_answered += kvp.Value + ",";
                        }
                    }
                    not_answered = String.IsNullOrEmpty(not_answered) ? "0" : not_answered;
                    player.functions.Chat("/msg " + manu_acc + " " + not_answered + " haben nicht geantwortet!"); //kann abkacken, wenn länge von msg zu lang (idk wie lang maximal seind darf)

                    return;
                } else if(lCmd.Equals(".report test stop"))
                {
                    testing_net = false;
                    return;
                }

                string com = null;
                string sender = "";

                //check if the reporter is authorised and get the person to report

                
                for (int i = 0; i < authorised.Length; i++)
                {
                    if (!parts[0].ToLower().Contains(authorised[i]) && !authorised[i].Equals("*")) continue;

                    com = parts[1].Trim().ToLower().Split(new char[] { ' ' })[1];   //kann abstürzen, weil keine überprüfung, ob zwei teile vorhanden
                    break;
                }

                //check if already report in progress
                if(waiting || already_reported_answer_waiting)
                {
                    player.functions.Chat("/cc [GommeReportNet] Es befindet sich gerade ein Report in arbeit. Bitte warte noch!");
                    return;
                }

                //check if there is a person to be reported -> if not, that means, that user has no permission
                if (!string.IsNullOrEmpty(com))
                {
                    //check if someone from the authorised users get reportet
                    if(authorised.Contains(com) || player.status.username.ToLower().Equals(com) || accounts.Contains(com))
                    {
                        player.functions.Chat("/cc [GommeReportNet] Nicht cool...");
                        return;
                    }

                    //set the sender of message
                    sender = parts[0].ToLower().Replace("[Clans]", "").Trim();

                    //jump to the sender
                    player.functions.Chat("/clan jump " + sender.Trim());

                    Thread.Sleep(2000);//sleep, to make sure he joined the game server

                    //making ready to report
                    command = cmd;
                    badGuy = com;
                    waiting = true;
                    player.functions.Chat("/" + cmd.Replace("%to_report%", com));
                } else
                {
                    player.functions.Chat("/cc [GommeReportNet] Du hast definitiv keine Berechtigung dazu!");
                }

            } else if(msg.StartsWith("[Friends]"))
            {
                //check if the bot is offline
                if(msg.Contains("currently offline"))
                {
                    trying_to_report = false;
                    if(bot_not_answered.ContainsKey(account_to_respond))
                    {
                        int temp = bot_not_answered[account_to_respond];
                        bot_not_answered.Remove(account_to_respond);
                        temp++;
                        if(temp >= 3)
                        {
                            player.functions.Chat("/msg " +manu_acc + " " + account_to_respond + " hat 3 mal in Folge nicht geantwortet!");
                            return;
                        } else
                        {
                            bot_not_answered.Add(account_to_respond, temp);
                            return;
                        }
                    } else
                    {
                        bot_not_answered.Add(account_to_respond, 1);
                    }
                } else if (!msg.Contains(":")) return;

                msg = msg.Replace("[Friends]", "");
                string[] parts = msg.Split(new char[] { ':' });

                if (parts.Length > 1)   //make sure, that there are two+ parts
                {
                    string bot = parts[0].Trim();

                    //yes/no respond
                    if (!(accounts_already_responded.Contains(bot))) //check if person already responded
                    {
                        if (parts[1].Contains("yes"))
                        {
                            recY++;
                            trying_to_report = false; //to allow master to send report to next bot
                            //add the bot to already answered list
                            accounts_already_responded.Add(bot);
                            //remove the bot from 3 times not answered
                            if(bot_not_answered.ContainsKey(bot))
                            {
                                bot_not_answered.Remove(bot);
                            }
                        }
                        if (parts[1].Contains("no"))
                        {
                            recN++;
                            trying_to_report = false; //to allow master to send report to next bot
                            //add the bot to already answered list
                            accounts_already_responded.Add(bot);
                            //remove bot from 3 times not answered
                            if (bot_not_answered.ContainsKey(bot))
                            {
                                bot_not_answered.Remove(bot);
                            }
                        }
                    }

                    //test respond
                    if (parts[1].Contains("test back"))
                    {
                        if (current_bot_testing.Equals(bot) && testing_net)
                        {
                            testing_results.Add(current_bot_testing, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - testing_time);
                            testing_accounts.RemoveAt(0);
                            sendTest(player);
                        }
                        return;
                    }
                }

                //ending of report
                endingReport(player);

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
                    player.functions.Chat("/cc [GommeReportNet] Starte Report von " + badGuy + ".");
                    startReportProcess(player);
                } else if(msg.Contains("You have already reported"))
                {
                    time_already_reported_answer_waiting_asked = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    already_reported_answer_waiting = true;
                    player.functions.Chat("/cc [GommeReportNet] Der Spieler wurde bereits reportet, willst du es trotzdem nochmal machen?");
                    player.functions.Chat("/cc [GommeReportNet] Ja: '.report confirm', Nein: '.report deny'");
                }//check if player wanted to report is there
                else if (msg.Contains("This player could not be found."))
                {
                    player.functions.Chat("/cc [GommeReportNet] Stoppe Report: <Spieler nicht gefunden>");
                    waiting = false;
                    return;
                }                
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

        private void sendTest(IPlayer player)
        {
            if(testing_accounts.Count == 0)
            {
                testing_net = false;
                return;
            }
            testing_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            current_bot_testing = testing_accounts.ElementAt(0);
            player.functions.Chat("/msg " + current_bot_testing + " test");
        }

        private void endingReport(IPlayer player)
        {
            if (tempAccs.Count == 0 && waiting && !trying_to_report)
            {
                player.functions.Chat("/hub");
                player.functions.Chat("/cc [GommeReportNet] " + (recY + recN) + "/" + sent + " Bots haben geantwortet.");
                player.functions.Chat("/cc [GommeReportNet] " + recY + "/" + sent + " Bots konnten reporten.");
                player.functions.Chat("/cc [GommeReportNet] " + recN + "/" + sent + " Bots konnten nicht reporten.");

                waiting = false;
                accounts_already_responded.Clear();

                return;
            }
        }

        private void onTick(IPlayer player)
        {
            //if bot does not answer after certain period of time
            if(waiting && trying_to_report)
            {
                if((last_heard+10000) <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    trying_to_report = false;
                    accounts_already_responded.Add(account_to_respond);

                    if (tempAccs.Count > 0)
                    {
                        sendReport(player, cmd, badGuy);
                    } else
                    {
                        endingReport(player);
                    }
                }
            }

            //for the question if wanna report person who already was reported
            else if (already_reported_answer_waiting)
            {
                if((time_already_reported_answer_waiting_asked+10000) <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    player.functions.Chat("/cc [GommeReportNet] Du hast zu lange zum Antworten gebraucht, deshalb wird jetzt abgebrochen..");
                    player.functions.Chat("/hub");
                    already_reported_answer_waiting = false;
                }
            }

            if(testing_net)
            {
                if((testing_time + 10000) <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    testing_results.Add(current_bot_testing, -1);
                    testing_accounts.RemoveAt(0);
                    sendTest(player);
                    return;
                }
            }
        }

    }
}
