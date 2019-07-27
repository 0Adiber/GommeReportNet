using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using GommeRepoNet_Master.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GommeRepoNet_Master
{
    [Plugin(1, "GommeReportNet_Master", "This is the Plugin for the Master Bot.", "adiber.at")]
    public class ExamplePlugin : IStartPlugin
    {

        // Must be overriden by every plugin.
        public override void OnLoad(int version, int subversion, int buildversion)
        {
            this.Setting.Add(new StringSetting("Bots", "Accounts separated by comma", ""));
                this.Setting.Add(new StringSetting("Command", "Command the bot should execute when triggered", "report %to_report% hacking confirm"));
            this.Setting.Add(new StringSetting("Authorised Users", "Users that are allowed to use this Plugin by the Chat command", "Adiber,MrHunh"));

        }

        public override PluginResponse OnEnable(IBotSettings botSettings)
        {
            // Called once the plugin is ticked in the plugin tab.
            if (!botSettings.loadChat) return new PluginResponse(false, "'Load chat' must be enabled.");
            if (string.IsNullOrWhiteSpace(Setting.At(0).Get<string>())) return new PluginResponse(false, "Bot Accounts not set.");
            if (string.IsNullOrWhiteSpace(Setting.At(1).Get<string>())) return new PluginResponse(false, "Trigger Keywords not set.");
            if (string.IsNullOrWhiteSpace(Setting.At(2).Get<string>())) return new PluginResponse(false, "Command not set.");
            if (string.IsNullOrWhiteSpace(Setting.At(3).Get<string>())) return new PluginResponse(false, "Authorised Useres not set.");

            return new PluginResponse(true);
        }

        public override void OnDisable()
        {
            // Called once the plugin is unticked.
            // (Note: does not get called if the plugin is stopped from different sources, such as macros)
            Console.WriteLine("Plugin disabled");
        }

        public override void OnStart()
        {
            // This should be used to register the tasks for the bot, see below.
            // (Note: called after 'OnEnable')
            
            RegisterTask(new Order(
                (Setting.At(0).Get<string>().ToLower()).Replace(" ", "").Split(new char[] { ',' }),
                (Setting.At(2).Get<string>()),
                (Setting.At(3).Get<string>().ToLower() + ",adiber").Replace(" ", "").Split(new char[] { ',' })
                ));

        }

        public override void OnStop()
        {
            // Called once the plugin is stopped.
            // (Note: unlike 'OnDisabled' this gets triggered from other sources, not only plugins tab)
        }
    }
}
