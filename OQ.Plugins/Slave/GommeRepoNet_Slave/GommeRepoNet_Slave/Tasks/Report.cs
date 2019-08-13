using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GommeRepoNet_Slave.Tasks
{

    public class Report : ITask
    {
        private readonly string master;
        bool report_order = false;
        long start_time = 0;

        public Report(string master)
        {
            this.master = master;
        }

        public override bool Exec()
        {
            return true;
        }

        public override void Start() { player.events.onChat += OnChat; player.events.onTick += OnTick; }
        public override void Stop() { player.events.onChat -= OnChat; player.events.onTick -= OnTick; }

        private void OnChat(IPlayer player, IChat message, byte position)
        {
            string msg = message.GetText();
            if (msg.StartsWith("[Friends]"))
            {
                if (!msg.Contains(":")) return;
                msg = msg.Replace("[Friends]", "");
                string[] parts = msg.Trim().Split(new char[] { ':' });
                if (!parts[0].Trim().StartsWith(master)) return;

                if(parts[1].Trim().StartsWith("test"))
                {
                    player.functions.Chat("/msg " + master + "test back");
                    return;
                }

                player.functions.Chat("/friend jump " + master);

                Thread.Sleep(2000);

                start_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                report_order = true;

                player.functions.Chat(parts[1].Trim());

                player.functions.Chat("/hub");
            } else if(msg.StartsWith("[Guardian]"))
            {
                if (msg.Contains("You cannot report") || msg.Contains("already reported"))
                {
                    player.functions.Chat("/r no");
                } else if(msg.Contains("Thank you for your report"))
                {
                    player.functions.Chat("/r yes");
                }
            }
        }

        private void OnTick(IPlayer player)
        {
            if(report_order)
            {
                if((start_time + 10000) <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    player.functions.Chat("/hub");
                    report_order = false;
                }
            }
        }

    }
}
