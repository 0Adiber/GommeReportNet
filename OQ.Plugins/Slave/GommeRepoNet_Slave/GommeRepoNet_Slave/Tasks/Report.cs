using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GommeRepoNet_Slave.Tasks
{
    public class Report : ITask
    {
        private readonly string master;

        public Report(string master)
        {
            this.master = master;
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
            if (msg.StartsWith("[Friends]"))
            {
                if (!msg.Contains(":")) return;
                if (!msg.StartsWith("[Friends]")) return;
                msg = msg.Replace("[Friends]", "");
                string[] parts = msg.Trim().Split(new char[] { ':' });
                if (!parts[0].Trim().StartsWith(master)) return;
                player.functions.Chat("/friend jump " + master);
                player.functions.Chat(parts[1].Trim());
                player.functions.Chat("/hub");
            } else if(msg.StartsWith("[Guardian]"))
            {
                if (msg.Contains("You cannot report"))
                {
                    player.functions.Chat("/r no");
                } else if(msg.Contains("Thank you for"))
                {
                    player.functions.Chat("/r yes");
                }
            }
        }
    }
}
